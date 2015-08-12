using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.DeckTypes
{
    [DeckType("black")]
    public class BlackCard : IDeckType
    {
        public int WhiteCards
        {
            get
            {
                return 0;
            }
        }

        public Card TakeWhiteCard()
        {
            throw new NotImplementedException("BlackCard cannot deposit any white cards!");
        }

        public string Description
        {
            get
            {
                return string.Format("{0} of {1}", Repeat, Card.Representation());
            }
        }

        public BlackCard(GameManager manager, IEnumerable<string> arguments)
        {
            if (arguments.Count() < 2)
                throw new ArgumentException("arguments", "Usage: count \"card\" \"card part 2\" (parts are seperated by _)");
            int result;
            if (!int.TryParse(arguments.First(), out result))
                throw new ArgumentOutOfRangeException("arguments[0]", "Invalid int!");

            Repeat = result;
            _leftOver = Repeat;
            Card = new Card() { Parts = arguments.Skip(1).ToArray() };
        }

        public Card Card
        {
            get;
            private set;
        }

        public int Repeat
        {
            get;
            private set;
        }

        private int _leftOver;

        public int BlackCards
        {
            get
            {
                return _leftOver;
            }
        }

        public Card TakeBlackCard()
        {
            _leftOver--;
            return Card;
        }
    }
}
