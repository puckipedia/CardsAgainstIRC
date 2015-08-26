using Jint;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string[] CommandNames
        {
            get;
            private set;
        }

        public CommandAttribute(params string[] command)
        {
            CommandNames = command;
        }
    }


    [AttributeUsage(AttributeTargets.Method)]
    public class CompoundCommandAttribute : Attribute
    {
        public string Name
        {
            get;
            private set;
        }

        public string Subcommand
        {
            get;
            private set;
        }

        public CompoundCommandAttribute(string name, string subcommand)
        {
            Name = name;
            Subcommand = subcommand;
        }
    }



    public class State
    {
        public GameManager Manager
        {
            get;
            private set;
        }

        public delegate void CommandDelegate(string user, IEnumerable<string> arguments);

        private Dictionary<string, CommandDelegate> _commands = new Dictionary<string, CommandDelegate>();
        private Dictionary<string, Dictionary<string, CommandDelegate>> _compoundCommands = new Dictionary<string, Dictionary<string, CommandDelegate>>();

        public State(GameManager manager)
        {
            Manager = manager;

            var methods = this.GetType().GetMethods().Where(a => a.GetCustomAttributes(typeof(CommandAttribute), true).Length > 0);
            foreach (var method in methods)
            {
                var commands = method.GetCustomAttributes(typeof(CommandAttribute), true).Cast<CommandAttribute>().SelectMany(a => a.CommandNames);
                foreach (var command in commands)
                {
                    _commands[command] = (CommandDelegate)method.CreateDelegate(typeof(CommandDelegate), this);
                }
            }

            var compoundMethods = this.GetType().GetMethods().Where(a => a.GetCustomAttributes(typeof(CompoundCommandAttribute), true).Length > 0);
            foreach (var compoundMethod in compoundMethods)
            {
                var attribute = compoundMethod.GetCustomAttributes(typeof(CompoundCommandAttribute), true).Cast<CompoundCommandAttribute>().First();
                if (!_commands.ContainsKey(attribute.Name))
                {
                    _compoundCommands[attribute.Name] = new Dictionary<string, CommandDelegate>();
                    _commands[attribute.Name] = new CommandDelegate(delegate (string user, IEnumerable<string> arguments)
                    {
                        string command = arguments.FirstOrDefault() ?? "list";
                        if (_compoundCommands[attribute.Name].ContainsKey(command))
                            _compoundCommands[attribute.Name][command](user, arguments.Count() > 0 ? arguments.Skip(1) : new string[0]);
                    });
                }

                _commands[string.Format("{0}.{1}", attribute.Name, attribute.Subcommand)] = _compoundCommands[attribute.Name][attribute.Subcommand] = (CommandDelegate)compoundMethod.CreateDelegate(typeof(CommandDelegate), this);
            }
        }

        public virtual bool ReceivedMessage(string nick, string message)
        {
            IEnumerable<string> parsed = GameManager.ParseCommandString(message);

            if (parsed.Count() < 1)
                return false;

            var first = parsed.First(); // ignore repeated !commands (!card !card 5)
            var arguments = parsed.SkipWhile(a => _commands.ContainsKey(a) && _commands[a] == _commands[first]);

            int result; // if a message is made up of (\d+ ?)+ try to parse it as a !card command
            if (_commands.ContainsKey("!card") && !parsed.Any(a => !int.TryParse(a, out result)))
            {
                first = "!card";
                arguments = parsed;
            }

            return Command(nick, first, arguments);
        }

        public virtual void Activate()
        {
        }

        public virtual void Deactivate()
        {
        }

        public virtual void Tick()
        { }

        internal virtual bool Command(string nick, string command, IEnumerable<string> arguments)
        {
            if (!_commands.ContainsKey(command))
                return false;

            _commands[command](nick, arguments);

            return true;
        }

        [Command("!state")]
        public void StateCommand(string user, IEnumerable<string> args)
        {
            Manager.SendPrivate(user, "Current state class is {0}", this.GetType());
        }

        [Command("!kill")]
        public void KillCommand(string user, IEnumerable<string> args)
        {
            Manager.Reset();
        }

        [CompoundCommand("!bot", "types")]
        public void BotsCommand(string user, IEnumerable<string> args)
        {
            Manager.SendPrivate(user, "bots: {0}", string.Join(",", GameManager.Bots.Keys));
        }

        [CompoundCommand("!deck", "types")]
        public void CardSetsCommand(string user, IEnumerable<string> args)
        {
            Manager.SendPrivate(user, "cardsets: {0}", string.Join(",", GameManager.DeckTypes.Keys));
        }

        [Command("!fake")]
        public void FakeCommand(string user, IEnumerable<string> args)
        {
            if (!_canDebug.ContainsKey(user) || !_canDebug[user])
                return;

            foreach (var arg in args)
                Manager.AddMessage(new IRCMessage(arg));
        }

        [CompoundCommand("!command", "list")]
        public void CommandsCommand(string user, IEnumerable<string> args)
        {
            Manager.SendPrivate(user, "Commands: {0}", string.Join(", ", _commands.Keys));
        }

        private static Dictionary<string, Guid> _debugKeys = new Dictionary<string, Guid>();
        private static Dictionary<string, bool> _canDebug = new Dictionary<string, bool>();
        private Engine _debugEngine = new Engine(a => a.AllowClr());
        private static Random _random = new Random();

        [Command("!debug")]
        public void DebugCommand(string nick, IEnumerable<string> arguments)
        {
            if (arguments.Count() == 0)
            {
                if (!_debugKeys.ContainsKey(nick))
                {
                    byte[] buffer = new byte[16];
                    _random.NextBytes(buffer);
                    _debugKeys[nick] = new Guid(buffer);
                }
                Console.WriteLine("Debug key for {0}: {1}", nick, _debugKeys[nick]);
                return;
            }

            if (arguments.Count() == 1 && _debugKeys.ContainsKey(nick) && (!_canDebug.ContainsKey(nick) || !_canDebug[nick]))
            {
                try
                {
                    _canDebug[nick] = new Guid(arguments.First()) == _debugKeys[nick];
                }
                catch (Exception)
                { _debugKeys.Remove(nick); }

                if (_canDebug.ContainsKey(nick) && _canDebug[nick])
                    Console.WriteLine("Debug for {0} enabled", nick);
                else
                    _debugKeys.Remove(nick);
                return;
            }

            if (!_canDebug.ContainsKey(nick) || !_canDebug[nick])
            {
                return;
            }

            _debugEngine.SetValue("manager", this.Manager);

            foreach (var argument in arguments)
            {
                var obj = _debugEngine.Execute(argument).GetCompletionValue().ToObject();
                if (obj != null)
                    Manager.SendPrivate(nick, "{0}", obj);
            }
        }

        [Command("!as")]
        public void AsCommand(string nick, IEnumerable<string> arguments)
        {
            if (!_canDebug.ContainsKey(nick) || !_canDebug[nick] || arguments.Count() < 2)
                return;

            ReceivedMessage(arguments.First(), string.Join(" ", arguments.Skip(1)));
        }

        [Command("!undebug")]
        public void UndebugCommand(string nick, IEnumerable<string> arguments)
        {
            _debugKeys.Remove(nick);
            _canDebug.Remove(nick);
        }
        public virtual bool UserLeft(GameUser user, bool voluntarily)
        {
            return true;
        }
    }
}
