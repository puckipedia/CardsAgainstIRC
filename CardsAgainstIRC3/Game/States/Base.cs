using Jint;
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
        public void LimitCommand(CommandContext context, IEnumerable<string> arguments)
        {
            if (arguments.Count() == 0)
            {
                SendInContext(context, "The current limit is: {0} {1}", Manager.Limit, Manager.LimitType);
            }
            else
            {
                int result;
                if (int.TryParse(arguments.First(), out result))
                {
                    Manager.Limit = result;

                    if (arguments.ElementAtOrDefault(1)?.ToLowerInvariant()?.StartsWith("r") == true)
                        Manager.LimitType = GameManager.LimitMode.Rounds;
                    else
                        Manager.LimitType = GameManager.LimitMode.Points;

                    Manager.SendPublic(context.Nick, "Set the limit to {0} {1}!", result, Manager.LimitType);
                }
                else
                {
                    SendInContext(context, "Failed to set the limit");
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
        public void DelayCommand(CommandContext context, IEnumerable<string> arguments)
        {
            Timeout += 20;
        }

        [Command("!undelay")]
        public void UndelayCommand(CommandContext context, IEnumerable<string> arguments)
        {
            if (Timeout > 20)
                Timeout -= 20;
            else
                Timeout = 1;
        }

        [Command("!join")]
        public void JoinCommand(CommandContext context, IEnumerable<string> arguments)
        {

            GameUser player = Manager.Resolve(context.Nick);
            if (player == null)
            {
                player = Manager.UserAdd(context.Nick);
                player.CanChooseCards = player.CanVote = true;
                Manager.UpdateCzars();
            }
            
            if (arguments.Count() > 0)
                player.JoinReason = " " + string.Join(" ", arguments);
            Manager.SendPublic(context.Nick, "You joined{0}!", player.JoinReason);
        }

        [Command("!pause")]
        public void PauseCommand(CommandContext context, IEnumerable<string> arguments)
        {
            var player = Manager.Resolve(context.Nick);
            if (player == null)
                return;

            UserLeft(player, false);
        }

        [Command("!resume")]
        public void ResumeCommand(CommandContext context, IEnumerable<string> arguments)
        {
            var player = Manager.Resolve(context.Nick);
            if (player == null)
                return;

            player.CanChooseCards = player.CanVote = true;
        }

        [Command("!leave")]
        public void LeaveCommand(CommandContext context, IEnumerable<string> arguments)
        {
            var player = Manager.Resolve(context.Nick);
            if (player == null)
                return;

            if (UserLeft(player, true))
            {
                Manager.UserQuit(context.Nick);
                Manager.SendPublic(context.Nick, "You left{0}!", player.JoinReason);
            }
            else
            {
                player.WantsToLeave = true;
                Manager.SendPublic(player, "You will leave{0} once this round ends!", player.JoinReason);
            }
        }

        [CompoundCommand("!bot", "add")]
        public void BotAddCommand(CommandContext context, IEnumerable<string> arguments)
        {
            if (arguments.Count() < 1)
            {
                SendInContext(context, "Usage: !bot add name [nick [arguments]]");
                return;
            }

            string botID = arguments.ElementAt(0);
            if (!GameManager.Bots.ContainsKey(botID))
            {
                SendInContext(context, "Invalid bot!");
                return;
            }

            string botNick = arguments.ElementAtOrDefault(1) ?? botID;

            try
            {
                Manager.AddBot(botNick, (IBot)GameManager.Bots[botID].GetConstructor(new Type[] { typeof(GameManager), typeof(IEnumerable<string>) }).Invoke(new object[] { Manager, arguments.Skip(2) }));
                Manager.SendPublic(context.Nick, "Added <{0}> (a bot of type {1})", botNick, botID);
            }
            catch (ArgumentException e)
            {
                SendInContext(context, "Error adding {0}: {1}", botNick, e.Message);
            }
        }

        [CompoundCommand("!deck", "add")]
        public void DeckAddCommand(CommandContext context, IEnumerable<string> arguments)
        {
            if (arguments.Count() < 1)
            {
                SendInContext(context, "Usage: !deck.add name [arguments...]");
                return;
            }

            string cardsetID = arguments.ElementAt(0);
            if (!GameManager.DeckTypes.ContainsKey(cardsetID))
            {
                SendInContext(context, "Invalid deck type!");
                return;
            }

            var cardSet = (IDeckType)GameManager.DeckTypes[cardsetID].GetConstructor(new Type[] { typeof(GameManager), typeof(IEnumerable<string>) }).Invoke(new object[] { Manager, arguments.Skip(1) });
            Manager.AddCardSet(cardSet);
            Manager.SendPublic(context.Nick, "Added {0}", cardSet.Description);
        }

        [CompoundCommand("!deck", "weight")]
        public void DeckWeightCommand(CommandContext context, IEnumerable<string> arguments)
        {
            if (arguments.Count() > 2 || arguments.Count() == 0)
            {
                SendInContext(context, "Usage: !deck.weight deck [weight]");
                return;
            }

            int deck;
            if (!int.TryParse(arguments.First(), out deck) || deck < 0 || deck >= Manager.CardSets.Count)
            {
                SendInContext(context, "Out of range!");
                return;
            }

            if (arguments.Count() == 1)
            {
                var deckinfo = Manager.CardSets[deck];
                SendInContext(context, "Weight of {0} is {1}", deckinfo.Item1.Description, deckinfo.Item2);
            }
            else
            {
                int weight;
                if (!int.TryParse(arguments.ElementAt(1), out weight) || weight < 1)
                {
                    SendInContext(context, "Weight is out of range!");
                    return;
                }

                var deckinfo = Manager.CardSets[deck];
                Manager.CardSets[deck] = new Tuple<IDeckType, int>(deckinfo.Item1, weight);
            }
        }

        [CompoundCommand("!deck", "list")]
        public void DeckListCommand(CommandContext context, IEnumerable<string> arguments)
        {
            var cardsets = Manager.CardSets;
            int i = 0;
            foreach (var set in cardsets)
            {
                SendInContext(context, "{0}. \x02{1}\x02 (weight {2})", i, set.Item1.Description, set.Item2);
                i++;
            }
        }

        [CompoundCommand("!deck", "remove")]
        public void DeckRemoveCommand(CommandContext context, IEnumerable<string> arguments)
        {
            if (arguments.Count() == 0)
            {
                SendInContext(context, "Usage: !deck.remove num [num2...]");
                return;
            }

            try
            {
                var toremove = arguments.Select(a => Manager.CardSets[int.Parse(a)]);
                Manager.CardSets.RemoveAll(a => toremove.Contains(a));
                Manager.SendPublic(context.Nick, "Removed card sets!");
            }
            catch (Exception e)
            {
                SendInContext(context, "Failed to remove {0} card sets!", arguments.Count());
                Console.WriteLine(e);
            }
        }

        [CompoundCommand("!deckset", "list")]
        public void DecksetListCommand(CommandContext context, IEnumerable<string> arguments)
        {
            SendInContext(context, "Deck sets: {0}", string.Join(", ", Manager.DefaultSets.Keys));
        }

        [CompoundCommand("!user", "list")]
        public void UsersCommand(CommandContext context, IEnumerable<string> arguments)
        {
            SendInContext(context, "Users: {0}", string.Join(", ", Manager.AllUsers.Select(a => a.Nick)));
        }

        [CompoundCommand("!user", "info")]
        public void UserInfoCommand(CommandContext context, IEnumerable<string> arguments)
        {
            if (arguments.Count() != 1)
            {
                SendInContext(context, "Usage: !user.info nick");
                return;
            }

            var user = Manager.Resolve(context.Nick);
            if (user == null)
            {
                SendInContext(context, "That nick doesn't exist!");
                return;
            }

            SendInContext(context, "Nick: '{0}', Can vote: {1}, Can choose cards: {2}", user.Nick, user.CanVote, user.CanChooseCards);
        }

        [CompoundCommand("!bot", "remove")]
        public void BotRemoveCommand(CommandContext context, IEnumerable<string> arguments)
        {
            if (arguments.Count() == 0)
                SendInContext(context, "Usage: !bot.remove name (without <>)");

            foreach (var bot in arguments)
            {
                Manager.RemoveBot(bot);
            }

            Manager.SendPublic(context.Nick, "Bots removed: {0}", string.Join(", ", arguments));
        }

        [CompoundCommand("!bot", "list")]
        public void BotListCommand(CommandContext context, IEnumerable<string> arguments)
        {
            SendInContext(context, "Current Bots: {0}", string.Join(", ", Manager.AllUsers.Where(a => a.Bot != null).Select(a => a.Nick)));
        }

        [CompoundCommand("!bot", "vote")]
        public void BotVoteCommand(CommandContext context, IEnumerable<string> arguments)
        {
            if (arguments.Count() < 1 || arguments.Count() > 2)
            {
                SendInContext(context, "Usage: !bot vote bot_name [should_be_able_to_vote]");
                return;
            }

            var bot = Manager.Resolve("<" + arguments.First() + ">");
            if (bot == null)
            {
                SendInContext(context, "That is not a bot!");
                return;
            }

            if (arguments.Count() == 2)
            {
                bot.CanVote = arguments.ElementAt(1).IsTruthy();
                if (bot.CanVote && !bot.Bot.CanVote)
                {
                    SendInContext(context, "The bot doesn't support voting!");
                    bot.CanVote = false;
                    return;
                }
            }

            Manager.SendPublic(context.Nick, "<{0}> Can{1} vote.", arguments.First(), bot.CanVote ? "" : "not");
        }

        [CompoundCommand("!deckset", "add")]
        public void DecksetAddCommand(CommandContext context, IEnumerable<string> arguments)
        {
            if (arguments.Count() == 0)
                arguments = new string[] { "default" };

            try {
                var defaults = arguments.Select(a => new Tuple<string, List<List<string>>>(a, Manager.DefaultSets[a]));
                foreach (var def in defaults)
                {
                    foreach (var list in def.Item2)
                    {
                        Manager.AddCardSet((IDeckType)GameManager.DeckTypes[list[0]].GetConstructor(new Type[] { typeof(GameManager), typeof(IEnumerable<string>) }).Invoke(new object[] { Manager, list.Skip(1) }));
                    }
                    Manager.SendPublic(context.Nick, "Added deck {0}", def.Item1);
                }
            }
            catch (Exception e)
            {
                SendInContext(context, "Failed to add deckset");
                Console.WriteLine(e);
            }
        }

        [Command("!mode")]
        public void ModeCommand(CommandContext context, IEnumerable<string> arguments)
        {
            if (arguments.Count() != 1)
            {
                SendInContext(context, "Current mode: {0}", Manager.Mode.ToString());
                return;
            }

            string mode = arguments.First().ToLowerInvariant();
            if (mode.StartsWith("c"))
            {
                Manager.Mode = GameManager.GameMode.Czar;
                Manager.SendPublic(context.Nick, "Mode set to Czar!");
            }
            else if (mode.StartsWith("s"))
            {
                Manager.Mode = GameManager.GameMode.SovietRussia;
                Manager.SendPublic(context.Nick, "Mode set to Soviet Russia!");
            }
            else if (mode.StartsWith("w"))
            {
                Manager.Mode = GameManager.GameMode.WinnnerIsCzar;
                Manager.SendPublic(context.Nick, "Mode set to Winner is Czar!");
            }
            else
            {
                SendInContext(context, "Usage: !mode {czar,soviet,winner}");
            }
        }
    }
}
