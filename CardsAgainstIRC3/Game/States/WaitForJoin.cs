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
        public void StartCommand(CommandContext context, IEnumerable<string> arguments)
        {
            if (Manager.Users < 3 || Manager.AllUsers.Count(a => a.CanVote) < 2 || Manager.AllUsers.Count(a => a.CanChooseCards) < 2)
            {
                SendInContext(context, "We don't have enough players!");
                return;
            }
            else if (Manager.CardSets.Sum(a => a.Item1.WhiteCards) == 0 || Manager.CardSets.Sum(a => a.Item1.BlackCards) == 0)
            {
                SendInContext(context, "Not enough cards to start the game!");
                return;
            }

            Manager.SelectRandomCzar();

            Manager.SendToAll("Game is starting...");
            Manager.StartState(new ChoosingCards(Manager));
        }
    }
}
