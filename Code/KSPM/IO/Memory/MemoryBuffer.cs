namespace KSPM.IO.Memory
{
    public class MemoryBuffer
    {
        protected byte[] ioBuffer;
        protected uint bufferSize;
        protected uint readingPosition;
        protected uint writingPosition;

        public MemoryBuffer(uint bufferLength)
        {
            this.bufferSize = bufferLength;
            this.ioBuffer = new byte[this.bufferSize];
            this.readingPosition = this.writingPosition = 0;
        }

        public uint Write(byte[] source, uint size)
        {
            ///Tells the size of the block to be written.
            uint blockSize = (this.writingPosition + size) % this.bufferSize;
            System.Buffer.BlockCopy(source, 0, this.ioBuffer, (int)this.writingPosition, (int)blockSize);
            this.writingPosition = (this.writingPosition + blockSize) % this.bufferSize;
            if (blockSize != size)
            {
                System.Buffer.BlockCopy(source, (int)blockSize, this.ioBuffer, (int)this.writingPosition, (int)(size - blockSize));
                this.writingPosition = (this.writingPosition + size - blockSize) % this.bufferSize;
            }
            return blockSize;
        }
    }
}
