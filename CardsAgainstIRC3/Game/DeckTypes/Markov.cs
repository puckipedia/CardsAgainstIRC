using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.DeckTypes
{
    [DeckType("markov")]
    class Markov : IDeckType
    {
        private IDeckType _deck;
        private int _maxWhite;
        private int _maxBlack;
        private MarkovGenerator _whiteGenerator = new MarkovGenerator();
        private MarkovGenerator _blackGenerator = new MarkovGenerator();

        public int WhiteCards
        {
            get
            {
                return _maxWhite;
            }
        }

        public int BlackCards
        {
            get
            {
                return _maxBlack;
            }
        }

        public Card TakeWhiteCard()
        {
            _maxWhite--;

            return new Card()
            {
                Parts = new string[]
                {
                    string.Join(" ", _whiteGenerator.Get())
                }
            };
        }

        public Card TakeBlackCard()
        {
            _maxBlack--;

            return new Card()
            {
                Parts = _blackGenerator.Get().ToArray()
            };
        }

        public string Description
        {
            get
            {
                return string.Format("Markov: ({0} white, {1} black left) {2}", _maxWhite, _maxBlack, _deck.Description);
            }
        }

        public Markov(GameManager manager, IEnumerable<string> arguments)
        {
            if (arguments.Count() < 3)
                throw new Exception("Usage: markov max_white max_black deck_type [deck_arguments]");
            _maxWhite = int.Parse(arguments.ElementAt(0));
            _maxBlack = int.Parse(arguments.ElementAt(1));
            _deck = (IDeckType)GameManager.DeckTypes[arguments.ElementAt(2)]
                .GetConstructor(new Type[] { typeof(GameManager), typeof(IEnumerable<string>) })
                .Invoke(new object[] { manager, arguments.Skip(3) });

            while (_deck.WhiteCards > 0)
                _whiteGenerator.Feed(_deck.TakeWhiteCard().Parts.First().Split(' '));

            while (_deck.BlackCards > 0)
                _blackGenerator.Feed(_deck.TakeBlackCard().Parts);
        }
    }
}
