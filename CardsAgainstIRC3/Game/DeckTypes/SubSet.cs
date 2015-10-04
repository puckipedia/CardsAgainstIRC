using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.DeckTypes
{
    [DeckType("subset")]
    class SubSet : IDeckType
    {
        private IDeckType _deck;
        int _maxWhite;
        int _maxBlack;

        public int WhiteCards
        {
            get
            {
                return Math.Min(_deck.WhiteCards, _maxWhite);
            }
        }

        public int BlackCards
        {
            get
            {
                return Math.Min(_deck.BlackCards, _maxBlack);
            }
        }

        public Card TakeWhiteCard()
        {
            var card = _deck.TakeWhiteCard();
            _maxWhite--;
            return card;
        }

        public Card TakeBlackCard()
        {
            var card = _deck.TakeBlackCard();
            _maxBlack--;
            return card;
        }

        public string Description
        {
            get
            {
                return string.Format("Subset: {0} white, {1} black of {2}", _maxWhite, _maxBlack, _deck.Description);
            }
        }

        public SubSet(GameManager manager, IEnumerable<string> arguments)
        {
            if (arguments.Count() < 3)
                throw new Exception("Usage: subset max_white max_black deck_type [deck_arguments][");
            _maxWhite = int.Parse(arguments.ElementAt(0));
            _maxBlack = int.Parse(arguments.ElementAt(1));
            _deck = (IDeckType) GameManager.DeckTypes[arguments.ElementAt(2)]
                .GetConstructor(new Type[] { typeof(GameManager), typeof(IEnumerable<string>) })
                .Invoke(new object[] { manager, arguments.Skip(3) });
        }
    }
}
