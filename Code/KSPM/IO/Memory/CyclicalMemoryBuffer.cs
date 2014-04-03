namespace KSPM.IO.Memory
{
    public class CyclicalMemoryBuffer
    {
        protected byte[][] buffers;
        protected uint[] usableBytes;
        protected uint size;
        protected uint buffersLength;
        protected uint writingIndex;
        protected uint readingIndex;

        /// <summary>
        /// Createas a cyclical array of buffers.
        /// </summary>
        /// <param name="size">How many buffers are going to be.</param>
        /// <param name="fixedBuffersLength">Size of each buffer.</param>
        public CyclicalMemoryBuffer(uint size, uint fixedBuffersLength)
        {
            this.size = size;
            this.buffersLength = fixedBuffersLength;
            this.writingIndex = this.readingIndex = 0;
            this.buffers = new byte[this.size][];
            this.usableBytes = new uint[this.size];
            for (int i = 0; i < this.size; i++)
            {
                this.buffers[i] = new byte[this.buffersLength];
                this.usableBytes[i] = 0;
            }
        }

        public uint Write(byte[] sourceArray, uint bytesToCopy)
        {
            System.Buffer.BlockCopy(sourceArray, 0, this.buffers[this.writingIndex], 0, (int)bytesToCopy);
            this.usableBytes[ this.writingIndex ] = bytesToCopy;
            this.writingIndex = (this.writingIndex + 1) % this.size;
            return bytesToCopy;
        }

        public uint Read(ref byte[] destArray)
        {
            uint worthyBytes = this.usableBytes[this.readingIndex];
            if (worthyBytes != 0)
            {
                System.Buffer.BlockCopy(this.buffers[this.readingIndex], 0, destArray, 0, (int)this.usableBytes[this.readingIndex]);
                this.usableBytes[this.readingIndex] = 0;
            }
            this.readingIndex = (this.readingIndex + 1) % this.size;
            return worthyBytes;
        }

        public void Release()
        {
            for (int i = 0; i < this.size; i++)
            {
                this.buffers[i] = null;
            }
            this.buffers = null;
            this.usableBytes = null;
        }

        public uint FixedLength
        {
            get
            {
                return this.buffersLength;
            }
        }
    }
}
