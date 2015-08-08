using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.States
{


    public class Base : State
    {
        public double Timeout
        {
            get;
            set;
        }

        public Base(GameManager manager, int timeout = -1)
            : base(manager)
        { Timeout = timeout; _lastTick = DateTime.Now; }

        [Command("!limit")]
        public void LimitCommand(string nick, IEnumerable<string> arguments)
        {
            if (arguments.Count() == 0)
            {
                Manager.SendPublic(nick, "The current limit is: {0}", Manager.Limit);
            }
            else
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

        public virtual void TimeoutReached()
        { }

        private DateTime _lastTick;

        public override void Tick()
        {
            if (Timeout < 0)
                return;
            var now = DateTime.Now;
            var diff = now - _lastTick;

            _lastTick = now;
            double previousTimeout = Timeout;
            Timeout -= diff.TotalSeconds;

            if ((int)(Timeout / 20) != (int)(previousTimeout / 20))
            {
                if (Timeout > 0.1 && Timeout < 60)
                {
                    Manager.SendToAll("{0} seconds to go!", (int)Timeout);
                }
            }

            if (Timeout <= 0.1)
                TimeoutReached();
        }

        [Command("!delay")]
        public void DelayCommand(string nick, IEnumerable<string> arguments)
        {
            Timeout += 20;
        }

        [Command("!undelay")]
        public void UndelayCommand(string nick, IEnumerable<string> arguments)
        {
            if (Timeout > 20)
                Timeout -= 20;
            else
                Timeout = 1;
        }

        [Command("!join")]
        public void JoinCommand(string nick, IEnumerable<string> arguments)
        {

            GameUser player = Manager.Resolve(nick);
            if (player == null)
            {
                player = Manager.UserAdd(nick);
                player.CanChooseCards = player.CanVote = true;
                Manager.UpdateCzars();
            }
            
            if (arguments.Count() > 0)
                player.JoinReason = " " + string.Join(" ", arguments);
            Manager.SendPublic(nick, "You joined{0}!", player.JoinReason);
        }

        [Command("!leave")]
        public void LeaveCommand(string nick, IEnumerable<string> arguments)
        {
            var player = Manager.Resolve(nick);
            if (player == null)
                return;


            if (UserLeft(player))
            {
                Manager.UserQuit(nick);
                Manager.SendPublic(nick, "You left{0}!", player.JoinReason);
            }
            else
            {
                player.WantsToLeave = true;
                Manager.SendPublic(player, "You will leave {0}once this round ends!", player.JoinReason);
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
            Manager.SendPublic(nick, "Added <{0}> (a bot of type {1})", botNick, botID);
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

            var cardSet = (ICardSet)GameManager.CardSetTypes[cardsetID].GetConstructor(new Type[] { typeof(IEnumerable<string>) }).Invoke(new object[] { arguments.Skip(1) });
            Manager.AddCardSet(cardSet);
            Manager.SendPublic(nick, "Added {0}", cardSet.Description);
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
                Manager.SendPublic(nick, "Removed card sets!");
            }
            catch (Exception e)
            {
                Manager.SendPublic(nick, "Failed to remove {0} card sets!", arguments.Count());
                Console.WriteLine(e);
            }
        }

        [Command("!defaults")]
        public void DefaultsCommand(string nick, IEnumerable<string> arguments)
        {
            Manager.SendPrivate(nick, "Defaults: {0}", string.Join(", ", Manager.DefaultSets.Keys));
        }

        [Command("!users")]
        public void UsersCommand(string nick, IEnumerable<string> arguments)
        {
            Manager.SendPrivate(nick, "Users: {0}", string.Join(", ", Manager.AllUsers.Select(a => a.Nick)));
        }

        [Command("!removebot")]
        public void RemoveBotCommand(string nick, IEnumerable<string> arguments)
        {
            if (arguments.Count() == 0)
                Manager.SendPrivate(nick, "Usage: !removebot name (without <>)");

            foreach (var bot in arguments)
            {
                Manager.RemoveBot(bot);
            }

            Manager.SendPublic(nick, "Bots removed: {0}", string.Join(", ", arguments));
        }

        [Command("!currentbots")]
        public void CurrentBotsCommand(string nick, IEnumerable<string> arguments)
        {
            Manager.SendPrivate(nick, "Current Bots: {0}", string.Join(", ", Manager.AllUsers.Where(a => a.Bot != null).Select(a => a.Nick)));
        }

        [Command("!adddefault")]
        public void AddDefaultCommand(string nick, IEnumerable<string> arguments)
        {
            if (arguments.Count() == 0)
                arguments = new string[] { "default" };

            try {
                var defaults = arguments.Select(a => new Tuple<string, List<List<string>>>(a, Manager.DefaultSets[a]));
                foreach (var def in defaults)
                {
                    foreach (var list in def.Item2)
                    {
                        Manager.AddCardSet((ICardSet)GameManager.CardSetTypes[list[0]].GetConstructor(new Type[] { typeof(IEnumerable<string>) }).Invoke(new object[] { list.Skip(1) }));
                    }
                    Manager.SendPublic(nick, "Added card set {0}", def.Item1);
                }
            }
            catch (Exception e)
            {
                Manager.SendPublic(nick, "Failed to add defaults");
                Console.WriteLine(e);
            }
        }
    }
}
