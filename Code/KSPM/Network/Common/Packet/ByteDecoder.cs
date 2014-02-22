
namespace KSPM.Network.Common.Packet
{
    public class ByteDecoder
    {
        /// <summary>
        /// Uncompress the bytes
        /// </summary>
        /// <param name="rawBytes"></param>
        /// <param name="uncompressedBytes"></param>
        /// <returns></returns>
        public static Error.ErrorType UncompressBytes(ref byte[] rawBytes, out byte[] uncompressedBytes)
        {
            uncompressedBytes = rawBytes;
            return Error.ErrorType.Ok;
        }
    }
}
