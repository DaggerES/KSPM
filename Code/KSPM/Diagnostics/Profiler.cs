using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPM.Diagnostics
{
    /// <summary>
    /// Class to create some metrics.
    /// </summary>
    public class Profiler
    {
        /// <summary>
        /// High presition timer.
        /// </summary>
        public System.Diagnostics.Stopwatch timer;

        /// <summary>
        /// Logger to write the results.
        /// </summary>
        public KSPM.IO.Logging.DiagnosticsLog logger;

        /// <summary>
        /// Time snapshot.
        /// </summary>
        public long startMeasure;

        /// <summary>
        /// Time snapshot taken when you want to measure the passed time.
        /// </summary>
        public long endMeasure;

        /// <summary>
        /// Creates a new Profiler and writes it down into a file.
        /// </summary>
        /// <param name="fileName">Filename </param>
        public Profiler(string fileName)
        {
            this.logger = new IO.Logging.DiagnosticsLog(KSPM.IO.Logging.DiagnosticsLog.GetAUniqueFilename(fileName), false);
            this.timer = new System.Diagnostics.Stopwatch();
            this.timer.Start();
            this.logger.WriteTo(string.Format("{0}", System.Diagnostics.Stopwatch.Frequency));
        }

        /// <summary>
        /// Sets the profiler to start a new measuring.
        /// </summary>
        public void Set()
        {
            this.startMeasure = this.timer.ElapsedTicks;
        }

        /// <summary>
        /// Sets the end of the measurement and writes down the result.
        /// </summary>
        public void Mark()
        {
            this.endMeasure = this.timer.ElapsedTicks;
            this.logger.WriteTo(string.Format("{0}", this.endMeasure - this.startMeasure));
        }

        /// <summary>
        /// Diposes the logger.
        /// </summary>
        public void Dispose()
        {
            this.logger.Dispose();
            this.timer.Stop();
        }
    }
}
