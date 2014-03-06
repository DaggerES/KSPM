using KSPM.IO.Logging;

namespace KSPM.Network.Common.Information
{
    public abstract class Status
    {
        public enum SystemStatus : byte { None, Running, Stoped };

        protected SystemStatus currentStatus;

        public abstract void WriteToLog(Log output);
    }
}
