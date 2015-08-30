using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.Bots
{
    [Bot("randkov")]
    public class Randkov : IBot
    {
        private class MarkovGenerator
        {
            private Dictionary<string, List<string>> _data = new Dictionary<string, List<string>>();
            private List<string> _startList = new List<string>();

            public void Feed(IEnumerable<string> data)
            {
                string previous = null;
                foreach (var item in data)
                {
                    if (previous == null)
                        _startList.Add(item);
                    else
                    {
                        if (!_data.ContainsKey(previous))
                            _data[previous] = new List<string>();
                        _data[previous].Add(item);
                    }
                    previous = item.ToLower();
                }

                if (!_data.ContainsKey(previous))
                    _data[previous] = new List<string>();
                _data[previous].Add(null);
            }

            public IEnumerable<string> Get()
            {
                string pointer = _startList[random.Next(_startList.Count)];
                while (pointer != null)
                {
                    yield return pointer;
                    pointer = _data[pointer.ToLower()][random.Next(_data[pointer.ToLower()].Count)];
                }
            }
        }

        public GameManager Manager
        {
            get;
            private set;
        }

        public GameUser User
        {
            get;
            private set;
        }

        public bool CanVote
        {
            get
            {
                return true;
            }
        }

        public bool CanChooseCards
        {
            get
            {
                return true;
            }
        }

        private IDeckType Deck;
        private MarkovGenerator gen = new MarkovGenerator();

        public Randkov(GameManager manager, IEnumerable<string> arguments)
        {

            if (arguments.Count() == 0)
            {
                throw new ArgumentException("Pass a deck to randkov!");
            }

            try
            {
                Deck = (IDeckType) GameManager.DeckTypes[arguments.First()].GetConstructor(new Type[] { typeof(GameManager), typeof(IEnumerable<string>) }).Invoke(new object[] { Manager, arguments.Skip(1) });
                while (Deck.WhiteCards > 0)
                    gen.Feed(string.Join(" ", Deck.TakeWhiteCard().Parts).Split(' '));
            }
            catch (Exception)
            {
                throw new ArgumentException("Failed to load deck!");
            }

            Manager = manager;
        }

        public void LinkedToUser(GameUser user)
        {
            User = user;
        }

        public Card[] ResponseToCard(Card blackCard)
        {
            return Enumerable.Range(0, blackCard.Parts.Length - 1).Select(a => new Card() { Parts = new string[] { string.Join(" ", gen.Get()) } }).ToArray();
        }

        public int[] WinningCardSet(Card[][] cards)
        {
            throw new NotImplementedException("Nope");
        }

        private static Random random = new Random();
    }
}
