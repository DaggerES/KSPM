using System.Net.Sockets;

namespace KSPM.Network.Common
{
    public class SocketAsyncEventArgsPool
    {
        protected System.Collections.Generic.Queue<SocketAsyncEventArgs> availableSAEA;
        protected uint availableSlots;

        public SocketAsyncEventArgsPool( uint initialCapacity )
        {
            this.availableSlots = initialCapacity;
            this.availableSAEA = new System.Collections.Generic.Queue<SocketAsyncEventArgs>((int)this.availableSlots);
            this.InitializeSlots();
        }

        protected void InitializeSlots()
        {
            SocketAsyncEventArgs item = null;
            for( int i = 0 ; i < this.availableSlots ; i++ )
            {
                item = new SocketAsyncEventArgs();
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
                        KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("Taking: " + this.availableSAEA.Count.ToString());
                        return this.availableSAEA.Dequeue();
                    }
                    else
                    {
                        ///This should not happen.
                        SocketAsyncEventArgs extraItem = new SocketAsyncEventArgs();
                        KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("Warning, extra SAEA added");
                        return extraItem;
                    }
                }
            }
        }

        public void Recycle(SocketAsyncEventArgs oldSocketAsyncEventArgs)
        {
            oldSocketAsyncEventArgs.AcceptSocket = null;
            oldSocketAsyncEventArgs.SetBuffer(null, 0, 0);
            oldSocketAsyncEventArgs.UserToken = null;
            lock (this.availableSAEA)
            {
                this.availableSAEA.Enqueue(oldSocketAsyncEventArgs);
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("Recycling: " + this.availableSAEA.Count.ToString());
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
        }
    }
}
