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

        [Command("!pause")]
        public void PauseCommand(string nick, IEnumerable<string> arguments)
        {
            var player = Manager.Resolve(nick);
            if (player == null)
                return;

            UserLeft(player, false);
        }

        [Command("!resume")]
        public void ResumeCommand(string nick, IEnumerable<string> arguments)
        {
            var player = Manager.Resolve(nick);
            if (player == null)
                return;

            player.CanChooseCards = player.CanVote = true;
        }

        [Command("!leave")]
        public void LeaveCommand(string nick, IEnumerable<string> arguments)
        {
            var player = Manager.Resolve(nick);
            if (player == null)
                return;

            if (UserLeft(player, true))
            {
                Manager.UserQuit(nick);
                Manager.SendPublic(nick, "You left{0}!", player.JoinReason);
            }
            else
            {
                player.WantsToLeave = true;
                Manager.SendPublic(player, "You will leave{0} once this round ends!", player.JoinReason);
            }
        }

        [CompoundCommand("!bot", "add")]
        public void BotAddCommand(string nick, IEnumerable<string> arguments)
        {
            if (arguments.Count() < 1 || arguments.Count() > 2)
            {
                Manager.SendPrivate(nick, "Usage: !bot add name [nick]");
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

        [CompoundCommand("!deck", "add")]
        public void DeckAddCommand(string nick, IEnumerable<string> arguments)
        {
            if (arguments.Count() < 1)
            {
                Manager.SendPrivate(nick, "Usage: !deck.add name [arguments...]");
                return;
            }

            string cardsetID = arguments.ElementAt(0);
            if (!GameManager.DeckTypes.ContainsKey(cardsetID))
            {
                Manager.SendPrivate(nick, "Invalid deck type!");
                return;
            }

            var cardSet = (IDeckType)GameManager.DeckTypes[cardsetID].GetConstructor(new Type[] { typeof(GameManager), typeof(IEnumerable<string>) }).Invoke(new object[] { Manager, arguments.Skip(1) });
            Manager.AddCardSet(cardSet);
            Manager.SendPublic(nick, "Added {0}", cardSet.Description);
        }

        [CompoundCommand("!deck", "weight")]
        public void DeckWeightCommand(string nick, IEnumerable<string> arguments)
        {
            if (arguments.Count() > 2 || arguments.Count() == 0)
            {
                Manager.SendPrivate(nick, "Usage: !deck.weight deck [weight]");
                return;
            }

            int deck;
            if (!int.TryParse(arguments.First(), out deck) || deck < 0 || deck >= Manager.CardSets.Count)
            {
                Manager.SendPrivate(nick, "Out of range!");
                return;
            }

            if (arguments.Count() == 1)
            {
                var deckinfo = Manager.CardSets[deck];
                Manager.SendPrivate(nick, "Weight of {0} is {1}", deckinfo.Item1.Description, deckinfo.Item2);
            }
            else
            {
                int weight;
                if (!int.TryParse(arguments.ElementAt(1), out weight) || weight < 1)
                {
                    Manager.SendPrivate(nick, "Weight is out of range!");
                    return;
                }

                var deckinfo = Manager.CardSets[deck];
                Manager.CardSets[deck] = new Tuple<IDeckType, int>(deckinfo.Item1, weight);
            }
        }

        [CompoundCommand("!deck", "list")]
        public void DeckListCommand(string nick, IEnumerable<string> arguments)
        {
            var cardsets = Manager.CardSets;
            int i = 0;
            foreach (var set in cardsets)
            {
                Manager.SendToAll("{0}. \x02{1}\x02 (weight {2})", i, set.Item1.Description, set.Item2);
                i++;
            }
        }

        [CompoundCommand("!deck", "remove")]
        public void DeckRemoveCommand(string nick, IEnumerable<string> arguments)
        {
            if (arguments.Count() == 0)
            {
                Manager.SendPrivate(nick, "Usage: !deck.remove num [num2...]");
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

        [CompoundCommand("!deckset", "list")]
        public void DecksetListCommand(string nick, IEnumerable<string> arguments)
        {
            Manager.SendPrivate(nick, "Deck sets: {0}", string.Join(", ", Manager.DefaultSets.Keys));
        }

        [CompoundCommand("!user", "list")]
        public void UsersCommand(string nick, IEnumerable<string> arguments)
        {
            Manager.SendPublic(nick, "Users: {0}", string.Join(", ", Manager.AllUsers.Select(a => a.Nick)));
        }

        [CompoundCommand("!user", "info")]
        public void UserInfoCommand(string nick, IEnumerable<string> arguments)
        {
            if (arguments.Count() != 1)
            {
                Manager.SendPrivate(nick, "Usage: !user.info nick");
                return;
            }

            var user = Manager.Resolve(nick);
            if (user == null)
            {
                Manager.SendPrivate(nick, "That nick doesn't exist!");
                return;
            }

            Manager.SendPrivate(nick, "Nick: '{0}', Can vote: {1}, Can choose cards: {2}", user.Nick, user.CanVote, user.CanChooseCards);
        }

        [CompoundCommand("!bot", "remove")]
        public void BotRemoveCommand(string nick, IEnumerable<string> arguments)
        {
            if (arguments.Count() == 0)
                Manager.SendPrivate(nick, "Usage: !bot.remove name (without <>)");

            foreach (var bot in arguments)
            {
                Manager.RemoveBot(bot);
            }

            Manager.SendPublic(nick, "Bots removed: {0}", string.Join(", ", arguments));
        }

        [CompoundCommand("!bot", "list")]
        public void BotListCommand(string nick, IEnumerable<string> arguments)
        {
            Manager.SendPrivate(nick, "Current Bots: {0}", string.Join(", ", Manager.AllUsers.Where(a => a.Bot != null).Select(a => a.Nick)));
        }

        [CompoundCommand("!bot", "vote")]
        public void BotVoteCommand(string nick, IEnumerable<string> arguments)
        {
            if (arguments.Count() < 1 || arguments.Count() > 2)
            {
                Manager.SendPrivate(nick, "Usage: !bot vote bot_name [should_be_able_to_vote]");
                return;
            }

            var bot = Manager.Resolve("<" + arguments.First() + ">");
            if (bot == null)
            {
                Manager.SendPrivate(nick, "That is not a bot!");
                return;
            }

            if (arguments.Count() == 2)
            {
                bot.CanVote = arguments.ElementAt(1).IsTruthy();
                if (bot.CanVote && !bot.Bot.CanVote)
                {
                    Manager.SendPrivate(nick, "The bot doesn't support voting!");
                    bot.CanVote = false;
                    return;
                }
            }

            Manager.SendPublic(nick, "<{0}> Can{1} vote.", arguments.First(), bot.CanVote ? "" : "not");
        }

        [CompoundCommand("!deckset", "add")]
        public void DecksetAddCommand(string nick, IEnumerable<string> arguments)
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
                    Manager.SendPublic(nick, "Added deck {0}", def.Item1);
                }
            }
            catch (Exception e)
            {
                Manager.SendPublic(nick, "Failed to add deckset");
                Console.WriteLine(e);
            }
        }

        [Command("!mode")]
        public void ModeCommand(string nick, IEnumerable<string> arguments)
        {
            if (arguments.Count() != 1)
            {
                Manager.SendPublic(nick, "Current mode: {0}", Manager.Mode.ToString());
                return;
            }

            string mode = arguments.First().ToLowerInvariant();
            if (mode == "czar")
            {
                Manager.Mode = GameManager.GameMode.Czar;
                Manager.SendPublic(nick, "Mode set to Czar!");
            }
            else if (mode == "soviet")
            {
                Manager.Mode = GameManager.GameMode.SovietRussia;
                Manager.SendPublic(nick, "Mode set to Soviet Russia!");
            }
            else
            {
                Manager.SendPrivate(nick, "Usage: !mode {czar,soviet}");
            }
        }
    }
}
