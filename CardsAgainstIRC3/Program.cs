using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3
{
    public class GameMain : Game.GameOutput, IDisposable
    {
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
            Client = new TcpClient(host, port);
            var stream = Client.GetStream();
            Reader = new StreamReader(stream);
            Writer = new StreamWriter(stream);
            Writer.AutoFlush = true;
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
                    Managers[msg.Arguments[0]] = new Game.GameManager(this, this, msg.Arguments[0]);
                }

                foreach(var mgr in Managers.Values)
                {
                    if (mgr.OnIRCMessage(msg))
                        break;
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
        }
    }
}
