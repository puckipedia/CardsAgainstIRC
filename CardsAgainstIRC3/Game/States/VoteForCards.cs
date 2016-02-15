using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.States
{

    public class VoteForCards : Base
    {
        public VoteForCards(GameManager manager)
            : base(manager, 60)
        { }

        public List<GameUser> ComradeOrder = null;
        public List<GameUser> CardsetOrder = null;
        public Dictionary<Guid, Card[]> CardSets = new Dictionary<Guid, Card[]>();
        public Dictionary<Guid, List<int>> Votes = new Dictionary<Guid, List<int>>();
        public Random Random = new Random();

        public override void Activate()
        {
            if (!Manager.AllUsers.Any(a => a.Bot != null || a.HasChosenCards))
            {
                Manager.SendToAll("Noone has chosen... Next round!");
                Manager.StartState(new ChoosingCards(Manager));
                return;
            }

            CardsetOrder = Manager.AllUsers.Where(a => (a.Bot != null && a.CanChooseCards) || a.HasChosenCards).OrderBy(a => Random.Next()).ToList();
            if (Manager.Mode != GameManager.GameMode.SovietRussia)
                ComradeOrder = new List<GameUser>() { Manager.CurrentCzar() };
            else
                ComradeOrder = CardsetOrder;

            Votes = ComradeOrder.Where(a => a.Bot == null || (a.Bot != null && a.CanVote)).ToDictionary(a => a.Guid, a => (List<int>) null);

            int i = 0;
            Manager.SendToAll("Everyone has chosen! The card sets are: ({0} - your time to choose)", string.Join(", ", ComradeOrder.Where(a => a.Bot == null).Select(a => a.Nick)));

            foreach (var or in CardsetOrder)
            {
                if (or.Bot != null)
                    CardSets[or.Guid] = or.Bot.ResponseToCard(Manager.CurrentBlackCard);
                else
                    CardSets[or.Guid] = or.ChosenCards.Select(a => or.Cards[a].Value).ToArray();

                Manager.SendToAll("{0}. {1}", i, Manager.CurrentBlackCard.Representation(CardSets[or.Guid]));

                i++;
            }

            foreach (var bot in ComradeOrder.Where(a => a.Bot != null && a.CanVote))
            {
                Votes[bot.Guid] = bot.Bot.WinningCardSet(CardsetOrder.Select(a => CardSets[a.Guid]).ToArray()).ToList();
            }

            SelectWinner();
        }

        public List<Guid> RunoffVoting()
        {
            List<Guid> Dismissed = new List<Guid>();
            while (true)
            {
                var count = TallyVotes(Dismissed);
                if (count.Count() == 0)
                    return CardsetOrder.Where(a => !Dismissed.Contains(a.Guid)).Select(a => a.Guid).ToList();
                var lowest = count.Min(a => a.Value);
                if (lowest == count.Max(a => a.Value))
                    return count.Keys.ToList();
                Dismissed.AddRange(count.Where(a => a.Value == lowest).Select(a => a.Key));
            }
        }

        public Dictionary<Guid, int> TallyVotes(List<Guid> dismissed)
        {
            return Votes.Where(a => a.Value != null)
                .Select(delegate(KeyValuePair<Guid, List<int>> a) {
                    var values = a.Value.SkipWhile(b => dismissed.Contains(CardsetOrder[b].Guid));
                    return values.Count() == 0 ? -1 : values.First();
                })
                .Where(a => a != -1)
                .GroupBy(a => a)
                .ToDictionary(a => CardsetOrder[a.Key].Guid, a => a.Count());
        }

        public override void TimeoutReached()
        {
            if (Votes.Count(a => a.Value != null) == 0)
            {
                Manager.SendToAll("Timeout has been reached! Noone won...");
                Manager.SendToAll("Points: {0}", Manager.GetPoints(a => CardsetOrder.Contains(a) ? " (" + CardsetOrder.IndexOf(a) + ")" : ""));
                foreach (var person in CardsetOrder)
                    person.ChosenCards = new int[] { };
                Manager.StartState(new ChoosingCards(Manager));
            }
            else
            {
                SelectWinner(true);
            }
        }

        [Command("!status")]
        public void StatusCommand(CommandContext context, IEnumerable<string> arguments)
        {
            if (Manager.Mode == GameManager.GameMode.SovietRussia)
                SendInContext(context, "Waiting for comrade(s) {0} to choose...", string.Join(", ", Votes.Where(a => a.Value == null).Select(a => Manager.Resolve(a.Key).Nick)));
            else
                SendInContext(context, "Waiting for czar {0} to choose...", string.Join(", ", Votes.Where(a => a.Value == null).Select(a => Manager.Resolve(a.Key).Nick)));
        }

        [Command("!card", "!pick", "!p")]
        public void CardCommand(CommandContext context, IEnumerable<string> arguments)
        {
            var user = Manager.Resolve(context.Nick);
            if (user == null)
                return;

            if (!Votes.ContainsKey(user.Guid))
            {
                Manager.SendPrivate(user, "You can't vote now!");
                return;
            }

            try
            {
                var order = arguments.Select(a => int.Parse(a));
                if (order.Any(a => a < 0) || order.Any(a => a >= CardsetOrder.Count))
                    Manager.SendPrivate(user, "Out of range!");
                else
                {
                    if (Votes[user.Guid] == null && Votes.Count(a => a.Value == null) > 1)
                        Manager.SendToAll("{0} has chosen!", user.Nick);

                    Votes[user.Guid] = order.Where(a => CardsetOrder[a] != user).ToList();

                    SelectWinner();
                }
            }
            catch (Exception)
            {
                Manager.SendPrivate(user, "Invalid int!");
            }
        }

        [Command("!skip")]
        public void SkipCommand(CommandContext context, IEnumerable<string> arguments)
        {
            var user = Manager.Resolve(context.Nick);
            if (user == null)
                return;

            if (!Votes.ContainsKey(user.Guid))
                return;

            Votes.Remove(user.Guid);
            SelectWinner();
        }

        private void SelectWinner(bool over = false)
        {
            if (Votes.Any(a => a.Value == null) && !over)
                return;

            var winners = RunoffVoting().Select(a => Manager.Resolve(a));

            if (winners.Count() == 1)
            {
                Manager.SendToAll("And the winner is... {0}!", winners.First().Nick);
                Manager.SendToAll(Manager.CurrentBlackCard.Representation(CardSets[winners.First().Guid]));
            }
            else
                Manager.SendToAll("And the winners are... {0}!", string.Join(", ", winners.Select(a => a.Nick + " (" + CardsetOrder.IndexOf(a) + ")")));

            foreach (var winner in winners)
                winner.Points++;

            Manager.SendToAll("Points: {0}", Manager.GetPoints(a => CardsetOrder.Contains(a) ? " (" + CardsetOrder.IndexOf(a) + ")" : ""));

            foreach (var person in Manager.AllUsers)
                person.RemoveCards();

            IEnumerable<GameUser> totalwinners = null;

            if (Manager.LimitType == GameManager.LimitMode.Rounds && Manager.Rounds >= Manager.Limit)
            {
                var maxpoints = Manager.AllUsers.OrderByDescending(a => a.Points).First().Points;
                totalwinners = Manager.AllUsers.Where(a => a.Points == maxpoints);
            }
            else if (Manager.LimitType == GameManager.LimitMode.Points)
                totalwinners = Manager.AllUsers.Where(a => a.Points >= Manager.Limit);
            if (totalwinners != null && totalwinners.Count() > 0)
            {
                if (totalwinners.Count() == 1)
                    Manager.SendToAll("We have a winner! {0} won{1}!", totalwinners.First().Nick, totalwinners.First().JoinReason);
                else
                    Manager.SendToAll("We have winners! {0}!", string.Join(", ", totalwinners.Select(a => a.Nick)));
                Manager.Reset();
            }
            else
            {
                if (Manager.Mode == GameManager.GameMode.WinnnerIsCzar)
                    Manager.SetCzar(winners.OrderBy(a => Random.Next()).First());
                Manager.StartState(new ChoosingCards(Manager));
            }
        }

        public override bool UserLeft(GameUser user, bool voluntarily)
        {
            user.CanVote = user.CanChooseCards = false;

            if (!Votes.ContainsKey(user.Guid))
                return !CardsetOrder.Contains(user);

            if (Votes[user.Guid] == null)
                Votes.Remove(user.Guid);

            SelectWinner();

            return true;
        }
    }
}
