using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPM.Network.Common.MessageHandlers
{/// <summary>
    /// Priority queue used to prioritize messages.
    /// It is called 3 way because it has 3 internal queues.
    /// </summary>
    public class PriorityQueue2Way
    {
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
        /// Flag to tell if the queue needs some purge.
        /// </summary>
        public int PurgeFlag;

        /// <summary>
        /// Flag to tell the current warning level of this Queue.
        /// </summary>
        public int WarningFlagLevel;

        /// <summary>
        /// Delegate to create an async method to perform a purge on the queues.
        /// </summary>
        protected delegate void PurgeAsync();

        /// <summary>
        /// Messages pool used to recycle those messages that require it.
        /// </summary>
        protected Messages.MessagesPool recyclingPool;

        /// <summary>
        /// Timer used when the system has entered in a halt mode, giving some time to the system to purge its queues and recover its functionality.
        /// </summary>
        protected System.Threading.Timer asyncHaltTimer;

        /// <summary>
        /// Callback that handles when the system has been halted.
        /// </summary>
        protected System.Threading.TimerCallback haltPurgeCallback;

        /// <summary>
        /// Amount of time that the system is going to be droping packets, purging queues trying to recover functionality.
        /// </summary>
        protected int haltPurgeInterval;

        /// <summary>
        /// Creates the queues and set them ready.
        /// </summary>
        /// <param name="primaryQueueBase">CommandQueue uses as base to create this reference.</param>
        /// <param name="recyclingPool">Reference to the pool of messages used to recycle those messages that must be droped.</param>
        public PriorityQueue2Way(CommandQueue primaryQueueBase, Messages.MessagesPool recyclingPool)
        {
            this.primaryQueue = primaryQueueBase;
            this.backupQueue = this.primaryQueue.CloneEmpty();
            this.recyclingPool = recyclingPool;

            this.WorkingQueue = this.primaryQueue;
            this.PurgeFlag = 0;
            //Setting the warning level to None.
            this.WarningFlagLevel = (int)KSPM.Globals.KSPMSystem.WarningLevel.None;

            this.haltPurgeCallback = new System.Threading.TimerCallback(this.PurgeHaltedSystem);
            ///5 seconds
            this.haltPurgeInterval = 5000;

            ///Creatint the timer to purge the system if the halt warning level is reached.
            this.asyncHaltTimer = new System.Threading.Timer(this.haltPurgeCallback, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }

        /// <summary>
        /// Purges all the queues but not set them to null.
        /// </summary>
        public void Purge(bool threadSafe)
        {
            Messages.Message disposingMessage = null;
            ///Cleaning the working queue.
            ///Taking off and recycling each message stored inside the working queue.
            this.WorkingQueue.DequeueCommandMessage(out disposingMessage);
            while (disposingMessage != null)
            {
                this.recyclingPool.Recycle(disposingMessage);
                this.WorkingQueue.DequeueCommandMessage(out disposingMessage);
            }

            ///Checking if the system is already purging some queues.
            if (System.Threading.Interlocked.CompareExchange(ref this.PurgeFlag, int.MaxValue, int.MaxValue) == 1)
            {
                while (this.PurgeFlag != 0)
                {
                    KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(string.Format("PriorityQueue3Way-System already purging, waiting for 100 miliseconds."));
                    System.Threading.Thread.Sleep(100);
                }
            }

            this.primaryQueue.Purge(false);
            this.backupQueue.Purge(false);
        }

        /// <summary>
        /// Releases all the used queues and set them to null. Also recycles those messages put them back into the pool.
        /// </summary>
        public void Release()
        {
            Messages.Message disposingMessage = null;
            ///Cleaning the working queue.
            ///Taking off and recycling each message stored inside the working queue.
            this.WorkingQueue.DequeueCommandMessage(out disposingMessage);
            while (disposingMessage != null)
            {
                this.recyclingPool.Recycle(disposingMessage);
                this.WorkingQueue.DequeueCommandMessage(out disposingMessage);
            }

            ///Checking if the system is already purging some queues.
            if (System.Threading.Interlocked.CompareExchange(ref this.PurgeFlag, int.MaxValue, int.MaxValue) == 1)
            {
                while (this.PurgeFlag != 0)
                {
                    KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(string.Format("PriorityQueue3Way-System already purging, waiting for 100 miliseconds."));
                    System.Threading.Thread.Sleep(100);
                }
            }

            this.primaryQueue.Purge(false);
            this.backupQueue.Purge(false);

            this.recyclingPool.Release();

            this.primaryQueue = null;
            this.backupQueue = null;
            this.recyclingPool = null;
        }

        /// <summary>
        /// Asynchronous method used to purge the queue. This method must be as fast as posible.
        /// </summary>
        protected void HandlePurgeCallback()
        {
            Messages.Message messageToThrow;
            Messages.MessagesPool messagePool = this.recyclingPool;

            System.Threading.Interlocked.Exchange(ref this.WarningFlagLevel, (int)KSPM.Globals.KSPMSystem.WarningLevel.Warning);
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
        protected void OnPurgeComplete(System.IAsyncResult result)
        {
            ///Getting the actual value.
            int actualPurgeFlag = System.Threading.Interlocked.CompareExchange(ref this.PurgeFlag, int.MaxValue, int.MaxValue);

            if (actualPurgeFlag == 1)
            {
                ///1 means that everything is going well and the backup queue is working.
                ///So only is purging this queue.
                ///Setting a proper value to the purge flag, basically reseting the flag.
                System.Threading.Interlocked.Exchange(ref this.PurgeFlag, 0);
            }
            else
            {
                if (actualPurgeFlag == 2)
                {
                    ///2 means that both queues got full, and the system is droping packets.
                    ///So  as this queue is purged we must change queues.
                    ///Swaping queues.
                    this.backupQueue = this.WorkingQueue;//recovering its original reference.
                    this.WorkingQueue = this.primaryQueue;

                    ///Disabling the timer becaues already is a timer checking the status.
                    this.asyncHaltTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

                    ///Creating an asynchronous method to purge the queue that is still affected.
                    PurgeAsync purgeCallback = new PurgeAsync(this.HandlePurgeCallback);
                    purgeCallback.BeginInvoke(this.OnPurgeComplete, purgeCallback);

                    ///Setting the flag to 1 because there is only one queue full.
                    System.Threading.Interlocked.Exchange(ref this.PurgeFlag, 1);
                }
            }


            ///Resetting the warning flag only if it is set to the warning level that cause the calling of this method.
            ///If it is set another warning level it will stays.
            ///System.Threading.Interlocked.CompareExchange(ref this.WarningFlagLevel, (int)KSPM.Globals.KSPMSystem.WarningLevel.None, (int)KSPM.Globals.KSPMSystem.WarningLevel.Warning);

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
            bool insertingResult = true;
            int readPurgeFlag;

            ///Checking the previous value of the purge flag.
            readPurgeFlag = System.Threading.Interlocked.CompareExchange( ref this.PurgeFlag, int.MaxValue, int.MaxValue );
            if( readPurgeFlag != 2 )
            {
                ///This code means that there is not a halt.
                insertingResult = this.WorkingQueue.EnqueueCommandMessage(ref message);
                this.usedSpace = this.WorkingQueue.OccupiedSpace;
                if (!insertingResult)
                {
                    if (System.Threading.Interlocked.CompareExchange(ref this.PurgeFlag, 1, 1) == 1)
                    {
                        ///This meens that both queues are full
                        ///

                        ///Setting the warning flag value.
                        System.Threading.Interlocked.Exchange(ref this.WarningFlagLevel, (int)KSPM.Globals.KSPMSystem.WarningLevel.Halt);

                        ///Setting the purge flag to 2.
                        System.Threading.Interlocked.Exchange(ref this.PurgeFlag, 2);
                        KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("WARNING BOTH QUEUES ARE FULL, STARTING TO DROP PAQUETS");

                        ///Starting the timer.
                        this.asyncHaltTimer.Change(this.haltPurgeInterval, this.haltPurgeInterval);

                        this.recyclingPool.Recycle(message);
                    }
                    else
                    {
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
                }
                else
                {
                    ///Almost full it requires to bypass more packets.
                    if (this.usedSpace >= 75)
                    {
                        System.Threading.Interlocked.Exchange(ref this.WarningFlagLevel, (int)KSPM.Globals.KSPMSystem.WarningLevel.Warning);
                    }
                    ///Starting to bypass packets.
                    else if (this.usedSpace >= 50)
                    {
                        System.Threading.Interlocked.Exchange(ref this.WarningFlagLevel, (int)KSPM.Globals.KSPMSystem.WarningLevel.Carefull);
                    }
                    else
                    {
                        System.Threading.Interlocked.Exchange(ref this.WarningFlagLevel, (int)KSPM.Globals.KSPMSystem.WarningLevel.None);
                    }
                }
            }
            /*
            bool insertingResult = this.WorkingQueue.EnqueueCommandMessage(ref message);
            this.usedSpace = this.WorkingQueue.OccupiedSpace;
            if (!insertingResult)
            {
                if (System.Threading.Interlocked.CompareExchange(ref this.PurgeFlag, 1, 1) == 1)
                {
                    ///This meens that both queues are full
                    ///Required to implement a new system to fix this situation.
                    this.recyclingPool.Recycle(message);

                    ///Setting the warning flag value.
                    System.Threading.Interlocked.Exchange(ref this.WarningFlagLevel, (int)KSPM.Globals.KSPMSystem.WarningLevel.Halt);
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
                if (this.usedSpace >= 75)
                {
                    System.Threading.Interlocked.Exchange(ref this.WarningFlagLevel, (int)KSPM.Globals.KSPMSystem.WarningLevel.Warning);
                }
                ///Starting to bypass packets.
                else if (this.usedSpace >= 50)
                {
                    System.Threading.Interlocked.Exchange(ref this.WarningFlagLevel, (int)KSPM.Globals.KSPMSystem.WarningLevel.Carefull);
                }
                else
                {
                    System.Threading.Interlocked.Exchange(ref this.WarningFlagLevel, (int)KSPM.Globals.KSPMSystem.WarningLevel.None);
                }
            }
             * */
            return insertingResult;
        }

        /// <summary>
        /// Purges whatever is required to recover functionality.
        /// </summary>
        /// <param name="statesInfo"></param>
        protected void PurgeHaltedSystem(object statesInfo)
        {
            ///Reading the actual value of the purge flag.
            int readPurgeFlag = System.Threading.Interlocked.CompareExchange(ref this.PurgeFlag, int.MaxValue, int.MaxValue);

            ///Checking the working queue to be sure it is ready to continue.
            ///Only when the occupied space is less than 10 percent, the queue is ready to be receive commands.
            if( this.WorkingQueue.OccupiedSpace <= 10 )
            {
                if( readPurgeFlag == 2)///If the system is in a halt state.
                {
                    ///It must change the flag to 1 which means that the backup queue is being purged.
                    System.Threading.Interlocked.Exchange(ref this.PurgeFlag, 1);
                }
                ///Disabling the timer.
                this.asyncHaltTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }
        }
    }
}
