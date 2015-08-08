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
        public IEnumerable<Card> WhiteCards
        {
            get
            {
                return new Card[] { };
            }
        }

        public string Description
        {
            get
            {
                return string.Format("{0} of {1}", Repeat, Card.Representation());
            }
        }

        public BlackCard(IEnumerable<string> arguments)
        {
            if (arguments.Count() < 2)
                throw new Exception("Usage: count \"card\" \"card part 2\" (parts are seperated by _)");
            int result;
            if (!int.TryParse(arguments.First(), out result))
                throw new Exception("Invalid int!");

            Repeat = result;
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

        public IEnumerable<Card> BlackCards
        {
            get
            {
                return Enumerable.Range(0, Repeat).Select(a => Card);
            }
        }
    }
}
