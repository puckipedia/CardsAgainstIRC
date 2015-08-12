using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.DeckTypes
{
    [DeckType("users")]
    class Users : IDeckType
    {
        public int BlackCards
        {
            get
            {
                return 0; 
            }
        }

        public string Description
        {
            get
            {
                return "One card per user";
            }
        }

        public HashSet<Guid> UsedNicks = new HashSet<Guid>();

        public int WhiteCards
        {
            get
            {
                return Manager.AllUsers.Count(a => !UsedNicks.Contains(a.Guid));
            }
        }

        public GameManager Manager
        {
            get;
            private set;
        }

        public Users(GameManager manager, IEnumerable<string> arguments)
        {
            Manager = manager;
        }

        public Card TakeBlackCard()
        {
            throw new NotImplementedException("Cannot take black card from this deck, lol");
        }

        private Random _random = new Random();

        public Card TakeWhiteCard()
        {
            var randomNick = Manager.AllUsers.Where(a => !UsedNicks.Contains(a.Guid)).OrderBy(a => _random.Next()).First();
            UsedNicks.Add(randomNick.Guid);
            return new Card() { Parts = new string[] { randomNick.Nick.Substring(0, 1) + "\u200b" + randomNick.Nick.Substring(1) } };
        }
    }
}
