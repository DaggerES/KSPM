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

        protected long maxNumberOfCommands;

        protected static readonly long MaxQueueSize = 5000;

        public CommandQueue()
        {
            this.commandMessagesQueue = new Queue<Message>();
            this.maxNumberOfCommands = CommandQueue.MaxQueueSize;
        }

        /// <summary>
        /// Enqueue a new command message to the underlayin queue.
        /// </summary>
        /// <param name="newMessage">Reference to the new message, if it is null nothing will performed.</param>
        public virtual void EnqueueCommandMessage(ref Message newMessage)
        {
            lock (this.commandMessagesQueue)
            {
                if (newMessage != null)
                {
                    if (this.commandMessagesQueue.Count < this.maxNumberOfCommands)
                    {
                        this.commandMessagesQueue.Enqueue(newMessage);
                    }
                    else
                    {
                        KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("!!!WARNING Droping packets!!!, MaxNumberOfMessagesAllowed reached: " + this.maxNumberOfCommands);
                        newMessage.Release();
                    }
                }
            }
        }
        
        /// <summary>
        /// Dequeue a command message from the underlaying queue.
        /// </summary>
        /// <param name="newMessage">out message, so it will hold the dequeued message, null if there was no message to dequeue.</param>
        public virtual void DequeueCommandMessage(out Message newMessage)
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
        /// As its name says it is not granted to return the actual count, it could change from one moment to another.
        /// </summary>
        public int DirtyCount
        {
            get
            {
                return this.commandMessagesQueue.Count;
            }
        }

        /// <summary>
        /// Removes all messages and calls the Release method on each one.
        /// </summary>
        /// <param name="threadSafe">Tells if the call should be ThreadSafe and lock the queue.</param>
        public virtual void Purge( bool threadSafe)
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
