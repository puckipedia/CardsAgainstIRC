using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace CardsAgainstIRC3.Tests
{
    [TestFixture]
    public class CommandParserTests
    {
        [Test(Description ="Tests if the command parser works properly with \"\" and ''")]
        public void CommandParserSplitting()
        {
            string[] expected = new string[] { "test 1", "test 2", "test 3" };
            string from = "test\" \"1 tes't '2    't'e's't\\ 3 ";
            string[] actual = Game.GameManager.ParseCommandString(from).ToArray();

            Assert.AreEqual(string.Join("|", expected), string.Join("|", actual), "Parsing the command string failed!");
        }
    }
}
