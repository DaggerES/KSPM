using KSPM.IO.Memory;
using KSPM.Network.Common.Messages;

namespace KSPM.Network.Common
{
    public class BufferedCommandQueue : CommandQueue
    {
        protected MemoryBuffer ioBuffer;

        public BufferedCommandQueue(uint bufferSize)
            : base()
        {
            this.ioBuffer = new MemoryBuffer(bufferSize);
        }

        public override void EnqueueCommandMessage(ref Message newMessage)
        {
            BufferedMessage reference = (BufferedMessage)newMessage;
            this.ioBuffer.Write(reference.bodyMessage, reference.StartsAt, reference.MessageBytesSize);
            base.EnqueueCommandMessage(ref newMessage);
        }

        public override void Purge(bool threadSafe)
        {
            this.ioBuffer.Release();
            this.ioBuffer = null;
            base.Purge(threadSafe);
        }
    }
}
