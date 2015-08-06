using NLog;
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
        public GameManager(GameMain main, GameOutput output, string Channel)
        {
            this.main = main;
            this.output = output;
            this.Channel = Channel;
            logger = LogManager.GetLogger("GameManager<" + Channel + ">");

            Reset();
        }

        private Logger logger;
        private GameMain main;
        private GameOutput output;

        public string Channel
        {
            get;
            private set;
        }

        public State CurrentState
        {
            get;
            private set;
        }

        public void SendToAll(string Message, params object[] args)
        {
            output.SendToAll(Channel, Message, args);
        }

        public void SendPublic(GameUser user, string Message, params object[] args)
        {
            if (user.Bot != null)
                return;
            output.SendPublic(Channel, user.Nick, Message, args);
        }

        public void SendPrivate(GameUser user, string Message, params object[] args)
        {
            if (user.Bot != null)
                return;
            output.SendPrivate(Channel, user.Nick, Message, args);
        }

        public void Reset()
        {
            StartState(new InactiveState(this));
            _whiteCardStack.Clear();
            _blackCardStack.Clear();
            _userMap.Clear();
            _users.Clear();
            Data.Clear();
        }

        public Dictionary<string, object> Data
        {
            get;
            private set;
        } = new Dictionary<string, object>();
        
        private Stack<Card> _whiteCardStack = new Stack<Card>();
        private Stack<Card> _blackCardStack = new Stack<Card>();

        private Dictionary<Guid, GameUser> _users = new Dictionary<Guid, GameUser>();
        private Dictionary<string, Guid> _userMap = new Dictionary<string, Guid>();

        public Card TakeWhiteCard()
        {
            return _whiteCardStack.Pop();
        }

        public IEnumerable<Card> TakeWhiteCards(int count = 1)
        {
            for (; count > 0; count--)
                yield return TakeWhiteCard();
        }

        public void StartState(State state)
        {
            if (CurrentState != null)
                CurrentState.Deactivate();
            CurrentState = state;
            state.Activate();
        }
        
        private enum CommandParserState
        {
            OutsideString,
            InDoubleString,
            InSingleString,
        }

        public GameUser Resolve(IRCMessageOrigin origin)
        {
            return Resolve(origin.Nick);
        }

        public GameUser Resolve(string nick)
        {
            if (nick == null)
                return null;

            if (!_userMap.ContainsKey(nick))
                return null;

            return _users[_userMap[nick]];
        }

        public GameUser Resolve(Guid guid)
        {
            if (guid == null)
                return null;

            if (!_users.ContainsKey(guid))
                return null;

            return _users[guid];
        }

        private void renameUser(string from, string to)
        {
            var user = Resolve(from);
            if (user == null)
                return;

            user.Nick = to;
            _userMap[to] = user.Guid;
            _userMap.Remove(from);

            logger.Info("{0} just changed name to {1}", from, to);
        }

        public void UserQuit(string nick)
        {
            var user = Resolve(nick);
            if (user == null)
                return;

            logger.Info("{0} just quit!");
        }

        public GameUser UserAdd(string nick)
        {
            var user = new GameUser(this);
            user.Nick = nick;
            _users[user.Guid] = user;
            _userMap[nick] = user.Guid;

            return user;
        }

        public bool OnIRCMessage(IRCMessage msg)
        {
            switch (msg.Command)
            {
                case "NICK":
                    renameUser(msg.Origin.Nick, msg.Arguments[1]);
                    break;
                case "QUIT":
                    UserQuit(msg.Origin.Nick);
                    break;
                case "PART":
                    if (msg.Arguments[0] != Channel)
                        break;
                    UserQuit(msg.Origin.Nick);
                    break;
                case "PRIVMSG":
                case "NOTICE":
                    if ((msg.Arguments[0] == main.BotName && _userMap.ContainsKey(msg.Origin.Nick)) || msg.Arguments[0] == Channel)
                    {
                        if (msg.Arguments[1][0] == '\u200B')
                            break; // ignore ZWSP

                        var parsed_data = ParseCommandString(msg.Arguments[1]);
                        var command = parsed_data.First();
                        if (CurrentState == null)
                        {
                            logger.Error("Eek! CurrentState is null! This shouldn't happen D:");
                            break;
                        }

                        CurrentState.Command(msg.Origin.Nick, command, parsed_data.Skip(1));
                    }
                    break;
            }
            return false;
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
