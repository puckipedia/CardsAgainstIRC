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

    public class CommandContext
    {
        public string Nick
        {
            get;
            set;
        }

        public enum CommandSource
        {
            PublicMessage,
            PrivateMessage,
        }

        public CommandSource Source
        {
            get;
            set;
        }
    }

    public class State
    {
        public GameManager Manager
        {
            get;
            private set;
        }

        public delegate void CommandDelegate(CommandContext context, IEnumerable<string> arguments);

        private Dictionary<string, CommandDelegate> _commands = new Dictionary<string, CommandDelegate>();
        private Dictionary<string, Dictionary<string, CommandDelegate>> _compoundCommands = new Dictionary<string, Dictionary<string, CommandDelegate>>();

        public void SendInContext(CommandContext context, string format, params object[] args)
        {
            if (context.Source == CommandContext.CommandSource.PrivateMessage)
                Manager.SendPrivate(context.Nick, format, args);
            else
                Manager.SendPublic(context.Nick, format, args);
        }

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
                    _commands[attribute.Name] = new CommandDelegate(delegate (CommandContext context, IEnumerable<string> arguments)
                    {
                        string command = arguments.FirstOrDefault() ?? "list";
                        if (_compoundCommands[attribute.Name].ContainsKey(command))
                            _compoundCommands[attribute.Name][command](context, arguments.Count() > 0 ? arguments.Skip(1) : new string[0]);
                    });
                }

                _commands[string.Format("{0}.{1}", attribute.Name, attribute.Subcommand)] = _compoundCommands[attribute.Name][attribute.Subcommand] = (CommandDelegate)compoundMethod.CreateDelegate(typeof(CommandDelegate), this);
            }
        }

        public virtual bool ReceivedMessage(CommandContext context, string message)
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

            return Command(context, first, arguments);
        }

        public virtual void Activate()
        {
        }

        public virtual void Deactivate()
        {
        }

        public virtual void Tick()
        { }

        internal virtual bool Command(CommandContext context, string command, IEnumerable<string> arguments)
        {
            if (!_commands.ContainsKey(command))
                return false;

            _commands[command](context, arguments);

            return true;
        }

        [Command("!state")]
        public void StateCommand(CommandContext context, IEnumerable<string> args)
        {
            SendInContext(context, "Current state class is {0}", this.GetType());
        }

        [Command("!kill")]
        public void KillCommand(CommandContext context, IEnumerable<string> args)
        {
            Manager.Reset();
        }

        [CompoundCommand("!bot", "types")]
        public void BotsCommand(CommandContext context, IEnumerable<string> args)
        {
            SendInContext(context, "bots: {0}", string.Join(",", GameManager.Bots.Keys));
        }

        [CompoundCommand("!deck", "types")]
        public void CardSetsCommand(CommandContext context, IEnumerable<string> args)
        {
            SendInContext(context, "cardsets: {0}", string.Join(",", GameManager.DeckTypes.Keys));
        }

        [Command("!fake")]
        public void FakeCommand(CommandContext context, IEnumerable<string> args)
        {
            if (!_canDebug.ContainsKey(context.Nick) || !_canDebug[context.Nick])
                return;

            foreach (var arg in args)
                Manager.AddMessage(new IRCMessage(arg));
        }

        [CompoundCommand("!command", "list")]
        public void CommandsCommand(CommandContext context, IEnumerable<string> args)
        {
            SendInContext(context, "Commands: {0}", string.Join(", ", _commands.Keys));
        }

        private static Dictionary<string, Guid> _debugKeys = new Dictionary<string, Guid>();
        private static Dictionary<string, bool> _canDebug = new Dictionary<string, bool>();
        private Engine _debugEngine = new Engine(a => a.AllowClr());
        private static Random _random = new Random();

        [Command("!debug")]
        public void DebugCommand(CommandContext context, IEnumerable<string> arguments)
        {
            if (arguments.Count() == 0)
            {
                if (!_debugKeys.ContainsKey(context.Nick))
                {
                    byte[] buffer = new byte[16];
                    _random.NextBytes(buffer);
                    _debugKeys[context.Nick] = new Guid(buffer);
                }
                Console.WriteLine("Debug key for {0}: {1}", context.Nick, _debugKeys[context.Nick]);
                return;
            }

            if (arguments.Count() == 1 && _debugKeys.ContainsKey(context.Nick) && (!_canDebug.ContainsKey(context.Nick) || !_canDebug[context.Nick]))
            {
                try
                {
                    _canDebug[context.Nick] = new Guid(arguments.First()) == _debugKeys[context.Nick];
                }
                catch (Exception)
                { _debugKeys.Remove(context.Nick); }

                if (_canDebug.ContainsKey(context.Nick) && _canDebug[context.Nick])
                    Console.WriteLine("Debug for {0} enabled", context.Nick);
                else
                    _debugKeys.Remove(context.Nick);
                return;
            }

            if (!_canDebug.ContainsKey(context.Nick) || !_canDebug[context.Nick])
            {
                return;
            }

            _debugEngine.SetValue("manager", this.Manager);

            foreach (var argument in arguments)
            {
                var obj = _debugEngine.Execute(argument).GetCompletionValue().ToObject();
                if (obj != null)
                    SendInContext(context, "{0}", obj);
            }
        }

        [Command("!as")]
        public void AsCommand(CommandContext context, IEnumerable<string> arguments)
        {
            if (!_canDebug.ContainsKey(context.Nick) || !_canDebug[context.Nick] || arguments.Count() < 2)
                return;

            ReceivedMessage(new CommandContext() { Nick = arguments.First(), Source = context.Source }, string.Join(" ", arguments.Skip(1)));
        }

        [Command("!undebug")]
        public void UndebugCommand(CommandContext context, IEnumerable<string> arguments)
        {
            _debugKeys.Remove(context.Nick);
            _canDebug.Remove(context.Nick);
        }
        public virtual bool UserLeft(GameUser user, bool voluntarily)
        {
            return true;
        }
    }
}
