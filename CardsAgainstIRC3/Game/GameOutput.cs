using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstIRC3.Game
{
    public class GameOutput
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public virtual void SendToAll(string Channel, string Message, params object[] format)
        {
            logger.Info("Message to all in {0}: {1}", Channel, string.Format(Message, format));
        }

        public virtual void SendPublic(string Channel, string Nick, string Message, params object[] format)
        {
            logger.Info("public message to {0} in {1}: {2}", Nick, Channel, string.Format(Message, format));
        }

        public virtual void SendPrivate(string Channel, string Nick, string Message, params object[] format)
        {
            logger.Info("private message to {0} in {1}: {2}", Nick, Channel, string.Format(Message, format));
        }
    }
}
