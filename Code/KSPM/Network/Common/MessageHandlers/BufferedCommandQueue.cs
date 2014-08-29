using KSPM.IO.Memory;
using KSPM.Network.Common.Messages;

namespace KSPM.Network.Common.MessageHandlers
{
    public class BufferedCommandQueue : CommandQueue
    {
        protected MemoryBuffer ioBuffer;

        public BufferedCommandQueue(uint bufferSize)
            : base()
        {
            this.ioBuffer = new MemoryBuffer(bufferSize);
        }

        public override bool EnqueueCommandMessage(ref Message newMessage)
        {
            BufferedMessage reference = (BufferedMessage)newMessage;
            uint startsAt = reference.StartsAt;
            this.ioBuffer.Write(reference.bodyMessage, ref startsAt, reference.MessageBytesSize);

            reference.StartsAt = startsAt;
            ///Seting the buffer used by the command queue.
            newMessage.bodyMessage = this.ioBuffer.IOBuffer;
            return base.EnqueueCommandMessage(ref newMessage);
        }

        public override void Purge(bool threadSafe)
        {
            this.ioBuffer.Release();
            this.ioBuffer = null;
            base.Purge(threadSafe);
        }

        /// <summary>
        /// Gets a new BufferedCommandQueue.
        /// </summary>
        /// <returns>Reference to a new object, it has the same properties as the original does.</returns>
        public override CommandQueue CloneEmpty()
        {
            BufferedCommandQueue target = new BufferedCommandQueue(this.ioBuffer.Size);
            return target;
        }
    }
}
