using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.States
{

    public class SovietRussiaVote : Base
    {
        public SovietRussiaVote(GameManager manager)
            : base(manager, 60)
        { }

        public List<GameUser> CzarOrder = null;
        public Dictionary<Guid, Card[]> CardSets = new Dictionary<Guid, Card[]>();
        public Dictionary<Guid, int> Votes = new Dictionary<Guid, int>();
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

            Votes = CzarOrder.Where(a => a.Bot == null).ToDictionary(a => a.Guid, a => -2);

            int i = 0;
            Manager.SendToAll("Everyone has chosen! The card sets are: ({0} - your time to choose)", string.Join(", ", CzarOrder.Select(a => a.Nick)));

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
            if (Votes.Count(a => a.Value > -2) == 0)
            {
                Manager.SendToAll("Timeout has been reached! Noone won...");
                Manager.SendToAll("Points: {0}", Manager.GetPoints(a => CzarOrder.Contains(a) ? " (" + CzarOrder.IndexOf(a) + ")" : ""));
                foreach (var person in CzarOrder)
                    person.ChosenCards = new int[] { };
                Manager.StartState(new ChoosingCards(Manager));
            }
            else
            {
                SelectWinner(true);
            }
        }

        [Command("!status")]
        public void StatusCommand(string nick, IEnumerable<string> arguments)
        {
            Manager.SendPublic(nick, "Waiting for czars {0} to choose...", string.Join(", ", Votes.Where(a => a.Value == -2).Select(a => Manager.Resolve(a.Key).Nick)));
        }

        [Command("!card")]
        public void CardCommand(string nick, IEnumerable<string> arguments)
        {
            var user = Manager.Resolve(nick);
            if (user == null)
                return;

            if (!Votes.ContainsKey(user.Guid))
                return;

            try
            {
                int winner = int.Parse(arguments.First());
                if (winner < 0 || winner >= CzarOrder.Count)
                    Manager.SendPrivate(user, "Out of range!");
                else
                {
                    Votes[user.Guid] = winner;
                    SelectWinner();
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
            if (user == null)
                return;

            if (!Votes.ContainsKey(user.Guid))
                return;

            Votes[user.Guid] = -1;
        }

        private void SelectWinner(bool over = false)
        {
            if (Votes.Any(a => a.Value == -2) && !over)
                return;

            var maxvalue = CzarOrder.Max(a => Votes.Count(b => b.Value > -1 && CzarOrder[b.Value] == a));
            var winners = CzarOrder.Where(a => Votes.Count(b => b.Value > -1 && CzarOrder[b.Value] == a) == maxvalue);
            if (winners.Count() == 1)
                Manager.SendToAll("And the winner is... {0}!", winners.First().Nick);
            else
                Manager.SendToAll("And the winners are... {0}!", string.Join(", ", winners.Select(a => a.Nick)));

            foreach (var winner in winners)
                winner.Points++;

            Manager.SendToAll("Points: {0}", Manager.GetPoints(a => CzarOrder.Contains(a) ? " (" + CzarOrder.IndexOf(a) + ")" : ""));

            foreach (var person in Manager.AllUsers)
                person.RemoveCards();


            var totalwinners = Manager.AllUsers.Where(a => a.Points >= Manager.Limit);
            if (totalwinners.Count() > 0)
            {
                if (totalwinners.Count() == 1)
                    Manager.SendToAll("We have a winner! {0}!", totalwinners.First().Nick);
                else
                    Manager.SendToAll("We have winners! {0}!", string.Join(", ", totalwinners.Select(a => a.Nick)));
                Manager.Reset();
            }
            else
                Manager.StartState(new ChoosingCards(Manager));
        }

        public override bool UserLeft(GameUser user)
        {
            if (!Votes.ContainsKey(user.Guid))
                return false;

            if (Votes[user.Guid] == -2)
                Votes.Remove(user.Guid);

            SelectWinner();

            return false;
        }
    }
}
