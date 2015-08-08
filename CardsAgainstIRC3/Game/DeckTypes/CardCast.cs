using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.CardSets
{
    [DeckType("cardcast")]
    class CardCast : IDeckType
    {
        public IEnumerable<Card> WhiteCards
        {
            get;
            private set;
        }

        public IEnumerable<Card> BlackCards
        {
            get;
            private set;
        }

        public string Description
        {
            get
            {
                return string.Format("CardCast: {0} (by {1}, code {2})", Deck.name, Deck.author.username, Deck.code);
            }
        }

        public class Author
        {
            public string id { get; set; }
            public string username { get; set; }
        }

        public class DeckResponse
        {
            public string name { get; set; }
            public string code { get; set; }
            public string description { get; set; }
            public bool unlisted { get; set; }
            public string created_at { get; set; }
            public string updated_at { get; set; }
            public bool external_copyright { get; set; }
            public object copyright_holder_url { get; set; }
            public string category { get; set; }
            public string call_count { get; set; }
            public string response_count { get; set; }
            public Author author { get; set; }
            public string rating { get; set; }
        }

        public class CardCastCard
        {
            public string id { get; set; }
            public List<string> text { get; set; }
            public string created_at { get; set; }
        }

        public class CardsResponse
        {
            public List<CardCastCard> calls { get; set; }
            public List<CardCastCard> responses { get; set; }
        }

        DeckResponse Deck;

        public CardCast(IEnumerable<string> arguments)
        {
            if (arguments.Count() == 0)
                throw new Exception("Need cardcast code");
            Deck = GetDeckInfo(arguments.First());
            var cards = GetCards(arguments.First());

            List<Card> WhiteCards = new List<Card>();
            List<Card> BlackCards = new List<Card>();
            foreach (var card in cards.calls)
            {
                BlackCards.Add(new Card() { Parts = card.text.ToArray() });
            }
            foreach (var card in cards.responses)
            {
                WhiteCards.Add(new Card() { Parts = card.text.ToArray() });
            }

            this.WhiteCards = WhiteCards;
            this.BlackCards = BlackCards;
        }

        public static DeckResponse GetDeckInfo(string str)
        {
            var request = WebRequest.CreateHttp("https://api.cardcastgame.com/v1/decks/" + str);
            var response = request.GetResponse();
            return JsonConvert.DeserializeObject<DeckResponse>(new StreamReader(response.GetResponseStream()).ReadToEnd());
        }

        public static CardsResponse GetCards(string str)
        {
            var request = WebRequest.CreateHttp("https://api.cardcastgame.com/v1/decks/" + str + "/cards");
            var response = request.GetResponse();
            return JsonConvert.DeserializeObject<CardsResponse>(new StreamReader(response.GetResponseStream()).ReadToEnd());
        }
    }
}
