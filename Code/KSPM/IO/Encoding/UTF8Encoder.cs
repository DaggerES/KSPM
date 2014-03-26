using System.Text;
using KSPM.Network.Common;

namespace KSPM.IO.Encoding
{
    class UTF8Encoder : Encoder
    {
        protected static readonly UTF8Encoding Encoder = new UTF8Encoding();

        public override Error.ErrorType GetString(byte[] rawBytes, int offset, int bytesToRead, out string outputString)
        {
            outputString = null;
            if (rawBytes == null)
            {
                return Error.ErrorType.InvalidArray;
            }
            if (offset <= rawBytes.Length && offset + bytesToRead > rawBytes.Length)
            {
                bytesToRead = rawBytes.Length - offset;
            }
            try
            {
                outputString = UTF8Encoder.Encoder.GetString(rawBytes, offset, bytesToRead);
            }
            catch (System.Text.DecoderFallbackException)
            {
                return Error.ErrorType.ByteBadFormat;
            }
            catch (System.ArgumentException)
            {
                return Error.ErrorType.ByteBadFormat;
            }
            return Error.ErrorType.Ok;
        }

        public override Error.ErrorType GetBytes(string inputString, out byte[] rawBytes)
        {
            rawBytes = null;
            if (inputString == null)
            {
                return Error.ErrorType.ByteBadFormat;
            }
            try
            {
                rawBytes = UTF8Encoder.Encoder.GetBytes(inputString);
            }
            catch (System.Text.EncoderFallbackException)
            {
                return Error.ErrorType.ByteBadFormat;
            }
            return Error.ErrorType.Ok;
        }
    }
}
