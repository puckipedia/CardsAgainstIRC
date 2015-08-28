using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.States
{
    public class Inactive : State
    {
        public Inactive(GameManager manager)
            : base(manager)
        { }


        [Command("!start")]
        public void StartCommand(CommandContext context, IEnumerable<string> arguments)
        {
            Manager.SendToAll("{0} started a game! | send !join to join!", context.Nick);
            var started = Manager.UserAdd(context.Nick);
            started.CanChooseCards = started.CanVote = true;
            Manager.UpdateCzars();
            Manager.Data["started"] = started;
            Manager.StartState(new WaitForJoin(Manager));
        }
    }
}
