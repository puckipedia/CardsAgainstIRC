using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string[] CommandNames
        {
            get;
            private set;
        }

        public CommandAttribute(params string[] command)
        {
            CommandNames = command;
        }
    }

    public class State
    {
        public GameManager Manager
        {
            get;
            private set;
        }

        public delegate void CommandDelegate(string user, IEnumerable<string> arguments);

        private Dictionary<string, CommandDelegate> _commands = new Dictionary<string, CommandDelegate>();

        public State(GameManager manager)
        {
            Manager = manager;

            var methods = this.GetType().GetMethods().Where(a => a.GetCustomAttributes(typeof(CommandAttribute), true).Length > 0);
            foreach (var method in methods)
            {
                var commands = method.GetCustomAttributes(typeof(CommandAttribute), true).Cast<CommandAttribute>().SelectMany(a => a.CommandNames);
                foreach (var command in commands)
                {
                    _commands[command] = (CommandDelegate)method.CreateDelegate(typeof(CommandDelegate), this);
                }
            }
        }

        public virtual void Activate()
        {
        }

        public virtual void Deactivate()
        {
        }

        internal virtual bool Command(string nick, string command, IEnumerable<string> arguments)
        {
            if (!_commands.ContainsKey(command))
                return false;

            _commands[command](nick, arguments);

            return true;
        }

        [Command("!state")]
        public void StateCommand(string user, IEnumerable<string> args)
        {
            Manager.SendPrivate(user, "Current state class is {0}", this.GetType());
        }

        [Command("!kill")]
        public void KillCommand(string user, IEnumerable<string> args)
        {
            Manager.Reset();
        }

        [Command("!bots")]
        public void BotsCommand(string user, IEnumerable<string> args)
        {
            Manager.SendPrivate(user, "bots: {0}", string.Join(",", GameManager.Bots.Keys));
        }

        [Command("!cardsets")]
        public void CardSetsCommand(string user, IEnumerable<string> args)
        {
            Manager.SendPrivate(user, "cardsets: {0}", string.Join(",", GameManager.CardSetTypes.Keys));
        }

        public virtual bool UserLeft(GameUser user)
        {
            return true;
        }
    }

    public class TestState : State
    {
        private Logger logger;

        public TestState(GameManager manager)
            : base(manager)
        {
            logger = LogManager.GetLogger("TestState<" + manager.Channel + ">");
        }

        public override void Activate()
        {
            logger.Trace("Test state activated");
        }

        [Command("!log")]
        public void LogCommand(string user, IEnumerable<string> str)
        {
            logger.Trace(string.Join("|", str));
        }
    }

    public class BaseState : State
    {
        public BaseState(GameManager manager)
            : base(manager)
        { }

        [Command("!limit")]
        public void LimitCommand(string nick, IEnumerable<string> arguments)
        {
            if (arguments.Count() == 0)
            {
                Manager.SendPublic(nick, "The current limit is: {0}", Manager.Limit);
            } else
            {
                int result;
                if (int.TryParse(arguments.First(), out result))
                {
                    Manager.Limit = result;
                    Manager.SendPublic(nick, "Set the limit to {0}!", result);
                }
                else
                {
                    Manager.SendPublic(nick, "Failed to set the limit");
                }
            }
        }

        [Command("!join")]
        public void JoinCommand(string nick, IEnumerable<string> arguments)
        {
            var player = Manager.UserAdd(nick);
            player.CanChooseCards = player.CanVote = true;
            Manager.UpdateCzars();
        }

        [Command("!leave")]
        public void LeaveCommand(string nick, IEnumerable<string> arguments)
        {
            var player = Manager.Resolve(nick);
            if (player == null)
                return;


            if (UserLeft(player))
                Manager.UserQuit(nick);
            else
            {
                player.WantsToLeave = true;
                Manager.SendPublic(player, "You will leave once this round ends!");
            }
        }

        [Command("!addbot")]
        public void AddBotCommand(string nick, IEnumerable<string> arguments)
        {
            if (arguments.Count() < 1 || arguments.Count() > 2)
            {
                Manager.SendPrivate(nick, "Usage: !addbot name [nick]");
                return;
            }

            string botID = arguments.ElementAt(0);
            if (!GameManager.Bots.ContainsKey(botID))
            {
                Manager.SendPrivate(nick, "Invalid bot!");
                return;
            }

            string botNick = arguments.ElementAtOrDefault(1) ?? botID;

            Manager.AddBot(botNick, (IBot)GameManager.Bots[botID].GetConstructor(new Type[] { typeof(GameManager) }).Invoke(new object[] { Manager }));
        }

        [Command("!addcards")]
        public void AddCardsCommand(string nick, IEnumerable<string> arguments)
        {
            if (arguments.Count() < 1 || arguments.Count() > 2)
            {
                Manager.SendPrivate(nick, "Usage: !addcards name [arguments...]");
                return;
            }

            string cardsetID = arguments.ElementAt(0);
            if (!GameManager.CardSetTypes.ContainsKey(cardsetID))
            {
                Manager.SendPrivate(nick, "Invalid card set type!");
                return;
            }

            Manager.AddCardSet((ICardSet)GameManager.CardSetTypes[cardsetID].GetConstructor(new Type[] { typeof(IEnumerable<string>) }).Invoke(new object[] { arguments.Skip(1) }));
        }

        [Command("!cards")]
        public void CardsCommand(string nick, IEnumerable<string> arguments)
        {
            var cardsets = Manager.CardSets;
            int i = 0;
            foreach (var set in cardsets)
            {
                Manager.SendToAll("{0}. {1}", i, set.Description);
                i++;
            }
        }

        [Command("!removecards")]
        public void RemoveCardsCommand(string nick, IEnumerable<string> arguments)
        {
            if (arguments.Count() == 0)
            {
                Manager.SendPrivate(nick, "Usage: !removecards num [num2...]");
                return;
            }

            try
            {
                var toremove = arguments.Select(a => Manager.CardSets[int.Parse(a)]);
                Manager.CardSets.RemoveAll(a => toremove.Contains(a));
            }
            catch (Exception)
            {
                Manager.SendPublic(nick, "Failed to remove {0} card sets!", arguments.Count());
            }
        }
    }

    public class WaitForJoinState : BaseState
    {
        public WaitForJoinState(GameManager manager)
            : base(manager)
        { }

        [Command("!start")]
        public void StartCommand(string nick, IEnumerable<string> arguments)
        {
            if (Manager.Users < 3)
            {
                Manager.SendPublic(nick, "We don't have enough players!");
                return;
            }
            else if (Manager.WhiteCards.Count() == 0 || Manager.BlackCards.Count() == 0)
            {
                Manager.SendPublic(nick, "Not evnough cards to start the game!");
                return;
            }

            Manager.ShuffleCards();
            Manager.SendToAll("Game is starting...");
            Manager.StartState(new ChoosingCardsState(Manager));
        }
    }

    public class ChoosingCardsState : BaseState
    {
        public ChoosingCardsState(GameManager manager)
            : base(manager)
        { }

        public List<GameUser> WaitingOnUsers = new List<GameUser>();
        public List<GameUser> ChosenUsers = new List<GameUser>();

        public override void Activate()
        {
            foreach (var person in Manager.AllUsers.Where(a => a.WantsToLeave).Select(a => a.Nick).ToArray())
            {
                Manager.UserQuit(person);
            }

            Manager.UpdateCzars();

            var czar = Manager.NextCzar();
            Manager.NewBlackCard();

            if (Manager.AllUsers.Count() < 3)
            {
                Manager.SendToAll("Not enough players, stopping game");
                Manager.Reset();
                return;
            }

            // XXX: Non-czar modes!
            foreach (var person in Manager.AllUsers)
            {
                person.HasChosenCards = person.HasVoted = false;

                if (person.Bot == null && person.CanChooseCards && person != czar)
                    WaitingOnUsers.Add(person);
            }

            Manager.SendToAll("New round! {0} is czar! {1}, choose your cards!", czar.Nick, string.Join(", ", WaitingOnUsers.Select(a => a.Nick)));
            Manager.SendToAll("Current Card: {0}", Manager.CurrentBlackCard.Representation());

            foreach (var user in WaitingOnUsers)
            {
                user.UpdateCards();
                user.SendCards();
            }

            CheckReady();
        }

        public override bool UserLeft(GameUser user)
        {
            if (WaitingOnUsers.Contains(user))
            {
                WaitingOnUsers.Remove(user);
                ChosenUsers.Add(user);
                user.HasChosenCards = true;
                user.CanVote = false; // remove from possible democracy
                CheckReady();
            }

            return false;
        }

        private void CheckReady()
        {
            if (WaitingOnUsers.Count == 0)
            {
                Manager.StartState(new VoteState(Manager));
            }
        }

        [Command("!card")]
        public void CardCommand(string nick, IEnumerable<string> arguments)
        {
            var user = Manager.Resolve(nick);
            if (user == null)
                return;
            if (!WaitingOnUsers.Contains(user) && !ChosenUsers.Contains(user))
            {
                Manager.SendPrivate(user, "You don't have to choose cards!");
                return;
            }

            try
            {
                int[] cards = arguments.Select(a => int.Parse(a)).ToArray();
                if (cards.Min() < 0 || cards.Max() > user.Cards.Length || cards.Any(a => !user.Cards[a].HasValue))
                {
                    Manager.SendPrivate(user, "Invalid cards!");
                }

                user.ChosenCards = cards;
                user.HasChosenCards = true;

                Manager.SendPrivate(user, "You have chosen: {0}", Manager.CurrentBlackCard.Representation(user.ChosenCards.Select(a => user.Cards[a].Value)));

                if (WaitingOnUsers.Contains(user))
                {
                    WaitingOnUsers.Remove(user);
                    ChosenUsers.Add(user);
                    CheckReady();
                }
            }
            catch (Exception)
            {
                Manager.SendPrivate(user, "Invalid int!");
            }
        }

        [Command("!skip")]
        public void SkipCommand(string nick, IEnumerable<string> arguments)
        {
            var user = Manager.Resolve(nick);
            if (user == null || (!WaitingOnUsers.Contains(user) && !ChosenUsers.Contains(user)))
                return;

            user.HasChosenCards = false;
            if (WaitingOnUsers.Contains(user))
            {
                WaitingOnUsers.Remove(user);
                ChosenUsers.Add(user);
                CheckReady();
            }
        }
    }

    public class VoteState : BaseState
    {
        public VoteState(GameManager manager)
            : base(manager)
        { }

        public List<GameUser> CzarOrder = null;
        public Dictionary<Guid, Card[]> CardSets = new Dictionary<Guid, Card[]>();
        public Random Random = new Random();

        public override void Activate()
        {
            Manager.SendToAll("Everyone has chosen! The card sets are:");

            CzarOrder = Manager.AllUsers.Where(a => a.Bot != null || a.HasChosenCards).OrderBy(a => Random.Next()).ToList();
            int i = 0;
            foreach (var or in CzarOrder)
            {
                if (or.Bot != null)
                    CardSets[or.Guid] = or.Bot.ResponseToCard(Manager.CurrentBlackCard);
                else
                    CardSets[or.Guid] = or.ChosenCards.Select(a => or.Cards[a].Value).ToArray();

                Manager.SendToAll("{0}. {1}", i, Manager.CurrentBlackCard.Representation(CardSets[or.Guid]));

                i++;
            }
        }

        [Command("!card")]
        public void CardCommand(string nick, IEnumerable<string> arguments)
        {
            var user = Manager.Resolve(nick);
            if (user == null)
                return;

            if (user != Manager.CurrentCzar())
                return;

            try
            {
                int winner = int.Parse(arguments.First());
                if (winner < 0 || winner >= CzarOrder.Count)
                    Manager.SendPrivate(user, "Out of range!");
                else
                {
                    Manager.SendToAll("And the winner is... {0}!", CzarOrder[winner].Nick);
                    Manager.SendToAll("{0}", Manager.CurrentBlackCard.Representation(CardSets[CzarOrder[winner].Guid]));
                    CzarOrder[winner].Points++;

                    Manager.SendToAll("Points: {0}", Manager.GetPoints(a => CzarOrder.Contains(a) ? " (" + CzarOrder.IndexOf(a) + ")" : ""));
                    if (CzarOrder[winner].Points >= Manager.Limit)
                    {
                        Manager.SendToAll("We have a winner! {0}!", CzarOrder[winner].Nick);
                        Manager.Reset();
                    }
                    else
                        Manager.StartState(new ChoosingCardsState(Manager));
                }
            }
            catch (Exception)
            {
                Manager.SendPrivate(user, "Invalid int!");
            }
        }

        public override bool UserLeft(GameUser user)
        {
            if (user == Manager.CurrentCzar())
            {
                int random = Random.Next(CzarOrder.Count);
                Manager.SendToAll("Czar has left! Choosing random card to win");
                CardCommand(user.Nick, new string[] { random.ToString() });
            }

            return false;
        }
    }

    public class InactiveState : State
    {
        public InactiveState(GameManager manager)
            : base(manager)
        { }


        [Command("!start")]
        public void StartCommand(string nick, IEnumerable<string> arguments)
        {
            Manager.SendToAll("{0} started a game! | send !join to join!", nick);
            var started = Manager.UserAdd(nick);
            started.CanChooseCards = started.CanVote = true;
            Manager.UpdateCzars();
            Manager.Data["started"] = started;
            Manager.StartState(new WaitForJoinState(Manager));
        }
    }
}
