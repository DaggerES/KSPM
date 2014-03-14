using System.Text;
using KSPM.Diagnostics;

namespace KSPM.IO.Logging
{
    /// <summary>
    /// Log which instead of printing the output  it will store it inside a buffer.
    /// </summary>
    public class BufferedLog : Log
    {
        /// <summary>
        /// Buffer in which is going to store all the output coming from WriteTo method.
        /// </summary>
        protected StringBuilder buffer;

        protected static readonly int MaximunLinesToBeBuffered = 10000;

        protected int maxBufferedLines;

        protected int printedLinesCounter;

        public BufferedLog()
        {
            this.buffer = new StringBuilder();
            this.maxBufferedLines = BufferedLog.MaximunLinesToBeBuffered;
            this.printedLinesCounter = 0;
        }

        public override void WriteTo(string message)
        {
            this.buffer.AppendLine(string.Format("{0}{1}", RealTimer.GetCurrentDateTime(), message));
            this.printedLinesCounter++;
            if (this.printedLinesCounter > this.maxBufferedLines)
            {
                this.Dispose();
            }
        }

        public override void Dispose()
        {
            this.buffer.Remove(0, this.buffer.Length);
        }
    }
}
