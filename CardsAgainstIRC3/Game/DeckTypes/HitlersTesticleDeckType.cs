using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.DeckTypes
{
    [DeckType("hot")]
    class HitlersTesticleCardSet : IDeckType
    {
        public int BlackCards
        {
            get
            {
                return 100 - _blackCount;
            }
        }

        private int _blackCount = 0;
        private int _whiteCount = 0;

        public Card TakeBlackCard()
        {
            return new Card() { Parts = new string[] { "Hitler's ", " testicle #" + _blackCount++ } };
        }

        public string Description
        {
            get
            {
                return "Hitler's testicle deck";
            }
        }

        public int WhiteCards
        {
            get
            {
                return 100 - _whiteCount;
            }
        }

        public Card TakeWhiteCard()
        {
            return new Card() { Parts = new string[] { "Hitler's testicle #" + _whiteCount++ } };
        }

        public HitlersTesticleCardSet(GameManager manager, IEnumerable<string> args)
        {

        }
    }
}
