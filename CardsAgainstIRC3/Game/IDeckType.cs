using System.Collections.Generic;

namespace CardsAgainstIRC3.Game
{
    public interface IDeckType
    {
        Card TakeWhiteCard();

        Card TakeBlackCard();

        int WhiteCards
        {
            get;
        }

        int BlackCards
        {
            get;
        }

        string Description
        {
            get;
        }
    }
}