using System.Collections.Generic;

namespace CardsAgainstIRC3.Game
{
    public interface ICardSet
    {
        IEnumerable<Card> WhiteCards
        {
            get;
        }
        IEnumerable<Card> BlackCards
        {
            get;
        }

        string Description
        {
            get;
        }
    }
}