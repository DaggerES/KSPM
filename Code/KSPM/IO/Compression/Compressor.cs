namespace KSPM.IO.Compression
{
    /// <summary>
    /// Base class of each compressor implemented on the KSPM
    /// </summary>
    public abstract class Compressor
    {
        /// <summary>
        /// Size of the buffer used by the compressors.
        /// </summary>
        protected static int MaxBufferSize = KSPM.Network.Server.ServerSettings.ServerBufferSize;

        /// <summary>
        /// Compress a byte array.<b>Abstract.</b>
        /// </summary>
        /// <param name="source">Byte array holding the information.</param>
        /// <param name="target">Byte array with the information already compressed.</param>
        /// <returns>Number of bytes that were compressed </returns>
        public abstract int Compress(ref byte[] source, out byte[] target);

        /// <summary>
        /// Decompress a chunk of bytes.
        /// </summary>
        /// <param name="source">Compressed byte array.</param>
        /// <param name="target">Decompressed information.</param>
        /// <returns>Number of bytes decompressed.</returns>
        public abstract int Decompress(ref byte[] source, out byte[] target);
    }
}
