//#define DEBUGPRINT
using System.Net.Sockets;


namespace KSPM.Network.Common
{
    public class SharedBufferSAEAPool
    {
        protected System.Collections.Generic.Queue<SocketAsyncEventArgs> availableSAEA;
        protected uint availableSlots;
        protected byte [] buffer;
        protected uint bufferSize;

        public delegate void OnCompleteOperation(object sender, SocketAsyncEventArgs e);

        /// <summary>
        /// Creates a SocketAsyncEventArgs pool, with the same buffer and sets the Complete event to the callback.
        /// </summary>
        /// <param name="initialCapacity">How many SAEA objects will be pooled.</param>
        /// <param name="sharedBuffer">Byte array to be set as the buffer to all the SAEA objects.</param>
        /// <param name="callback">Method to be set as the SocketAsyncEventArgs.Complete event.</param>
        public SharedBufferSAEAPool(uint initialCapacity, byte [] sharedBuffer, OnCompleteOperation callback)
        {
            this.availableSlots = initialCapacity;
            this.availableSAEA = new System.Collections.Generic.Queue<SocketAsyncEventArgs>((int)this.availableSlots);
            this.bufferSize = (uint)sharedBuffer.Length;
            this.buffer = sharedBuffer;
            this.InitializeSlots( callback );
        }

        protected void InitializeSlots( OnCompleteOperation method )
        {
            SocketAsyncEventArgs item = null;
            for( int i = 0 ; i < this.availableSlots ; i++ )
            {
                item = new SocketAsyncEventArgs();
                item.Completed += new System.EventHandler<SocketAsyncEventArgs>(method);
                item.SetBuffer(this.buffer, 0, (int)this.bufferSize);
                this.availableSAEA.Enqueue(item);
            }
        }

        public SocketAsyncEventArgs NextSlot
        {
            get
            {
                lock (this.availableSAEA)
                {
                    if (this.availableSAEA.Count > 0)
                    {
#if DEBUGPRINT
                        KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("Taking: " + this.availableSAEA.Count.ToString());
#endif
                        return this.availableSAEA.Dequeue();
                    }
                    else
                    {
                        ///This should not happen.
                        SocketAsyncEventArgs extraItem = new SocketAsyncEventArgs();
                        extraItem.SetBuffer(this.buffer, 0, (int)this.bufferSize);
                        KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("Warning, extra SAEA added");
                        return extraItem;
                    }
                }
            }
        }

        public void Recycle(SocketAsyncEventArgs oldSocketAsyncEventArgs)
        {
            oldSocketAsyncEventArgs.AcceptSocket = null;
            oldSocketAsyncEventArgs.UserToken = null;

            ///Reseting the buffer, if you do not reset it, you can get Fault errors at high speeds.
            oldSocketAsyncEventArgs.SetBuffer(0, 0);
            lock (this.availableSAEA)
            {
                this.availableSAEA.Enqueue(oldSocketAsyncEventArgs);
#if DEBUGPRINT
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("Recycling: " + this.availableSAEA.Count.ToString());
#endif
            }
        }

        public void Release(bool threadSafe)
        {
            SocketAsyncEventArgs[] items;
            if (threadSafe)
            {
                lock (this.availableSAEA)
                {
                    items = this.availableSAEA.ToArray();
                    for (int i = 0; i < items.Length; i++)
                    {
                        items[i].Dispose();
                        items[i] = null;
                    }
                    this.availableSAEA.Clear();
                }
            }
            else
            {
                items = this.availableSAEA.ToArray();
                for (int i = 0; i < items.Length; i++)
                {
                    items[i].Dispose();
                    items[i] = null;
                }
                this.availableSAEA.Clear();
            }
            this.availableSAEA = null;
            this.availableSlots = 0;
            this.bufferSize = 0;
            this.buffer = null;
        }

        public uint BufferSize
        {
            get
            {
                return this.bufferSize;
            }
        }
    }
}
