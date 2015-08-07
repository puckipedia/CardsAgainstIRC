using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.States
{

    public class Vote : Base
    {
        public Vote(GameManager manager)
            : base(manager, 60)
        { }

        public List<GameUser> CzarOrder = null;
        public Dictionary<Guid, Card[]> CardSets = new Dictionary<Guid, Card[]>();
        public Random Random = new Random();

        public override void Activate()
        {
            CzarOrder = Manager.AllUsers.Where(a => a.Bot != null || a.HasChosenCards).OrderBy(a => Random.Next()).ToList();
            if (CzarOrder.Count == 0)
            {
                Manager.SendToAll("Noone has chosen... Next round!");
                Manager.StartState(new ChoosingCards(Manager));
                return;
            }

            int i = 0;
            Manager.SendToAll("Everyone has chosen! The card sets are: ({0} - your time to choose)", Manager.CurrentCzar().Nick);

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

        public override void TimeoutReached()
        {
            Manager.SendToAll("Timeout has been reached! Noone won... Point subtracted for the czar!");
            Manager.CurrentCzar().Points--;
            Manager.SendToAll("Points: {0}", Manager.GetPoints(a => CzarOrder.Contains(a) ? " (" + CzarOrder.IndexOf(a) + ")" : ""));
            foreach (var person in CzarOrder)
                person.ChosenCards = new int[] { };
            Manager.StartState(new ChoosingCards(Manager));
        }

        [Command("!status")]
        public void StatusCommand(string nick, IEnumerable<string> arguments)
        {
            Manager.SendPublic(nick, "Waiting for czar {0} to choose...", Manager.CurrentCzar().Nick);
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
                    SelectWinner(winner);
                }
            }
            catch (Exception)
            {
                Manager.SendPrivate(user, "Invalid int!");
            }
        }

        private void SelectWinner(int winner)
        {
            Manager.SendToAll("And the winner is... {0}!", CzarOrder[winner].Nick);
            Manager.SendToAll("{0}", Manager.CurrentBlackCard.Representation(CardSets[CzarOrder[winner].Guid]));
            CzarOrder[winner].Points++;

            Manager.SendToAll("Points: {0}", Manager.GetPoints(a => CzarOrder.Contains(a) ? " (" + CzarOrder.IndexOf(a) + ")" : ""));

            foreach (var person in Manager.AllUsers)
                person.RemoveCards();

            if (CzarOrder[winner].Points >= Manager.Limit)
            {
                Manager.SendToAll("We have a winner! {0}!", CzarOrder[winner].Nick);
                Manager.Reset();
            }
            else
                Manager.StartState(new ChoosingCards(Manager));
        }

        public override bool UserLeft(GameUser user)
        {
            if (user == Manager.CurrentCzar())
            {
                int random = Random.Next(CzarOrder.Count);
                Manager.SendToAll("Czar has left! Choosing random card to win");
                SelectWinner(random);
            }

            return false;
        }
    }
}
