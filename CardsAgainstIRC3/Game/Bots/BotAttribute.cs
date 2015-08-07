using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game.Bots
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BotAttribute : Attribute
    {
        public string Name
        {
            get;
            private set;
        }

        public BotAttribute(string name)
        {
            Name = name;
        }
    }
}
