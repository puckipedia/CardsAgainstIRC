using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3
{
    public struct IRCMessageOrigin
    {
        public string User;
        public string Nick;
        public string Host;

        public IRCMessageOrigin(string data)
        {
            // nick!user@host
            if (data.Contains("!"))
            {
                var parts = data.Split('!', '@');
                Nick = parts[0];
                User = parts[1];
                Host = parts[2];
            }
            else if (data.Contains("."))
            {
                Nick = User = null;
                Host = data;
            }
            else
            {
                User = Host = null;
                Nick = data;
            }
        }

        public override string ToString()
        {
            if (Host != null && User == null && Nick == null)
                return Host;
            if (Nick != null && User == null && Host == null)
                return Nick;

            return string.Format("{0}!{1}@{2}", Nick, User, Host);
        }
    }

    public struct IRCMessage
    {
        public IRCMessageOrigin Origin;
        public string Command;
        public string[] Arguments;

        public IRCMessage(string message)
        {
            string[] splitUp = message.Split(' ');
            if (message.Length == 0)
            {
                Command = null;
                Origin = new IRCMessageOrigin();
                Arguments = new string[] { };
                return;
            }

            if (message[0] == ':')
            {
                Origin = new IRCMessageOrigin(splitUp[0].Substring(1));
                splitUp = splitUp.Skip(1).ToArray();
            }
            else
            {
                Origin = new IRCMessageOrigin();
            }

            if (splitUp.Length == 0)
            {
                Command = null;
                Arguments = new string[] { };
                return;
            }

            Command = splitUp[0];

            List<string> arguments = new List<string>();
            string lastArgument = null;
            foreach (var argument in splitUp.Skip(1))
            {
                if (lastArgument != null)
                    lastArgument += " " + argument;
                else if (argument[0] == ':')
                {
                    lastArgument = argument.Substring(1);
                }
                else
                    arguments.Add(argument);
            }

            if (lastArgument != null)
                arguments.Add(lastArgument);

            Arguments = arguments.ToArray();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            if (Origin.Nick != null || Origin.Host != null)
            {
                builder.Append(":");
                builder.Append(Origin.ToString());
                builder.Append(" ");
            }

            builder.Append(Command);

            foreach(var argument in Arguments)
            {
                builder.Append(" ");
                if (argument[0] == ':' || argument.Contains(" "))
                    builder.Append(":");
                builder.Append(argument);
            }

            return builder.ToString();
        }
    }
}
