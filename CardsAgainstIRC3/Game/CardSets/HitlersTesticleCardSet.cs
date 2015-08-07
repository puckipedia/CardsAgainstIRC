using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.CardSets
{
    [CardSet("hot")]
    class HitlersTesticleCardSet : ICardSet
    {
        public IEnumerable<Card> BlackCards
        {
            get
            {
                return Enumerable.Range(0, 100).Select(a => new Card() { Parts = new string[] { "Hitler's ", " testicle #" + a } });
            }
        }

        public string Description
        {
            get
            {
                return "Hitler's testicle card set";
            }
        }

        public IEnumerable<Card> WhiteCards
        {
            get
            {
                return Enumerable.Range(0, 100).Select(a => new Card() { Parts = new string[] { "Hitler's testicle #" + a } });
            }
        }

        public HitlersTesticleCardSet(IEnumerable<string> args)
        {

        }
    }
}
