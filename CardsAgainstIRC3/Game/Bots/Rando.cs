using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.Bots
{
    [Bot("rando")]
    public class Rando : IBot
    {
        public GameManager Manager
        {
            get;
            private set;
        }

        public GameUser User
        {
            get;
            private set;
        }

        public Rando(GameManager manager)
        {
            Manager = manager;
        }

        public void LinkedToUser(GameUser user)
        {
            User = user;
        }

        public Card[] ResponseToCard(Card blackCard)
        {
            return Manager.TakeWhiteCards(blackCard.Parts.Length - 1).ToArray();
        }
    }
}
