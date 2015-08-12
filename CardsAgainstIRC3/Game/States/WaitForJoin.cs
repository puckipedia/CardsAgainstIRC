using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.States
{
    public class WaitForJoin : Base
    {
        public WaitForJoin(GameManager manager)
            : base(manager)
        { }

        [Command("!start")]
        public void StartCommand(string nick, IEnumerable<string> arguments)
        {
            if (Manager.Users < 3)
            {
                Manager.SendPublic(nick, "We don't have enough players!");
                return;
            }
            else if (Manager.CardSets.Sum(a => a.Item1.WhiteCards) == 0 || Manager.CardSets.Sum(a => a.Item1.BlackCards) == 0)
            {
                Manager.SendPublic(nick, "Not enough cards to start the game!");
                return;
            }

            Manager.SendToAll("Game is starting...");
            Manager.StartState(new ChoosingCards(Manager));
        }
    }
}
