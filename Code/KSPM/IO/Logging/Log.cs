using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPM.IO.Logging
{
    /// <summary>
    /// Base class to every log class on the system.
    /// </summary>
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

        /// <summary>
        /// Enum property to tell what kind of logger can be used on the system.
        /// </summary>
        public enum LogginMode : byte 
        { 
            /// <summary>
            /// None is logged.
            /// </summary>
            DevNull,

            /// <summary>
            /// Everything is written into a file.
            /// </summary>
            File,

            /// <summary>
            /// It uses the console output.
            /// </summary>
            Console,

            /// <summary>
            /// Logged into the memory.
            /// </summary>
            Buffered,

            /// <summary>
            /// Any class that inherits from Log and it is defined by the user.
            /// </summary>
            UserDefined
        };
    }
}
