using KSPM.Diagnostics;

namespace KSPM.IO.Logging
{
    /// <summary>
    /// Log class to write the log into the console output.
    /// </summary>
    public class ConsoleLog : Log
    {
        /// <summary>
        /// Write the given message into the console.
        /// </summary>
        /// <param name="message"></param>
        public override void WriteTo(string message)
        {
            System.Console.WriteLine(RealTimer.GetCurrentDateTime() + message);
        }

        public override void Dispose()
        {
        }
    }
}
