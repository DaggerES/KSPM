using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPM.IO.Logging
{
    public class DiagnosticsLog : FileLog
    {
        public DiagnosticsLog(string logFileName, bool isBinaryLog)
            : base(logFileName, isBinaryLog)
        {
        }

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
