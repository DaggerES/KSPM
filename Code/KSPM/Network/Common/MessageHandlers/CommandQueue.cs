using System.Collections.Generic;

using KSPM.Network.Common.Messages;

namespace KSPM.Network.Common.MessageHandlers
{
    /// <summary>
    /// A thread safe queue which holds the command messages.
    /// </summary>
    public class CommandQueue
    {
        /// <summary>
        /// Queue of messages.
        /// </summary>
        protected Queue<Message> commandMessagesQueue;

        /// <summary>
        /// Amount of allowed messages on this queue.
        /// </summary>
        protected long maxNumberOfCommands;

        /// <summary>
        /// Default amount of allowed messages on all the queues.
        /// </summary>
        protected static readonly long MaxQueueSize = 5000;

        /// <summary>
        /// Tells the occupied percent of the queue.
        /// </summary>
        protected int itemsCount;

        public CommandQueue()
        {
            this.commandMessagesQueue = new Queue<Message>();
            this.maxNumberOfCommands = CommandQueue.MaxQueueSize;
        }

        /// <summary>
        /// Enqueue a new command message to the underlayin queue.
        /// </summary>
        /// <param name="newMessage">Reference to the new message, if it is null nothing will performed.</param>
        /// <returns>True if the message was successfully enqueued, FALSE otherwise.</returns>
        public virtual bool EnqueueCommandMessage(ref Message newMessage)
        {
            if (newMessage != null)
            {
                lock (this.commandMessagesQueue)
                {
                    if (this.commandMessagesQueue.Count < this.maxNumberOfCommands)
                    {
                        this.commandMessagesQueue.Enqueue(newMessage);
                        this.itemsCount = this.commandMessagesQueue.Count;
                        return true;
                    }
                    else
                    {
                        KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("!!!WARNING Droping packets!!!, MaxNumberOfMessagesAllowed reached: " + this.maxNumberOfCommands);
                    }
                }
            }
            return false;
            /*
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
            */
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
                    this.itemsCount = this.commandMessagesQueue.Count;
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
        /// Tells the occupied percent of the queue.
        /// </summary>
        public int OccupiedSpace
        {
            get
            {
                return this.itemsCount * 100 / (int)this.maxNumberOfCommands;
            }
        }

        /// <summary>
        /// Tells the Max amount of messages allowed on this queue.
        /// </summary>
        public long MaxCommandAllowed
        {
            get
            {
                return this.maxNumberOfCommands;
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

        /// <summary>
        /// Returns a new CommandQueue instance ready to be used.
        /// </summary>
        /// <returns>CommandQueue reference.</returns>
        public virtual CommandQueue CloneEmpty()
        {
            CommandQueue target = new CommandQueue();
            return target;
        }
    }
}
