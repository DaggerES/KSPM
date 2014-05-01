using System.Text;
using KSPM.Diagnostics;

namespace KSPM.IO.Logging
{
    public class WebLog : BufferedLog
    {
        public override void WriteTo(string message)
        {
            this.buffer.AppendLine(string.Format("{0}{1}", RealTimer.GetCurrentDateTime(), message));
            this.printedLinesCounter++;
            if (this.printedLinesCounter > this.maxBufferedLines)
            {
                this.Dispose();
            }
        }
    }
}
