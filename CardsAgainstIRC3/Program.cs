using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3
{
    public class GameMain
    {
        public string BotName
        {
            get;
            private set;
        } = "CaI";

        static void Main(string[] args)
        {
            var main = new GameMain();
            var manager = new Game.GameManager(main, new Game.GameOutput(), "#");
            while (true)
            {
                IRCMessage msg = new IRCMessage(Console.ReadLine());
                manager.OnIRCMessage(msg);
            }
        }
    }
}
