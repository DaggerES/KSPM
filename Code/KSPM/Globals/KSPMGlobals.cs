using KSPM.IO.Logging;
using KSPM.Diagnostics;
using KSPM.Network.Server;

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

        protected bool binaryEnabled;

        #endregion

        #region Server variables

        public RealTimer realTimer;

        protected GameServer gameServer;

        #endregion

        protected KSPMGlobals()
        {
            this.nullLogger = new DevNullLog();
            this.log = this.nullLogger;
            this.gameServer = null;
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
                    this.fileLogger = new FileLog(FileLog.GetAUniqueFilename("KSPMLog"), this.binaryEnabled);
                    this.log = this.fileLogger;
                    break;
                ///In this mode no other loggers will be created, so if you need to change the logging mode we have to create a method to handle that action.
                case Log.LogginMode.DevNull:
                    this.log = this.nullLogger;
                    break;
            }
        }

        public void SetServerReference(ref GameServer reference)
        {
            this.gameServer = reference;
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
    }
}
