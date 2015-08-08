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
        public IEnumerable<Card> BlackCards
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

        public WhiteCard(IEnumerable<string> arguments)
        {
            if (arguments.Count() != 2)
                throw new Exception("Usage: count \"card\"");
            int result;
            if (!int.TryParse(arguments.First(), out result))
                throw new Exception("Invalid int!");

            Repeat = result;
            Card = new Card() { Parts = new string[] { arguments.ElementAt(1) }};
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

        public IEnumerable<Card> WhiteCards
        {
            get
            {
                return Enumerable.Range(0, Repeat).Select(a => Card);
            }
        }
    }
}
