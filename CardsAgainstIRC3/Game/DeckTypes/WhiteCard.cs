using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.DeckTypes
{
    [DeckType("white")]
    public class WhiteCard : IDeckType
    {
        public int BlackCards
        {
            get
            {
                return 0;
            }
        }

        public Card TakeBlackCard()
        {
            throw new NotImplementedException("WhiteCard cannot deposit any black cards");
        }

        public string Description
        {
            get
            {
                return string.Format("{0} of {1}", Repeat, Card.Representation());
            }
        }

        public WhiteCard(GameManager manager, IEnumerable<string> arguments)
        {
            if (arguments.Count() != 2)
                throw new Exception("Usage: count \"card\"");
            int result;
            if (!int.TryParse(arguments.First(), out result))
                throw new Exception("Invalid int!");

            Repeat = result;
            Card = new Card() { Parts = new string[] { arguments.ElementAt(1) }};
            _leftOver = Repeat;
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

        public int WhiteCards
        {
            get
            {
                return _leftOver;
            }
        }

        public Card TakeWhiteCard()
        {
            _leftOver--;
            return Card;
        }
    }
}
