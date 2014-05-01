using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPM.IO.Logging
{
    public abstract class Log : IDisposable
    {
        /// <summary>
        /// Writes a message into the log.
        /// </summary>
        /// <param name="message"></param>
        public abstract void WriteTo(string message);

        /// <summary>
        /// Releases all the utilized resources.
        /// </summary>
        public abstract void Dispose();

        public enum LogginMode : byte { DevNull, File, Console, Buffered };
    }
}
