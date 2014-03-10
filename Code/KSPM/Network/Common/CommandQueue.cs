using System.Collections.Generic;

using KSPM.Network.Common.Messages;

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

        /// <summary>
        /// Removes all messages and calls the Release method on each one.
        /// </summary>
        /// <param name="threadSafe">Tells if the call should be ThreadSafe and lock the queue.</param>
        public void Purge( bool threadSafe)
        {
            Message [] messages;
            if (threadSafe)
            {
                lock (this.commandMessagesQueue)
                {
                    messages = this.commandMessagesQueue.ToArray();
                    for (int i = 0; i < messages.Length; i++)
                    {
                        messages[i].Release();
                    }
                    this.commandMessagesQueue.Clear();
                }
            }
            else
            {
                messages = this.commandMessagesQueue.ToArray();
                for (int i = 0; i < messages.Length; i++)
                {
                    messages[i].Release();
                }
                this.commandMessagesQueue.Clear();
            }
        }
    }
}
