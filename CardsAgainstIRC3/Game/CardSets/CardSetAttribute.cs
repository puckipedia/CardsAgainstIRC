using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.CardSets
{

    [AttributeUsage(AttributeTargets.Class)]
    public class CardSetAttribute : Attribute
    {
        public string Name
        {
            get;
            private set;
        }

        public CardSetAttribute(string name)
        {
            Name = name;
        }
    }
}
