using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game
{
    /// <summary>
    /// A CardsAgainstIRC bot.
    /// </summary>
    public interface IBot
    {
        /// <summary>
        /// Called once the bot has been linked to a user
        /// </summary>
        /// <param name="user">The user this bot is using</param>
        void LinkedToUser(GameUser user);

        /// <summary>
        /// If a bot has CanChooseCards == true, respond with a list of cards.
        /// </summary>
        /// <param name="blackCard">The black card to respond to</param>
        /// <returns>A hand of cards to respond on here.</returns>
        Card[] ResponseToCard(Card blackCard);

        /// <summary>
        /// If a bot has CanVote == true, vote for a set of cards to win.
        /// </summary>
        /// <param name="cards">The card sets to choose from.</param>
        /// <returns></returns>
        int[] WinningCardSet(Card[][] cards);

        /// <summary>
        /// If this is true, the bot can act as a czar and WinningCardSet can be called.
        /// </summary>
        bool CanVote
        {
            get;
        }

        /// <summary>
        /// If this is true, the bot can choose cards and ResponseToCard will be called.
        /// </summary>
        bool CanChooseCards
        {
            get;
        }
    }
}
