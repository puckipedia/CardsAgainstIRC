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

        private IDeckType _deck;
        private MarkovGenerator _generator = new MarkovGenerator();

        public Randkov(GameManager manager, IEnumerable<string> arguments)
        {

            if (arguments.Count() == 0)
            {
                throw new ArgumentException("Pass a deck to randkov!");
            }

            try
            {
                _deck = (IDeckType) GameManager.DeckTypes[arguments.First()].GetConstructor(new Type[] { typeof(GameManager), typeof(IEnumerable<string>) }).Invoke(new object[] { Manager, arguments.Skip(1) });
                while (_deck.WhiteCards > 0)
                    _generator.Feed(string.Join(" ", _deck.TakeWhiteCard().Parts).Split(' '));
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
            return Enumerable.Range(0, blackCard.Parts.Length - 1).Select(a => new Card() { Parts = new string[] { string.Join(" ", _generator.Get()) } }).ToArray();
        }

        public int[] WinningCardSet(Card[][] cards)
        {
            throw new NotImplementedException("Nope");
        }
    }
}
