using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.States
{


    public class Base : State
    {
        public Base(GameManager manager)
            : base(manager)
        { }

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
}
