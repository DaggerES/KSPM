using KSPM.IO.Logging;
using KSPM.IO.Encoding;
using KSPM.Diagnostics;
using KSPM.Network.Server;
using KSPM.Network.NAT;

namespace KSPM.Globals
{
    public class KSPMGlobals
    {

        public static readonly KSPMGlobals Globals = new KSPMGlobals();

        #region Logging variables

        protected Log log;
        protected FileLog fileLogger;
        protected ConsoleLog consoleLogger;
        protected DevNullLog nullLogger;
        protected Log.LogginMode loggingMode;
        protected BufferedLog bufferLogger;

        protected bool binaryEnabled;

        #endregion

        #region Server variables

        protected GameServer gameServer;

        protected NATTraversal natTraversingMethod;

        #endregion

        #region IO

        protected Encoder stringEncoder;

        protected string ioFilePath;

        #endregion

        protected KSPMGlobals()
        {
            this.nullLogger = new DevNullLog();
            this.log = this.nullLogger;
            this.gameServer = null;

            this.natTraversingMethod = new NATNone();

            this.stringEncoder = new UTF8Encoder();
            this.ioFilePath = string.Format(".{0}config{1}", System.IO.Path.DirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);

            RealTimer.Timer.Start();
        }

        /// <summary>
        /// Initializes the loggers and set the proper one according to the given parameters. If a DevNull mode is set at the beginning, the other logging modes become unavailable.
        /// </summary>
        /// <param name="loggingMode">Determines what kind of logging mode will be used to perform all the outputs.</param>
        /// <param name="isBinaryEnabled">Determines if the binary writing is enabled when the logging mode is set to File mode, otherwise it has no effect.</param>
        public void InitiLogging( Log.LogginMode loggingMode, bool isBinaryEnabled )
        {
            this.loggingMode = loggingMode;
            this.binaryEnabled = isBinaryEnabled;
            switch (this.loggingMode)
            {
                case Log.LogginMode.Console:
                    this.fileLogger = null;
                    this.consoleLogger = new ConsoleLog();
                    this.log = this.consoleLogger;
                    break;
                case Log.LogginMode.File:
                    this.consoleLogger = null;
                    this.fileLogger = new FileLog(this.ioFilePath + FileLog.GetAUniqueFilename("KSPMLog"), this.binaryEnabled);
                    this.log = this.fileLogger;
                    break;
                case Log.LogginMode.Buffered:
                    this.consoleLogger = null;
                    this.fileLogger = null;
                    this.bufferLogger = new BufferedLog();
                    this.log = this.bufferLogger;
                    break;
                ///In this mode no other loggers will be created, so if you need to change the logging mode we have to create a method to handle that action.
                case Log.LogginMode.DevNull:
                    this.log = this.nullLogger;
                    break;
            }
        }

        public void SetNATTraversingMethod(NATTraversal method)
        {
            this.natTraversingMethod = method;
        }

        public void SetServerReference(ref GameServer reference)
        {
            this.gameServer = reference;
        }

        /// <summary>
        /// Changes the default IO file path, so be careful when you call this method.<b>Use normal slash '/' as separator, and add one '/' at the end.</b>
        /// </summary>
        /// <param name="newPath">New path to the IO folder where all files are going to be written/read.</param>
        public void ChangeIOFilePath(string newPath)
        {
            string normalizedPath = newPath.Replace('/', System.IO.Path.DirectorySeparatorChar);
            this.ioFilePath = normalizedPath;
        }

        public Log Log
        {
            get
            {
                return this.log;
            }
        }

        public GameServer KSPMServer
        {
            get
            {
                return this.gameServer;
            }
        }

        /// <summary>
        /// Gets the NAT traversing method used by the KSPM model. <b>By default it is set to None.</b>
        /// </summary>
        public NATTraversal NAT
        {
            get
            {
                return this.natTraversingMethod;
            }
        }

        public Encoder StringEncoder
        {
            get
            {
                return this.stringEncoder;
            }
        }

        public string IOFilePath
        {
            get
            {
                return this.ioFilePath;
            }
        }
    }
}
