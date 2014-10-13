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

        /// <summary>
        /// Sets the amount of lines that the buffer will be hold before releases them to every reference.
        /// </summary>
        protected static readonly int MaximunLinesToBeBuffered = 10000;

        /// <summary>
        /// Amount of lines before the object releases the memory to 0.
        /// </summary>
        protected int maxBufferedLines;

        /// <summary>
        /// Amount of lines printed into the memory.
        /// </summary>
        protected int printedLinesCounter;

        /// <summary>
        /// Creates an empty BufferedLog.
        /// </summary>
        public BufferedLog()
        {
            this.buffer = new StringBuilder();
            this.maxBufferedLines = BufferedLog.MaximunLinesToBeBuffered;
            this.printedLinesCounter = 0;
        }

        /// <summary>
        /// Writes into the buffer.
        /// </summary>
        /// <param name="message"></param>
        public override void WriteTo(string message)
        {
            this.buffer.AppendLine(string.Format("{0}{1}", RealTimer.GetCurrentDateTime(), message));
            this.printedLinesCounter++;
            if (this.printedLinesCounter > this.maxBufferedLines)
            {
                this.buffer.Remove(0, this.buffer.Length);
            }
        }

        /// <summary>
        /// Disposes the buffer and set it to null, becoming unusable.
        /// </summary>
        public override void Dispose()
        {
            this.buffer.Remove(0, this.buffer.Length);
            this.buffer = null;
        }

        /// <summary>
        /// Gets the buffer as string, ready to print.
        /// </summary>
        public string Buffer
        {
            get
            {
                return this.buffer.ToString();
            }
        }
    }
}
