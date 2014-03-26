using KSPM.Network.Common;

namespace KSPM.IO.Encoding
{
    public abstract class Encoder
    {
        public abstract Error.ErrorType GetString(byte[] rawBytes, int offset, int bytesToRead, out string outputString);
        public abstract Error.ErrorType GetBytes(string inputString, out byte[] rawBytes);
    }
}
