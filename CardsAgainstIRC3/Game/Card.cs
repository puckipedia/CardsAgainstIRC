using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game
{
    public struct Card
    {
        public string[] Parts
        {
            get;
            set;
        }

        public string Representation()
        {
            var part = string.Join("_", Parts);
            return "\x02[" + part + "]\x02";
        }

        public string Representation(IEnumerable<Card> cards)
        {
            var fil = cards.Select(a => a.Representation()).ToArray();

            var formatString = new StringBuilder();
            for(int i = 0; i < Parts.Length; i++)
            {
                formatString.Append(Parts[i]);
                if (i + 1 != Parts.Length)
                    formatString.Append(fil[i]);
            }

            for (int i = 0; i < fil.Length; i++)
            {
                formatString.Replace("$" + i, fil[i]);
            }

            return formatString.ToString();
        }
    }
}
