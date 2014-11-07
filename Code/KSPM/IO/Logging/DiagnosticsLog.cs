using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPM.IO.Logging
{
    /// <summary>
    /// Class to write performance logs into a file.
    /// </summary>
    public class DiagnosticsLog : FileLog
    {
        /// <summary>
        /// Creates a new Logger.
        /// </summary>
        /// <param name="logFileName">Filename to be written into.</param>
        /// <param name="isBinaryLog"></param>
        public DiagnosticsLog(string logFileName, bool isBinaryLog)
            : base(logFileName, isBinaryLog)
        {
        }

        /// <summary>
        /// Writes down into the file.
        /// </summary>
        /// <param name="message"></param>
        public override void WriteTo(string message)
        {
            if (this.binaryLogEnabled)
            {
                this.logBinaryWriter.Write(message);
                this.logBinaryWriter.Flush();
            }
            else
            {
                this.logTextWriter.WriteLine(message);
                this.logTextWriter.Flush();
            }
        }
    }
}
