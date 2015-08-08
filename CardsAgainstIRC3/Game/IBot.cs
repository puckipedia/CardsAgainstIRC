using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game
{
    public interface IBot
    {
        void LinkedToUser(GameUser user);
        Card[] ResponseToCard(Card blackCard);
    }
}
