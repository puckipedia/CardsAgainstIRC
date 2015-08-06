using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CardsAgainstIRC3.Tests
{
    [TestClass]
    public class IRCMessageTests
    {
        [TestMethod]
        public void ParsesMessage()
        {
            string msg = "PRIVMSG ###cards :This is a test";
            IRCMessage expected_msg = new IRCMessage();
            expected_msg.Command = "PRIVMSG";
            expected_msg.Arguments = new string[] { "###cards", "This is a test" };

            IRCMessage parsed_msg = new IRCMessage(msg);

            Assert.AreEqual(expected_msg.Command, parsed_msg.Command);
            Assert.AreEqual(expected_msg.Arguments.Length, parsed_msg.Arguments.Length);

            for (int i = 0; i < expected_msg.Arguments.Length; i++)
            {
                Assert.AreEqual(expected_msg.Arguments[i], parsed_msg.Arguments[i]);
            }
        }

        [TestMethod]
        public void ParsesOrigin()
        {
            IRCMessageOrigin expected_origin = new IRCMessageOrigin();
            expected_origin.Host = "puckipedia.com";
            expected_origin.User = "puck";
            expected_origin.Nick = "puckipedia";

            IRCMessageOrigin parsed_origin = new IRCMessageOrigin("puckipedia!puck@puckipedia.com");

            Assert.AreEqual(expected_origin.Host, parsed_origin.Host);
            Assert.AreEqual(expected_origin.User, parsed_origin.User);
            Assert.AreEqual(expected_origin.Nick, parsed_origin.Nick);
        }
    }
}
