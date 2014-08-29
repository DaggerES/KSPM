﻿//using KSPM.Network.Common.Messages;

namespace KSPM.Network.Common.MessageHandlers
{
    /// <summary>
    /// Priority queue used to prioritize messages.
    /// It is called 3 way because it has 3 internal queues.
    /// </summary>
    public class PriorityQueue3Way
    {
        /// <summary>
        /// This is the main priority queue.
        /// </summary>
        protected CommandQueue priorityQueue;

        /// <summary>
        /// Primary queue of messages.
        /// </summary>
        protected CommandQueue primaryQueue;

        /// <summary>
        /// Secondary message queue used when the primary queue is full.
        /// </summary>
        protected CommandQueue backupQueue;

        /// <summary>
        /// Holds the amount of memopry used and controls the warning levels.
        /// </summary>
        protected int usedSpace;

        /// <summary>
        /// This is the queue used by the system, so do not worry how this works.
        /// </summary>
        public CommandQueue WorkingQueue;

        /// <summary>
        /// Flag to tells if the queue needs some purge.
        /// </summary>
        public int PurgeFlag;

        /// <summary>
        /// Delegate to create an async method to perform a purge on the queues.
        /// </summary>
        protected delegate void PurgeAsync();

        /// <summary>
        /// Messages pool used to recycle those messages that require it.
        /// </summary>
        protected Messages.MessagesPool recyclingPool;

        /// <summary>
        /// Creates the queues and set them ready.
        /// </summary>
        /// <param name="primaryQueueBase">CommandQueue uses as base to create this reference.</param>
        /// <param name="priorityQueueEnabled">Tells if the priority queue is enabled and will be initializated.</param>
        /// <param name="recyclingPool">Reference to the pool of messages used to recycle those messages that must be droped.</param>
        public PriorityQueue3Way( CommandQueue primaryQueueBase, bool priorityQueueEnabled, Messages.MessagesPool recyclingPool)
        {
            this.priorityQueue = null;
            this.primaryQueue = primaryQueueBase;
            this.backupQueue = this.primaryQueue.CloneEmpty();
            this.recyclingPool = recyclingPool;
            if( priorityQueueEnabled )
            {
                this.priorityQueue = this.primaryQueue.CloneEmpty();
            }

            this.WorkingQueue = this.primaryQueue;
            this.PurgeFlag = 0;
        }

        /// <summary>
        /// Creates and sets the queues with the given references.
        /// </summary>
        /// <param name="primaryQueueBase">CommandQueue uses as base to create this reference.</param>
        /// <param name="priorityQueue">Reference to the priority queue</param>
        /// <param name="recyclingPool">Reference to the pool of messages used to recycle those messages that must be droped.</param>
        public PriorityQueue3Way(CommandQueue primaryQueueBase, CommandQueue priorityQueue, Messages.MessagesPool recyclingPool)
        {
            this.priorityQueue = priorityQueue;
            this.primaryQueue = primaryQueueBase;
            this.backupQueue = this.primaryQueue.CloneEmpty();
            this.recyclingPool = recyclingPool;

            this.WorkingQueue = this.primaryQueue;
            this.PurgeFlag = 0;
        }

        /// <summary>
        /// Purges all the queues and set them to null, becoming unusable.<b>You must set to null those references passed in the constructor method. Only set them to null.</b>
        /// </summary>
        public void Purge(bool threadSafe)
        {
            this.primaryQueue.Purge(threadSafe);
            this.backupQueue.Purge(threadSafe);
            if (this.priorityQueue != null)
            {
                this.priorityQueue.Purge(threadSafe);
            }

            this.recyclingPool.Release();

            this.priorityQueue = null;
            this.primaryQueue = null;
            this.backupQueue = null;
            this.recyclingPool = null;
        }

        /// <summary>
        /// Releases all the used queues and set them to null.
        /// </summary>
        public void Release()
        {
            this.primaryQueue.Purge(false);
            this.backupQueue.Purge(false);
            if (this.priorityQueue != null)
            {
                this.priorityQueue.Purge(false);
            }

            this.recyclingPool.Release();

            this.priorityQueue = null;
            this.primaryQueue = null;
            this.backupQueue = null;
            this.recyclingPool = null;
        }

        /// <summary>
        /// Gets the priority queue either it is set to null or not.
        /// </summary>
        public CommandQueue PriorityQueue
        {
            get
            {
                return this.priorityQueue;
            }
        }

        /// <summary>
        /// Asynchronous method used to purge the queue. This method must be as fast as posible.
        /// </summary>
        protected void HandlePurgeCallback()
        {
            Messages.Message messageToThrow;
            Messages.MessagesPool messagePool = this.recyclingPool;

            System.Threading.Interlocked.Exchange(ref KSPM.Globals.KSPMGlobals.Globals.KSPMServer.warningLevel, (int)KSPM.Globals.KSPMSystem.WarningLevel.Warning);
            KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("Primary queue is full, starting to purge.");

            ///Loop to discard messages.
            while (this.backupQueue.DirtyCount > 0)
            {
                this.backupQueue.DequeueCommandMessage(out messageToThrow);
                messagePool.Recycle(messageToThrow);
            }
        }

        /// <summary>
        /// Method called once the purge process is finished.
        /// </summary>
        /// <param name="result"></param>
        protected void OnPurgeComplete( System.IAsyncResult result )
        {
            ///Setting a proper value to the purge flag, basically reseting the flag.
            System.Threading.Interlocked.Exchange(ref this.PurgeFlag, 0);

            ///Resetting the warning flag only if it is set to the warning level that cause the calling of this method.
            ///If it is set another warning level it will stays.
            System.Threading.Interlocked.CompareExchange(ref KSPM.Globals.KSPMGlobals.Globals.KSPMServer.warningLevel, (int)KSPM.Globals.KSPMSystem.WarningLevel.None, (int)KSPM.Globals.KSPMSystem.WarningLevel.Warning);

            ///Finishing the async method.
            PurgeAsync caller = (PurgeAsync)result.AsyncState;
            caller.EndInvoke(result);

            KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("Queue purged...");
        }

        /// <summary>
        /// Tries to enqueue an incoming message. Also calls the purge method if it is required.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>True if the messages was enqueued successfully, False otherwise.</returns>
        public bool TryToEnqueueMessage(ref Messages.Message message)
        {
            bool insertingResult = this.WorkingQueue.EnqueueCommandMessage(ref message);
            this.usedSpace = this.WorkingQueue.OccupiedSpace;
            if (!insertingResult)
            {
                if (System.Threading.Interlocked.CompareExchange( ref this.PurgeFlag, 1, 1 ) == 1)
                {
                    ///This meens that both queues are full
                    ///Required to implement a new system to fix this situation.
                    this.recyclingPool.Recycle(message);

                    ///Setting the warning flag value.
                    System.Threading.Interlocked.Exchange(ref KSPM.Globals.KSPMGlobals.Globals.KSPMServer.warningLevel, (int)KSPM.Globals.KSPMSystem.WarningLevel.Halt);
                    KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("WARNING BOTH QUEUES ARE FULL, STARTING TO DROP PAQUETS");
                }
                ///If this code is reached it means that the working queue is full, so the system has to implemente some sort of purge method.
                ///Setting up a proper value to the purge flag.
                System.Threading.Interlocked.Exchange(ref this.PurgeFlag, 1);

                ///Swaping queues.
                this.WorkingQueue = this.backupQueue;
                this.backupQueue = this.primaryQueue;

                ///Recycling the incoming message.
                this.recyclingPool.Recycle(message);

                ///Creating an asynchronous method to purge the affected queue.
                PurgeAsync purgeCallback = new PurgeAsync(this.HandlePurgeCallback);
                purgeCallback.BeginInvoke(this.OnPurgeComplete, purgeCallback);
            }
            else
            {
                ///Almost full it requires to bypass more packets.
                if( this.usedSpace >= 75 )
                {
                    System.Threading.Interlocked.Exchange(ref KSPM.Globals.KSPMGlobals.Globals.KSPMServer.warningLevel, (int)KSPM.Globals.KSPMSystem.WarningLevel.Warning);
                }
                    ///Starting to bypass packets.
                else if(this.usedSpace >= 50 )
                {
                    System.Threading.Interlocked.Exchange(ref KSPM.Globals.KSPMGlobals.Globals.KSPMServer.warningLevel, (int)KSPM.Globals.KSPMSystem.WarningLevel.Carefull);
                }
                else
                {
                    System.Threading.Interlocked.Exchange(ref KSPM.Globals.KSPMGlobals.Globals.KSPMServer.warningLevel, (int)KSPM.Globals.KSPMSystem.WarningLevel.None);
                }
            }
            return insertingResult;
        }
    }
}
