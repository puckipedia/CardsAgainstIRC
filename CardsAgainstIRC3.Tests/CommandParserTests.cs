using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CardsAgainstIRC3.Game;

namespace CardsAgainstIRC3.Tests
{
    [TestClass]
    public class CommandParserTests
    {
        [TestMethod]
        public void ArgumentSplitting()
        {
            string to_parse = "test test1 test2";
            var parsed = GameManager.ParseCommandString(to_parse);
            Assert.AreEqual(to_parse, string.Join(" ", parsed));
        }

        [TestMethod]
        public void QuoteHandling()
        {
            string to_parse = "te\"st 1\" \"test 2\" 'test '\"3\"'";
            string[] expect = new string[] { "test 1", "test 2", "test 3" };
            string[] actual = GameManager.ParseCommandString(to_parse).ToArray();

            Assert.AreEqual(expect.Length, actual.Length);
            for (int i = 0; i < expect.Length; i++)
            {
                Assert.AreEqual(expect[i], actual[i]);
            }
        }
    }
}
