using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game
{
    public static class Utilities
    {
        public static bool IsTruthy(this string str)
        {
            var lower = str.ToLower();
            if (lower == "true" || lower == "1" || lower == "yes" || lower == "t" || lower == "#t")
                return true;

            return false;
        }
    }
}
