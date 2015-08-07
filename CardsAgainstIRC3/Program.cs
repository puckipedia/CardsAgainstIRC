using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3
{
    public class IrcConfig
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("user")]
        public string User;

        [JsonProperty("gecos")]
        public string Gecos;

        [JsonProperty("pass")]
        public string Pass;

        [JsonProperty("server")]
        public string Server;

        [JsonProperty("port")]
        public int Port;
    }

    public class Config
    {
        [JsonProperty("irc")]
        public IrcConfig IRC
        {
            get;
            set;
        }

        [JsonProperty("cardsets")]
        public Dictionary<string, List<List<string>>> CardSets
        {
            get;
            set;
        }
        
        [JsonProperty("channels")]
        public List<string> Channels
        {
            get;
            set;
        }
    }

    public class GameMain : Game.GameOutput, IDisposable
    {
        public Config Config;

        public override void SendToAll(string Channel, string Message, params object[] format)
        {
            Send(new IRCMessage() { Command = "PRIVMSG", Arguments = new string[] { Channel, "\u200B" + string.Format(Message, format) } });
        }

        public override void SendPublic(string Channel, string Nick, string Message, params object[] format)
        {
            Send(new IRCMessage() { Command = "PRIVMSG", Arguments = new string[] { Channel, "\u200B" + Nick + ": " + string.Format(Message, format) } });
        }

        public override void SendPrivate(string Channel, string Nick, string Message, params object[] format)
        {
            Send(new IRCMessage() { Command = "NOTICE", Arguments = new string[] { Nick, "\u200B" + string.Format(Message, format) } });
        }

        public void Send(IRCMessage msg)
        {
            string tostr = msg.ToString();
            Console.WriteLine("> {0}", tostr);
            Writer.Write(tostr + "\r\n");
        }

        StreamReader Reader;
        StreamWriter Writer;
        TcpClient Client;
        Dictionary<string, Game.GameManager> Managers = new Dictionary<string, Game.GameManager>();

        public GameMain(string host, int port)
        {
            Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));

            Client = new TcpClient(Config.IRC.Server, Config.IRC.Port);
            BotName = Config.IRC.Name;

            var stream = Client.GetStream();
            Reader = new StreamReader(stream);
            Writer = new StreamWriter(stream);
            Writer.AutoFlush = true;

            if (Config.IRC.Pass != null)
                Send(new IRCMessage() { Command = "PASS", Arguments = new string[] { Config.IRC.Pass } });
            Send(new IRCMessage() { Command = "NICK", Arguments = new string[] { Config.IRC.Name } });
            Send(new IRCMessage() { Command = "USER", Arguments = new string[] { Config.IRC.User, "*", "*", Config.IRC.Gecos } });

            foreach (var channel in Config.Channels)
            {
                Send(new IRCMessage() { Command = "JOIN", Arguments = new string[] { channel } });
            }
        }

        public void Go()
        {
            while (true)
            {
                string Line = Reader.ReadLine();
                Console.WriteLine("< {0}", Line);
                IRCMessage msg = new IRCMessage(Line);

                if (msg.Command == "JOIN" && msg.Origin.Nick == BotName)
                {
                    Managers[msg.Arguments[0]] = Game.GameManager.CreateManager(this, this, msg.Arguments[0]);
                }

                if (msg.Command == "PING")
                {
                    msg.Command = "PONG";
                    Send(msg);
                    continue;
                }

                foreach(var mgr in Managers.Values)
                {
                    mgr.AddMessage(msg);
                }
            }
        }

        public string BotName
        {
            get;
            private set;
        } = "CardsAgainstIRC3";

        static void Main(string[] args)
        {
            var main = new GameMain("chat.freenode.net", 6667);
            main.Send(new IRCMessage() { Command = "NICK", Arguments = new string[] { main.BotName } });
            main.Send(new IRCMessage() { Command = "USER", Arguments = new string[] { main.BotName, "*", "*", main.BotName } });
            main.Send(new IRCMessage() { Command = "JOIN", Arguments = new string[] { "##ingsoc" } });
            main.Go();
        }

        public void Dispose()
        {
            Reader.Dispose();
            Writer.Dispose();
            Client.Close();
        }


        public override void DistinguishPeople(string Channel, IEnumerable<string> persons)
        {
            while (persons.Count() > 0)
            {
                var next = persons.Take(4);
                persons = persons.Skip(4);
                Send(new IRCMessage() { Command = "MODE", Arguments = (new string[] { Channel, "+vvvv" }).Concat(next).ToArray() });
            }
        }

        public override void UndistinguishPeople(string Channel, IEnumerable<string> persons)
        {
            while (persons.Count() > 0)
            {
                var next = persons.Take(4);
                persons = persons.Skip(4);
                Send(new IRCMessage() { Command = "MODE", Arguments = (new string[] { Channel, "-vvvv" }).Concat(next).ToArray() });
            }
        }
    }
}
