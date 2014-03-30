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
            uint availableBytes = this.bufferSize - this.writingPosition - size;
            if (availableBytes >= 0)
            {
                System.Buffer.BlockCopy(source, 0, this.ioBuffer, (int)this.writingPosition, (int)size);
                this.writingPosition = (this.writingPosition + size) % this.bufferSize;
            }
            else
            {

            }
            return 0;
        }
    }
}
