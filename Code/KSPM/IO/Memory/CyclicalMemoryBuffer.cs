namespace KSPM.IO.Memory
{
    /// <summary>
    /// Cyclical byte buffer array.
    /// </summary>
    public class CyclicalMemoryBuffer
    {
        /// <summary>
        /// Array of buffers to hold the infomation.
        /// </summary>
        protected byte[][] buffers;

        /// <summary>
        /// Amount of usable bytes on each buffer.
        /// </summary>
        protected uint[] usableBytes;

        /// <summary>
        /// Array count of the buffer.
        /// </summary>
        protected uint size;

        /// <summary>
        /// Amount of bytes that each buffer can hold.
        /// </summary>
        protected uint buffersLength;

        /// <summary>
        /// Index position where the next writing opertion will be performed.
        /// </summary>
        protected uint writingIndex;

        /// <summary>
        /// Index position where the reading operation will be performed.
        /// </summary>
        protected uint readingIndex;

        /// <summary>
        /// Creates a cyclical array of buffers.
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

        /// <summary>
        /// Writes an specified byte array into the buffer.<b>This method copies the entire array.</b>
        /// </summary>
        /// <param name="sourceArray">Source array. Those bytes to be copied into the buffer.</param>
        /// <param name="bytesToCopy">Amount of bytes to be copied.</param>
        /// <returns>Amount of copied bytes.</returns>
        public uint Write(byte[] sourceArray, uint bytesToCopy)
        {
            System.Buffer.BlockCopy(sourceArray, 0, this.buffers[this.writingIndex], 0, (int)bytesToCopy);
            this.usableBytes[ this.writingIndex ] = bytesToCopy;
            this.writingIndex = (this.writingIndex + 1) % this.size;
            return bytesToCopy;
        }

        /// <summary>
        /// Reads the information from the buffer and writes it into the given array.<b>The destination array must have enough space to hold the incoming information.
        /// Otherwise an IndexOutMemoryException will be thrown. You can use the FixedLength method to know the maximun amount of information that this buffer can deliver on
        /// a single read operation.</b>
        /// </summary>
        /// <param name="destArray">Array to be filled with the information.</param>
        /// <returns>Amount of read bytes.</returns>
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

        /// <summary>
        /// Releases all the memory buffers and sets them to null.<b>After calling this method the entire reference becomes unusable.</b>
        /// </summary>
        public void Release()
        {
            for (int i = 0; i < this.size; i++)
            {
                this.buffers[i] = null;
            }
            this.buffers = null;
            this.usableBytes = null;
            this.buffersLength = 0;
            this.readingIndex = 0;
            this.writingIndex = 0;
            this.size = 0;
        }

        /// <summary>
        /// Gets the size of each buffer.
        /// </summary>
        public uint FixedLength
        {
            get
            {
                return this.buffersLength;
            }
        }
    }
}
