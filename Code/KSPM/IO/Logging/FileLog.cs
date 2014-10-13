using System;
using System.Text;
using System.IO;
using KSPM.Diagnostics;

namespace KSPM.IO.Logging
{

    /// <summary>
    /// Writes messages into a file either in binary or text mode using the UTF8 enconding.
    /// </summary>
    public class FileLog : Log
    {
        /// <summary>
        /// If it is false the log will be written in text style for human read, by default set to false.
        /// </summary>
        protected bool binaryLogEnabled;

        /// <summary>
        /// Name of the filename where the log will be written, it must contain the path either.
        /// </summary>
        protected string logFileName;

        /// <summary>
        /// A stream which will hold the log messages
        /// </summary>
        protected FileStream logOutputStream;

        /// <summary>
        /// Writer used to save as text mode.
        /// </summary>
        protected StreamWriter logTextWriter;

        /// <summary>
        /// Writer used to save as binary mode.
        /// </summary>
        protected BinaryWriter logBinaryWriter;

        /// <summary>
        /// Creates a FileLog object using the UTF8 encoding to to that.
        /// </summary>
        /// <param name="logFileName">The name of the file which will contain the messages.</param>
        /// <param name="isBinaryLog">Tells if the file would be written using a binary mode or a text mode</param>
        public FileLog(string logFileName, bool isBinaryLog)
        {
            this.logFileName = logFileName;
            this.binaryLogEnabled = isBinaryLog;
            try
            {
                this.logOutputStream = new FileStream(this.logFileName, FileMode.Create, FileAccess.Write);
                if (this.binaryLogEnabled)
                {
                    this.logBinaryWriter = new BinaryWriter(this.logOutputStream, UTF8Encoding.UTF8);
                    this.logTextWriter = null;
                }
                else
                {
                    this.logTextWriter = new StreamWriter(this.logOutputStream, UTF8Encoding.UTF8);
                    this.logBinaryWriter = null;
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Sets or Gets the binary log value.
        /// </summary>
        public bool IsBinaryLog
        {
            get
            {
                return this.binaryLogEnabled;
            }
        }
        /// <summary>
        /// Returns an unique filename merged with the given baseName, it uses the DateTime to create the returned name.
        /// </summary>
        /// <param name="baseName">The basename to be used, if one is provided.</param>
        /// <returns>An unique filename</returns>
        public static string GetAUniqueFilename(string baseName)
        {
            DateTime dateTime = DateTime.Now;
            string fileName;
            if (baseName == null)
            {
                fileName = String.Format("{0}_{1}.log", dateTime.ToString("dd-MM-yyyy"), dateTime.ToFileTime().ToString());
            }
            else
            {
                fileName = String.Format("{0}_{1}_{2}.log", baseName, dateTime.ToString("dd-MM-yyyy"), dateTime.ToFileTime().ToString());
            }
            return fileName;
        }

        /// <summary>
        /// Write the message into the log either in text mode or binary mode.
        /// </summary>
        /// <param name="message">Message to be written into the log.</param>
        public override void WriteTo(string message)
        {
            if (this.binaryLogEnabled)
            {
                this.logBinaryWriter.Write( RealTimer.GetCurrentDateTime() + message);
                this.logBinaryWriter.Flush();
            }
            else
            {
                this.logTextWriter.WriteLine( RealTimer.GetCurrentDateTime() + message);
                this.logTextWriter.Flush();
            }
        }

        /// <summary>
        /// Dispose and releases all the resources, closes the writers nicely.
        /// </summary>
        public override void Dispose()
        {
            if (this.binaryLogEnabled)
            {
                this.logBinaryWriter.Flush();
                this.logBinaryWriter.Close();
            }
            else
            {
                this.logTextWriter.Flush();
                this.logTextWriter.Close();
            }
            if (this.logOutputStream != null)
            {
                this.logOutputStream.Flush();
                this.logOutputStream.Close();
            }
        }
    }
}
