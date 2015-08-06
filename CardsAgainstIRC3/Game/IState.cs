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

    public class State
    {
        public GameManager Manager
        {
            get;
            private set;
        }

        public delegate void CommandDelegate(GameUser user, IEnumerable<string> arguments);

        private Dictionary<string, CommandDelegate> _commands = new Dictionary<string, CommandDelegate>();

        public State(GameManager manager)
        {
            Manager = manager;

            var methods = this.GetType().GetMethods().Where(a => a.GetCustomAttributes(typeof(CommandAttribute), true).Length > 0);
            foreach (var method in methods)
            {
                var commands = method.GetCustomAttributes(typeof(CommandAttribute), true).Cast<CommandAttribute>().SelectMany(a => a.CommandNames);
                foreach (var command in commands)
                {
                    _commands[command] = (CommandDelegate) method.CreateDelegate(typeof(CommandDelegate), this);
                }
            }
        }

        public virtual void Activate()
        {
        }

        public virtual void Deactivate()
        {
        }

        internal virtual bool Command(string nick, string command, IEnumerable<string> arguments)
        {
            if (!_commands.ContainsKey(command))
                return false;

            _commands[command](Manager.Resolve(nick), arguments);

            return true;
        }

        [Command("!state")]
        public void StateCommand(GameUser user, IEnumerable<string> args)
        {
            Manager.SendPrivate(user, "Current state class is {0}", this.GetType());
        }
    }

    public class TestState : State
    {
        private Logger logger;

        public TestState(GameManager manager)
            : base(manager)
        {
            logger = LogManager.GetLogger("TestState<" + manager.Channel + ">");
        }

        public override void Activate()
        {
            logger.Trace("Test state activated");
        }

        [Command("!log")]
        public void LogCommand(GameUser user, IEnumerable<string> str)
        {
            logger.Trace(string.Join("|", str));
        }
    }

    public class BaseState : State {
        public BaseState(GameManager manager)
            : base(manager)
        { }
    }

    public class WaitForJoinState : BaseState
    {
        public WaitForJoinState(GameManager manager)
            : base(manager)
        { }

    }

public class InactiveState : State
    {
        public InactiveState(GameManager manager)
            : base(manager)
        { }

        internal override bool Command(string nick, string command, IEnumerable<string> arguments)
        {
            if (command == "!start")
            {
                Manager.SendToAll("{0} started a game! | send !join to join!", nick);
                var started = Manager.UserAdd(nick);
                Manager.Data["started"] = started;
                Manager.StartState(new WaitForJoinState(Manager));
                return true;
            }

            return base.Command(nick, command, arguments);
        }

    }
}
