namespace KSPM.IO.Logging
{
    /// <summary>
    /// Log class which Writes nothing, is basically a message > /dev/null
    /// </summary>
    public class DevNullLog : Log
    {
        /// <summary>
        /// Does nothing at all.
        /// </summary>
        public override void Dispose()
        {
        }

        /// <summary>
        /// Writes to /dev/null
        /// </summary>
        /// <param name="message"></param>
        public override void WriteTo(string message)
        {
        }
    }
}
