using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace CardsAgainstIRC3.Tests
{
    [TestFixture]
    public class IRCMessageTests
    {
        [Test(Description = "Tests if it can parse the command part of a message")]
        public void ParsesCommand()
        {
            string message = ":puckipedia!puck@irc.puckipedia.com PRIVMSG ###cards :!card 4 2";
            var parsed = new IRCMessage(message);

            Assert.AreEqual(parsed.Command, "PRIVMSG");
            Assert.AreEqual(parsed.Origin.Host, "irc.puckipedia.com", "Host is incorrect!");
            Assert.AreEqual(parsed.Origin.User, "puck", "User is incorrect!");
            Assert.AreEqual(parsed.Origin.Nick, "puckipedia", "Nick is incorrect!");
            Assert.AreEqual(2, parsed.Arguments.Length, "Invalid argument count");

            Assert.AreEqual("###cards", parsed.Arguments[0], "Parsed first argument incorrectly");
            Assert.AreEqual("!card 4 2", parsed.Arguments[1], "Parsed second argument incorrectly");
        }
        [Test(Description = "Tests if it can parse the arguments of a message")]
        public void ParsesArguments()
        {
            string message = ":puckipedia!puck@irc.puckipedia.com PRIVMSG ###cards :!card 4 2";
            var parsed = new IRCMessage(message);

            Assert.AreEqual(2, parsed.Arguments.Length, "Invalid argument count");

            Assert.AreEqual("###cards", parsed.Arguments[0], "Parsed first argument incorrectly");
            Assert.AreEqual("!card 4 2", parsed.Arguments[1], "Parsed second argument incorrectly");
        }
        [Test(Description = "Tests if it can parse the origin of a message")]
        public void ParsesOrigin()
        {
            string message = ":puckipedia!puck@irc.puckipedia.com PRIVMSG ###cards :!card 4 2";
            var parsed = new IRCMessage(message);

            Assert.AreEqual(parsed.Origin.Host, "irc.puckipedia.com", "Host is incorrect!");
            Assert.AreEqual(parsed.Origin.User, "puck", "User is incorrect!");
            Assert.AreEqual(parsed.Origin.Nick, "puckipedia", "Nick is incorrect!");
        }

        [Test(Description = "Does not explode with 0-length strings")]
        public void HandlesEmptyStrings()
        {
            var parsed = new IRCMessage("");

            Assert.AreEqual(parsed.Command, null, "Somehow parsed a command");
            Assert.AreEqual(parsed.Arguments.Length, 0, "Somehow parsed an argument");

            Assert.AreEqual(parsed.Origin.Host, null, "Somehow parsed a host");
            Assert.AreEqual(parsed.Origin.User, null, "Somehow parsed a user");
            Assert.AreEqual(parsed.Origin.Nick, null, "Somehow parsed a nick");
        }

        [Test(Description = "Does not explode with messages just containing an origin (presumes origin parses properly)")]
        public void HandlesJustOrigin()
        {
            var parsed = new IRCMessage(":puckipedia!puck@irc.puckipedia.com");

            Assert.AreEqual(parsed.Command, null, "Somehow parsed a command");
            Assert.AreEqual(parsed.Arguments.Length, 0, "Somehow parsed an argument");
        }

        [Test(Description = "Tries to parse an origin containing just a host")]
        public void ParsesServerOrigin()
        {
            var parsed = new IRCMessageOrigin("irc.puckipedia.com");

            Assert.AreEqual(parsed.Host, "irc.puckipedia.com");
            Assert.AreEqual(parsed.User, null, "Somehow parsed a user");
            Assert.AreEqual(parsed.Nick, null, "Somehow parsed a nick");
        }

        [Test(Description = "Tries to parse an origin containing just a nick")]
        public void ParsesNickOrigin()
        {
            var parsed = new IRCMessageOrigin("puckipedia");

            Assert.AreEqual(parsed.Nick, "puckipedia");
            Assert.AreEqual(parsed.User, null, "Somehow parsed a user");
            Assert.AreEqual(parsed.Host, null, "Somehow parsed a host");
        }

        [Test(Description = "Synthesizes a message back into a string properly")]
        public void MessageToString()
        {
            string message = ":puckipedia!puck@irc.puckipedia.com PRIVMSG ###cards :!card 4 2";
            var parsed = new IRCMessage(message);
            var tostring = parsed.ToString();

            Assert.AreEqual(message, tostring);
        }
    }
}
