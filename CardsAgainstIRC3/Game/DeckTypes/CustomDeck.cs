using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.DeckTypes
{
    [DeckType("custom")]
    public class CustomDeck : IDeckType
    {
        private Random _random = new Random();

        public Collection<Card> BlackCardList
        {
            get;
            set;
        } = new Collection<Card>();

        public Collection<Card> WhiteCardList
        {
            get;
            set;
        } = new Collection<Card>();

        public int BlackCards
        {
            get
            {
                return BlackCardList.Count;
            }
        }

        public string Description
        {
            get
            {
                return string.Format("Custom Deck: {0} white, {1} black", WhiteCards, BlackCards);
            }
        }

        public int WhiteCards
        {
            get
            {
                return WhiteCardList.Count;
            }
        }

        public Card TakeBlackCard()
        {
            int rnd = _random.Next(BlackCards);
            var card = BlackCardList[rnd];
            BlackCardList.RemoveAt(rnd);

            return card;
        }

        public Card TakeWhiteCard()
        {
            int rnd = _random.Next(WhiteCards);
            var card = WhiteCardList[rnd];
            WhiteCardList.RemoveAt(rnd);

            return card;
        }
    }
}
