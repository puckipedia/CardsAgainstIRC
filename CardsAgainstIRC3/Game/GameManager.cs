using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        public bool UpdateCards()
        {
            for (var i = 0; i < Cards.Length; i++)
                if (!Cards[i].HasValue)
                {
                    try {
                        Cards[i] = _manager.TakeWhiteCard();
                    }
                    catch (InvalidOperationException)
                    {
                        return false;
                    }
                }

            return true;
        }

        public void RemoveCards(IEnumerable<int> cards = null)
        {
            if (cards == null)
                cards = ChosenCards;

            foreach (var card in cards)
                Cards[card] = null;
        }

        public void SendCards()
        {
            // :{me} NOTICE {user} :{ZWSP}{data}
            // 512 - 2 - len(": NOTICE  :") - 3 (ZWSP) - len(me) - len(user)

            int max_message_length = 512 - 2 - 11 - 3 - _manager.Me.ToString().Length - Nick.Length;
            List<string> currentSegments = new List<string>();
            int totalLength = 0;

            for (int i = 0; i < Cards.Length; i++)
            {
                var newSegment = string.Format("{0}: {1}", i, (!Cards[i].HasValue ? "<null>" : Cards[i].Value.Representation()));
                if (totalLength + 3 + newSegment.Length > max_message_length)
                {
                    _manager.SendPrivate(this, "{0}", string.Join(" | ", currentSegments));
                    totalLength = 0;
                    currentSegments.Clear();
                }

                currentSegments.Add(newSegment);
                totalLength += 3 + newSegment.Length;
            }

            if (currentSegments.Count > 0)
                _manager.SendPrivate(this, "{0}", string.Join(" | ", currentSegments));
        }

        public string Nick = "";
        public Guid Guid = Guid.NewGuid();
        public Card?[] Cards = new Card?[10];
        public int[] ChosenCards = new int[0];
        public int Points = 0;
        public bool HasVoted = false;
        public bool HasChosenCards = false;

        private bool? _canVote = null;
        private bool? _canChooseCards = null;
        public bool CanVote
        {
            get
            {
                return _canVote ?? (Bot == null ? false : Bot.CanVote);
            }

            set { _canVote = value; }
        }
        public bool CanChooseCards
        {
            get
            {
                return _canChooseCards ?? (Bot == null ? false : Bot.CanChooseCards);
            }

            set { _canChooseCards = value; }
        }
        public bool WantsToLeave = false;
        public string JoinReason = "";

        public IBot Bot = null;
    }

    public class GameManager
    {
        public enum GameMode
        {
            Czar,
            WinnnerIsCzar,
            SovietRussia
        }

        public GameMode Mode
        {
            get;
            set;
        } = GameMode.Czar;

        public IRCMessageOrigin Me
        {
            get;
            set;
        }

        private GameManager(GameMain main, GameOutput output, string Channel, IRCMessageOrigin me)
        {
            this._main = main;
            this._output = output;
            this.Channel = Channel;
            this.Me = me;

            _log = LogManager.GetLogger("GameManager<" + Channel + ">");

            if (Bots == null)
            {
                Bots = this.GetType().Assembly.GetTypes()
                    .Where(a => a.GetCustomAttributes(typeof(Bots.BotAttribute), false).Length > 0)
                    .ToDictionary(a => (a.GetCustomAttributes(typeof(Bots.BotAttribute), false).First() as Bots.BotAttribute).Name, a => a);
            }

            if (DeckTypes == null)
            {
                DeckTypes = this.GetType().Assembly.GetTypes()
                    .Where(a => a.GetCustomAttributes(typeof(DeckTypes.DeckTypeAttribute), false).Length > 0)
                    .ToDictionary(a => (a.GetCustomAttributes(typeof(DeckTypes.DeckTypeAttribute), false).First() as DeckTypes.DeckTypeAttribute).Name, a => a);
            }

            Reset();
            _output.SendToAll(Channel, "(Active.)");
        }

        public Dictionary<string, List<List<string>>> DefaultSets
        {
            get
            {
                return _main.Config.CardSets;
            }
        }

        public int Limit
        {
            get;
            set;
        }

        private Logger _log;
        private GameMain _main;
        private GameOutput _output;
        private Random _random = new Random();

        public List<Tuple<IDeckType, int>> CardSets
        {
            get;
            private set;
        } = new List<Tuple<IDeckType, int>>();

        private List<GameUser> _czarOrder = new List<GameUser>();
        private int _currentCzar = 0;

        public static Dictionary<string, Type> Bots
        {
            get;
            private set;
        } = null;

        public static Dictionary<string, Type> DeckTypes
        {
            get;
            private set;
        } = null;

        public Dictionary<string, object> Data
        {
            get;
            private set;
        } = new Dictionary<string, object>();

        private Dictionary<Guid, GameUser> _users = new Dictionary<Guid, GameUser>();
        private Dictionary<string, Guid> _userMap = new Dictionary<string, Guid>();

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

        public int Users
        {
            get
            {
                return _users.Count;
            }
        }

        public IEnumerable<GameUser> AllUsers
        {
            get
            {
                return _users.Values;
            }
        }

        public GameUser CurrentCzar()
        {
            return _czarOrder[_currentCzar];
        }

        public GameUser NextCzar()
        {
            _currentCzar = (_currentCzar + 1) % _czarOrder.Count;
            return _czarOrder[_currentCzar];
        }

        public delegate string PointsMetadata(GameUser user);

        public string GetPoints(PointsMetadata metadata)
        {
            List<string> data = new List<string>();
            foreach(var person in AllUsers)
            {
                data.Add(string.Format("{0}: {1}{2}", person.Nick, person.Points, metadata(person)));
            }

            return string.Join(" | ", data);
        }

        public void AddBot(string name, IBot bot)
        {
            var user = UserAdd("<" + name + ">");
            user.Bot = bot;
            user.CanVote = false;
            bot.LinkedToUser(user);
        }

        public void RemoveBot(string name)
        {
            var user = Resolve("<" + name + ">");
            if (user == null)
                return;
            if (CurrentState.UserLeft(user, true))
                UserQuit("<" + name + ">");
            else
                user.WantsToLeave = true;
        }

        public void UpdateCzars()
        {
            for (int i = 0; i < _czarOrder.Count; i++)
            {
                if (!_users.ContainsValue(_czarOrder[i]) || !_czarOrder[i].CanVote)
                {
                    if (_currentCzar >= i && _currentCzar > 0)
                        _currentCzar--;
                    _czarOrder.RemoveAt(i--);
                }
            }

            foreach (var person in AllUsers)
            {
                if (person.CanVote && !_czarOrder.Contains(person))
                    _czarOrder.Add(person);
            }
        }

        public void AddCardSet(IDeckType set, int weight = 1)
        {
            CardSets.Add(new Tuple<IDeckType, int>(set, weight));
        }

        public void RemoveCardSet(IDeckType set)
        {
            CardSets.RemoveAll(a => a.Item1 == set);
        }

        public void SendToAll(string Message, params object[] args)
        {
            _output.SendToAll(Channel, Message, args);
        }

        public void SendPublic(GameUser user, string Message, params object[] args)
        {
            if (user.Bot != null)
                return;
            _output.SendPublic(Channel, user.Nick, Message, args);
        }

        public void SendPrivate(GameUser user, string Message, params object[] args)
        {
            if (user.Bot != null)
                return;
            _output.SendPrivate(Channel, user.Nick, Message, args);
        }

        public void SendPublic(string user, string Message, params object[] args)
        {
            _output.SendPublic(Channel, user, Message, args);
        }

        public void SendPrivate(string user, string Message, params object[] args)
        {
            _output.SendPrivate(Channel, user, Message, args);
        }


        public void Reset()
        {
            StartState(new States.Inactive(this));
            _userMap.Clear();
            _output.UndistinguishPeople(Channel, _users.Where(a => a.Value.Bot == null).Select(a => a.Value.Nick));
            _users.Clear();
            CardSets.Clear();
            Data.Clear();
            Limit = 10;
        }

        public Card TakeWhiteCard()
        {
            int total_cards = CardSets.Sum(a => a.Item1.WhiteCards * a.Item2);
            if (total_cards == 0)
                throw new InvalidOperationException("No cards left!");

            int random_card = _random.Next(total_cards);
            int i = 0;
            foreach (var set in CardSets.OrderBy(a => _random.Next()))
            {
                i += set.Item1.WhiteCards * set.Item2;
                if (random_card < i)
                    return set.Item1.TakeWhiteCard();
            }

            return CardSets.Last().Item1.TakeWhiteCard();
        }

        public Card CurrentBlackCard
        {
            get;
            private set;
        } = new Card();

        public void NewBlackCard()
        {
            int total_cards = CardSets.Sum(a => a.Item1.BlackCards * a.Item2);
            int random_card = _random.Next(total_cards);
            int i = 0;
            foreach (var set in CardSets)
            {
                i += set.Item1.BlackCards * set.Item2;
                if (random_card < i)
                {
                    CurrentBlackCard = set.Item1.TakeBlackCard();
                    return;
                }
            }

            CurrentBlackCard = CardSets.Last().Item1.TakeBlackCard();
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
            if (!_users.ContainsKey(guid))
                return null;

            return _users[guid];
        }

        public void RenameUser(string from, string to)
        {
            var user = Resolve(from);
            if (user == null)
                return;

            user.Nick = to;
            _userMap[to] = user.Guid;
            _userMap.Remove(from);

            _log.Info("{0} just changed name to {1}", from, to);
        }

        public void UserQuit(string nick)
        {
            var user = Resolve(nick);
            if (user == null)
                return;

            _userMap.Remove(nick);
            _users.Remove(user.Guid);
            _output.UndistinguishPeople(Channel, new string[] { nick });
            _log.Info("{0} just quit!");
        }

        public GameUser UserAdd(string nick)
        {
            var user = new GameUser(this);
            user.Nick = nick;
            _users[user.Guid] = user;
            _userMap[nick] = user.Guid;
            if (!nick.StartsWith("<"))
                _output.DistinguishPeople(Channel, new string[] { nick });
            return user;
        }

        private void handleUserLeaving(GameUser user, bool voluntarily)
        {
            if (CurrentState != null)
                CurrentState.UserLeft(user, voluntarily);
            else
                UserQuit(user.Nick);
        }

        private bool OnIRCMessage(IRCMessage msg)
        {
            switch (msg.Command)
            {
                case "NICK":
                    RenameUser(msg.Origin.Nick, msg.Arguments[0]);
                    break;
                case "QUIT":
                    if (!_userMap.ContainsKey(msg.Origin.Nick))
                        break;
                    handleUserLeaving(Resolve(msg.Origin), false);
                    break;
                case "PART":
                    if (msg.Arguments[0] != Channel)
                        break;
                    if (!_userMap.ContainsKey(msg.Origin.Nick))
                        break;
                    handleUserLeaving(Resolve(msg.Origin), false);
                    break;
                case "KICK":
                    if (msg.Arguments[0] != Channel)
                        break;
                    if (!_userMap.ContainsKey(msg.Arguments[1]))
                        break;
                    handleUserLeaving(Resolve(msg.Arguments[1]), true);
                    break;
                case "PRIVMSG":
                case "NOTICE":
                    if ((msg.Arguments[0] == _main.BotName && _userMap.ContainsKey(msg.Origin.Nick)) || msg.Arguments[0] == Channel)
                    {
                        if (msg.Arguments[1][0] == '\u200B')
                            break; // ignore ZWSP

                        if (CurrentState == null)
                        {
                            _log.Error("Eek! CurrentState is null! This shouldn't happen D:");
                            break;
                        }

                        CurrentState.ReceivedMessage(new CommandContext() { Nick = msg.Origin.Nick, Source = msg.Arguments[0] == _main.BotName ? CommandContext.CommandSource.PrivateMessage : CommandContext.CommandSource.PublicMessage }, msg.Arguments[1]);
                    }
                    break;
            }
            return false;
        }

        private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
        private ConcurrentQueue<IRCMessage> _messages = new ConcurrentQueue<IRCMessage>();
        private Thread _runloopThread;
        private void Runloop()
        {
            DateTime start = DateTime.Now;
            while (true)
            {
                try {
                    IRCMessage msg;
                    if (_messages.Count > 0 && _messages.TryDequeue(out msg))
                    {
                        OnIRCMessage(msg);
                    }


                    CurrentState.Tick();
                } catch (Exception e)
                {
                    SendToAll("An exception occured, trying to recover...");
                    if (_main.Config.ResetOnException)
                        Reset();
                    Console.WriteLine(e);
                }
                _autoResetEvent.WaitOne(((int)(DateTime.Now - start).TotalMilliseconds) % 1000);
                _autoResetEvent.Reset();
            }
        }



        public static GameManager CreateManager(GameMain main, GameOutput output, string Channel, IRCMessageOrigin origin)
        {
            var manager = new GameManager(main, output, Channel, origin);

            Thread thread = new Thread(delegate () { manager.Runloop(); });
            manager._runloopThread = thread;

            thread.Start();
            return manager;
        }

        public void AddMessage(IRCMessage msg)
        {
            _messages.Enqueue(msg);
            _autoResetEvent.Set();
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
                    if (storage.Length > 0)
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
