using System.Collections.Generic;

namespace KSPM.Network.Common
{
    /// <summary>
    /// A thread safe queue which holds the command messages.
    /// </summary>
    public class CommandQueue
    {
        protected Queue<Message> commandMessagesQueue;

        public CommandQueue()
        {
            this.commandMessagesQueue = new Queue<Message>();
        }

        /// <summary>
        /// Enqueue a new command message to the underlayin queue.
        /// </summary>
        /// <param name="newMessage">Reference to the new message, if it is null nothing will performed.</param>
        public void EnqueueCommandMessage(ref Message newMessage)
        {
            lock (this.commandMessagesQueue)
            {
                if (newMessage != null)
                {
                    this.commandMessagesQueue.Enqueue(newMessage);
                }
            }
        }
        
        /// <summary>
        /// Dequeue a command message from the underlaying queue.
        /// </summary>
        /// <param name="newMessage">out message, so it will hold the dequeued message, null if there was no message to dequeue.</param>
        public void DequeueCommandMessage(out Message newMessage)
        {
            newMessage = null;
            lock (this.commandMessagesQueue)
            {
                if (this.commandMessagesQueue.Count > 0)
                {
                    newMessage = this.commandMessagesQueue.Dequeue();
                }
            }
        }

        /// <summary>
        /// Verifies if the queue is empty or not.
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            lock (this.commandMessagesQueue)
            {
                return this.commandMessagesQueue.Count == 0;
            }
        }

        public void Purge( bool threadSafe)
        {
            if (threadSafe)
            {
                lock (this.commandMessagesQueue)
                {
                    this.commandMessagesQueue.Clear();
                }
            }
            else
            {
                this.commandMessagesQueue.Clear();
            }
        }
    }
}
