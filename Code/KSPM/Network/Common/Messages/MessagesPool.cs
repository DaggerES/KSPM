//#define DEBUGPRINT
namespace KSPM.Network.Common.Messages
{
    public class MessagesPool
    {
        protected System.Collections.Generic.Queue<Messages.Message> messagesPool;

        protected uint poolSize;

        protected Message sample;

        public MessagesPool(uint poolSize, Message messageSample)
        {
            this.poolSize = poolSize;
            this.messagesPool = new System.Collections.Generic.Queue<Message>((int)poolSize);
            this.sample = messageSample;
            Message item;
            for (int i = 0; i < this.poolSize; i++)
            {
                //item = new BufferedMessage(Message.CommandType.Null, 0, 0);
                item = this.sample.Empty();
                this.messagesPool.Enqueue(item);
            }
        }

        /// <summary>
        /// Takes off an item from the pool ready to be used.
        /// </summary>
        public Messages.Message BorrowMessage
        {
            get
            {
                lock (this.messagesPool)
                {
                    if (this.messagesPool.Count > 0)
                    {
#if DEBUGPRINT
                        KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(string.Format("Borrow from pool: {0}", this.messagesPool.Count));
#endif
                        return this.messagesPool.Dequeue();
                    }
                    else
                    {
                        Messages.BufferedMessage extraItem = new Messages.BufferedMessage(Messages.Message.CommandType.Null, 0, 0);
                        return extraItem;
                    }
                }
            }
        }

        /// <summary>
        /// Recycles the given item and put it back to the pool.
        /// </summary>
        /// <param name="oldItem"></param>
        public void Recycle(Message oldItem)
        {
            if (oldItem == null)
                return;
            oldItem.Release();
            lock (this.messagesPool)
            {
                this.messagesPool.Enqueue(oldItem);
#if DEBUGPRINT
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(string.Format("Recycle to pool: {0}", this.messagesPool.Count));
#endif
            }
        }

        /// <summary>
        /// Releases all the stored messages, making this reference unable to perform any further job.
        /// </summary>
        public void Release()
        {
            Message item;
            lock (this.messagesPool)
            {
                while (this.messagesPool.Count > 0)
                {
                    item = this.messagesPool.Dequeue();
                    item.Release();
                    item = null;
                }
            }
            this.sample.Release();
            this.sample = null;
        }

        /// <summary>
        /// Gets the initial size of the pool.
        /// </summary>
        public uint FixedSize
        {
            get
            {
                return this.poolSize;
            }
        }

        /// <summary>
        /// Returns the current size of the pool.<b>This could be a dirty value, so be carefull about this value.</b>
        /// </summary>
        public int CurrentSize
        {
            get
            {
                return this.messagesPool.Count;
            }
        }

        /// <summary>
        /// Returns the instance on which is based the MessagePool.
        /// </summary>
        public Message MessageSample
        {
            get
            {
                return this.sample;
            }
        }
    }
}
