namespace KSPM.IO.Compression
{
    /// <summary>
    /// Base class of each compressor implemented on the KSPM
    /// </summary>
    public abstract class Compressor
    {
        protected static int MaxBufferSize = KSPM.Network.Server.ServerSettings.ServerBufferSize;

        public abstract int Compress(ref byte[] source, out byte[] target);
        public abstract int Decompress(ref byte[] source, out byte[] target);
    }
}
