﻿using System;
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

            if (Manager.Mode == GameManager.GameMode.Czar)
                ComradeOrder = new List<GameUser>() { Manager.CurrentCzar() };
            else
                ComradeOrder = Manager.AllUsers.Where(a => a.Bot != null || a.HasChosenCards).OrderBy(a => Random.Next()).ToList();

            Votes = ComradeOrder.Where(a => a.Bot == null).ToDictionary(a => a.Guid, a => (List<int>) null);

            int i = 0;
            Manager.SendToAll("Everyone has chosen! The card sets are: ({0} - your time to choose)", string.Join(", ", ComradeOrder.Where(a => a.Bot == null).Select(a => a.Nick)));

            foreach (var or in ComradeOrder)
            {
                if (or.Bot != null)
                    CardSets[or.Guid] = or.Bot.ResponseToCard(Manager.CurrentBlackCard);
                else
                    CardSets[or.Guid] = or.ChosenCards.Select(a => or.Cards[a].Value).ToArray();

                Manager.SendToAll("{0}. {1}", i, Manager.CurrentBlackCard.Representation(CardSets[or.Guid]));

                i++;
            }
        }

        public List<Guid> RunoffVoting()
        {
            List<Guid> Dismissed = new List<Guid>();
            while (true)
            {
                var count = TallyVotes(Dismissed);
                if (count.Count() == 0)
                    return ComradeOrder.Where(a => !Dismissed.Contains(a.Guid)).Select(a => a.Guid).ToList();
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
                    var values = a.Value.SkipWhile(b => dismissed.Contains(ComradeOrder[b].Guid));
                    return values.Count() == 0 ? -1 : values.First();
                })
                .Where(a => a != -1)
                .GroupBy(a => a)
                .ToDictionary(a => ComradeOrder[a.Key].Guid, a => a.Count());
        }

        public override void TimeoutReached()
        {
            if (Votes.Count(a => a.Value != null) == 0)
            {
                Manager.SendToAll("Timeout has been reached! Noone won...");
                Manager.SendToAll("Points: {0}", Manager.GetPoints(a => ComradeOrder.Contains(a) ? " (" + ComradeOrder.IndexOf(a) + ")" : ""));
                foreach (var person in ComradeOrder)
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
            if (Manager.Mode == GameManager.GameMode.SovietRussia)
                Manager.SendPublic(nick, "Waiting for comrade(s) {0} to choose...", string.Join(", ", Votes.Where(a => a.Value == null).Select(a => Manager.Resolve(a.Key).Nick)));
            else
                Manager.SendPublic(nick, "Waiting for czar {0} to choose...", string.Join(", ", Votes.Where(a => a.Value == null).Select(a => Manager.Resolve(a.Key).Nick)));
        }

        [Command("!card", "!pick", "!p")]
        public void CardCommand(string nick, IEnumerable<string> arguments)
        {
            var user = Manager.Resolve(nick);
            if (user == null)
                return;

            if (!Votes.ContainsKey(user.Guid))
                return;

            try
            {
                var order = arguments.Select(a => int.Parse(a));
                if (order.Any(a => a < 0) || order.Any(a => a >= ComradeOrder.Count))
                    Manager.SendPrivate(user, "Out of range!");
                else
                {
                    Votes[user.Guid] = order.Where(a => ComradeOrder[a] != user).ToList();
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

            Votes[user.Guid] = new List<int>();
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
                Manager.SendToAll("And the winners are... {0}!", string.Join(", ", winners.Select(a => a.Nick + " (" + ComradeOrder.IndexOf(a) + ")")));

            foreach (var winner in winners)
                winner.Points++;

            Manager.SendToAll("Points: {0}", Manager.GetPoints(a => ComradeOrder.Contains(a) ? " (" + ComradeOrder.IndexOf(a) + ")" : ""));

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

        public override bool UserLeft(GameUser user, bool voluntarily)
        {
            if (!voluntarily)
                user.CanVote = user.CanChooseCards = false;

            if (!Votes.ContainsKey(user.Guid))
                return false;

            if (Votes[user.Guid] == null)
                Votes.Remove(user.Guid);

            SelectWinner();

            return false;
        }
    }
}