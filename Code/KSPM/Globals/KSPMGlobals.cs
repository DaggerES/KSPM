using KSPM.IO.Logging;
using KSPM.IO.Encoding;
using KSPM.Diagnostics;
using KSPM.Network.Server;
using KSPM.Network.NAT;

namespace KSPM.Globals
{
    /// <summary>
    /// Class to hold those properties required during the KSPM execution, such as the Loggers, server and client references.
    /// </summary>
    public class KSPMGlobals
    {
        /// <summary>
        /// Singletong pattern, so this is a static object reference.
        /// </summary>
        public static readonly KSPMGlobals Globals = new KSPMGlobals();

        #region Logging variables

        /// <summary>
        /// Polymorfic reference to a Log object.
        /// </summary>
        protected Log log;

        /// <summary>
        /// File logger reference used to write into a file.
        /// </summary>
        protected FileLog fileLogger;

        /// <summary>
        /// Default loggers, this prints into a console.
        /// </summary>
        protected ConsoleLog consoleLogger;

        /// <summary>
        /// Means that everything is going to be discarded.
        /// </summary>
        protected DevNullLog nullLogger;

        /// <summary>
        /// Binary or Text mode used on those file logs.
        /// </summary>
        protected Log.LogginMode loggingMode;

        /// <summary>
        /// This logger writes everything into a memory.
        /// </summary>
        protected BufferedLog bufferLogger;

        /// <summary>
        /// Says if the binary log is enabled.
        /// </summary>
        protected bool binaryEnabled;

        #endregion

        #region Server variables

        /// <summary>
        /// Reference to the GameServer, being accesible by any object on the server side.
        /// </summary>
        protected GameServer gameServer;

        /// <summary>
        /// Holds the NAT method used to connect through internet.
        /// </summary>
        protected NATTraversal natTraversingMethod;

        #endregion

        #region IO

        /// <summary>
        /// Encoder used to encode the strings in the system.<b>By default is used UTF8</b>
        /// </summary>
        protected Encoder stringEncoder;

        /// <summary>
        /// Path to the file used by the logger.
        /// </summary>
        protected string ioFilePath;

        #endregion

        /// <summary>
        /// Protected contructor used to create the singleton reference.
        /// </summary>
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
        /// If UserDefined mode is set you must call SetUserDefinedLogger( Log userDefinedLogger ) because the DevNull is set to avoid null references.
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
                case IO.Logging.Log.LogginMode.UserDefined:
                    this.log = nullLogger;
                    break;
                ///In this mode no other loggers will be created, so if you need to change the logging mode we have to create a method to handle that action.
                case Log.LogginMode.DevNull:
                    this.log = this.nullLogger;
                    break;
            }
        }

        /// <summary>
        /// Sets the NAT method to be used by the system.
        /// </summary>
        /// <param name="method"></param>
        public void SetNATTraversingMethod(NATTraversal method)
        {
            this.natTraversingMethod = method;
        }

        /// <summary>
        /// Sets the GameServer reference.
        /// </summary>
        /// <param name="reference"></param>
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

        /// <summary>
        /// Defines a new logger into the system.
        /// </summary>
        /// <param name="userDefinedLogger"></param>
        public void SetUserDefinedLogger( Log userDefinedLogger)
        {
            this.log = userDefinedLogger;
        }

        /// <summary>
        /// Gets the reference to the logger used on the system.
        /// </summary>
        public Log Log
        {
            get
            {
                return this.log;
            }
        }

        /// <summary>
        /// Gets the GameServer reference.
        /// </summary>
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

        /// <summary>
        /// Gets the Encoder used by the system.
        /// </summary>
        public Encoder StringEncoder
        {
            get
            {
                return this.stringEncoder;
            }
        }

        /// <summary>
        /// Gets the path to the file used by the logger.
        /// </summary>
        public string IOFilePath
        {
            get
            {
                return this.ioFilePath;
            }
        }

        /// <summary>
        /// Tells if the system is running using Mono or not.<b>Mono is used under Unix OS.</b>
        /// </summary>
        public bool IsRunningUnderMono
        {
            get
            {
                System.Type monoType = System.Type.GetType("Mono.Runtime");
                return monoType != null;
            }
        }
    }
}
