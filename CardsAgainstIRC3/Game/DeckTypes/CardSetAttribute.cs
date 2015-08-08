using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.CardSets
{

    [AttributeUsage(AttributeTargets.Class)]
    public class DeckTypeAttribute : Attribute
    {
        public string Name
        {
            get;
            private set;
        }

        public DeckTypeAttribute(string name)
        {
            Name = name;
        }
    }
}
