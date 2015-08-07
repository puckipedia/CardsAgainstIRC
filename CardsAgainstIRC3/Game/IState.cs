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

        public delegate void CommandDelegate(string user, IEnumerable<string> arguments);

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
                    _commands[command] = (CommandDelegate)method.CreateDelegate(typeof(CommandDelegate), this);
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

        [Command("!bots")]
        public void BotsCommand(string user, IEnumerable<string> args)
        {
            Manager.SendPrivate(user, "bots: {0}", string.Join(",", GameManager.Bots.Keys));
        }

        [Command("!cardsets")]
        public void CardSetsCommand(string user, IEnumerable<string> args)
        {
            Manager.SendPrivate(user, "cardsets: {0}", string.Join(",", GameManager.CardSetTypes.Keys));
        }

        public virtual bool UserLeft(GameUser user)
        {
            return true;
        }
    }
}
