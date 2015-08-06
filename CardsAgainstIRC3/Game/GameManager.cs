using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game
{
    public class GameUser
    {
        private GameManager _manager;
        public GameUser(GameManager manager)
        {
            _manager = manager;
        }

        public void UpdateCards()
        {
            for (var i = 0; i < Cards.Length; i++)
                if (!Cards[i].HasValue)
                {
                    Cards[i] = _manager.TakeWhiteCard();
                }
        }

        public void RemoveCards(IEnumerable<int> cards = null)
        {
            if (cards == null)
                cards = ChosenCards;

            foreach (var card in cards)
                Cards[card] = null;
        }

        public string Nick = "";
        public Guid Guid = Guid.NewGuid();
        public Card?[] Cards = new Card?[10];
        public int[] ChosenCards = new int[0];

        public IBot Bot = null;
    }

    public class GameManager
    {
        public string Channel
        {
            get;
            private set;
        }

        private Stack<Card> _whiteCardStack = new Stack<Card>();
        private Stack<Card> _blackCardStack = new Stack<Card>();

        public Card TakeWhiteCard()
        {
            return _whiteCardStack.Pop();
        }

        public IEnumerable<Card> TakeWhiteCards(int count = 1)
        {
            for (; count > 0; count--)
                yield return TakeWhiteCard();
        }


        private enum CommandParserState
        {
            OutsideString,
            InDoubleString,
            InSingleString,
        }

        public static IEnumerable<string> ParseCommandString(string command)
        {
            StringBuilder storage = new StringBuilder();
            CommandParserState state = CommandParserState.OutsideString;
            bool escape = false;

            foreach (var chr in command)
            {
                if (escape)
                {
                    storage.Append(chr);
                    escape = false;
                }
                else if ((state == CommandParserState.OutsideString || state == CommandParserState.InDoubleString) && chr == '\\')
                {
                    escape = true;
                }
                else if (state == CommandParserState.InDoubleString && chr == '"')
                    state = CommandParserState.OutsideString;
                else if (state == CommandParserState.InSingleString && chr == '\'')
                    state = CommandParserState.OutsideString;
                else if (state == CommandParserState.OutsideString && chr == '"')
                    state = CommandParserState.InDoubleString;
                else if (state == CommandParserState.OutsideString && chr == '\'')
                    state = CommandParserState.InSingleString;
                else if (state == CommandParserState.OutsideString && chr == ' ')
                {
                    yield return storage.ToString();
                    storage.Clear();
                }
                else
                    storage.Append(chr);
            }

            if (storage.Length > 0)
                yield return storage.ToString();
        }
    }
}
