using KSPM.Network.Common;

namespace KSPM.IO.Encoding
{
    /// <summary>
    /// Base class to every Encoder on the system.
    /// </summary>
    public abstract class Encoder
    {
        /// <summary>
        /// Decodes a byte array into a string.<b>Abstract.</b>
        /// </summary>
        /// <param name="rawBytes">Byte array with the information in raw format.</param>
        /// <param name="offset">From where the encoder must start to decode de information.</param>
        /// <param name="bytesToRead">Number of bytes to read.</param>
        /// <param name="outputString">Out reference to the string to be filled with the information.</param>
        /// <returns>ErrorType.Ok if everything goes well, error code otherwise.</returns>
        public abstract Error.ErrorType GetString(byte[] rawBytes, int offset, int bytesToRead, out string outputString);

        /// <summary>
        /// Encode a given string into a byte array.<b>Abstract.</b>
        /// </summary>
        /// <param name="inputString">Original string holding the information.</param>
        /// <param name="rawBytes">Out reference to the byte array that will hold the encoded information.</param>
        /// <returns>ErrorType.Ok if everything goes well, error code otherwise.</returns>
        public abstract Error.ErrorType GetBytes(string inputString, out byte[] rawBytes);
    }
}
