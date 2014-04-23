using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPM.Diagnostics
{
    public class Profiler
    {
        public System.Diagnostics.Stopwatch timer;
        public KSPM.IO.Logging.DiagnosticsLog logger;
        public long startMeasure;
        public long endMeasure;

        public Profiler(string fileName)
        {
            this.logger = new IO.Logging.DiagnosticsLog(KSPM.IO.Logging.DiagnosticsLog.GetAUniqueFilename(fileName), false);
            this.timer = new System.Diagnostics.Stopwatch();
            this.timer.Start();
            this.logger.WriteTo(string.Format("{0}", System.Diagnostics.Stopwatch.Frequency));
        }

        public void Set()
        {
            this.startMeasure = this.timer.ElapsedTicks;
        }

        public void Mark()
        {
            this.endMeasure = this.timer.ElapsedTicks;
            this.logger.WriteTo(string.Format("{0}", this.endMeasure - this.startMeasure));
        }

        public void Dispose()
        {
            this.logger.Dispose();
        }
    }
}
