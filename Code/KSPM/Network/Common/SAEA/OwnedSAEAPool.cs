/*
	<file name="OwnedSAEAPool.cs"/>
	<author>Scr_Ra</author>
	<date>3/26/2015 5:34:04 PM</date>
*/

using System.Net.Sockets;
using System.Collections.Generic;

namespace KSPM.Network.Common.SAEA
{
    /// <sumary>
    /// Definition for OwnedSAEAPool.
    ///	OwnedSAEAPool is a:
    /// </sumary>
    public class OwnedSAEAPool
    {
        /// <summary>
        /// Definition of the method that must be called once the async operation is completed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void OnCompleteOperation(object sender, SocketAsyncEventArgs e);

        protected OnCompleteOperation completionMethod;

        /// <summary>
        /// Pool of objects.
        /// </summary>
        protected System.Collections.Generic.Queue<OwnedSAEA> availableSAEA;

        /// <summary>
        /// 
        /// </summary>
        protected uint availableSlots;

        /// <summary>
        /// Creates an OwnedSocketAsyncEventArgs pool, with the same buffer and sets the Complete event to the callback.
        /// </summary>
        /// <param name="initialCapacity">How many SAEA objects will be pooled.</param>
        /// <param name="callback">Method to be set as the SocketAsyncEventArgs.Complete event.</param>
        public OwnedSAEAPool(uint initialCapacity, OnCompleteOperation callback)
        {
            this.availableSlots = initialCapacity;
            this.availableSAEA = new System.Collections.Generic.Queue<OwnedSAEA>((int)this.availableSlots);
            this.InitializeSlots( callback );
            this.completionMethod = callback;
        }

        /// <summary>
        /// Creates and initializes the pool and its objects.
        /// </summary>
        /// <param name="method"></param>
        protected void InitializeSlots( OnCompleteOperation method )
        {
            OwnedSAEA item = null;
            for( int i = 0 ; i < this.availableSlots ; i++ )
            {
                item = new OwnedSAEA();
                item.saeaReference.Completed += new System.EventHandler<SocketAsyncEventArgs>(method);
                this.availableSAEA.Enqueue(item);
            }
        }

        /// <summary>
        /// GEts a fresh object from the pool.
        /// </summary>
        public OwnedSAEA NextSlot
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
                        OwnedSAEA extraItem = new OwnedSAEA();
                        extraItem.saeaReference.Completed += new System.EventHandler<SocketAsyncEventArgs>(this.completionMethod);
                        KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("Warning, extra SAEA added");
                        return extraItem;
                    }
                }
            }
        }

        /// <summary>
        /// Recycles and puts the object into the pool.
        /// </summary>
        /// <param name="oldSocketAsyncEventArgs"></param>
        public void Recycle(OwnedSAEA oldObject)
        {
            oldObject.saeaReference.AcceptSocket = null;
            oldObject.saeaReference.UserToken = null;

            ///Reseting the buffer, if you do not reset it, you can get Fault errors at high speeds.
            oldObject.saeaReference.SetBuffer(0, 0);
            lock (this.availableSAEA)
            {
                this.availableSAEA.Enqueue(oldObject);
#if DEBUGPRINT
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo("Recycling: " + this.availableSAEA.Count.ToString());
#endif
            }
        }

        /// <summary>
        /// Releases each resource used by the object.
        /// </summary>
        /// <param name="threadSafe"></param>
        public void Release(bool threadSafe)
        {
            OwnedSAEA[] items;
            if (threadSafe)
            {
                lock (this.availableSAEA)
                {
                    items = this.availableSAEA.ToArray();
                    for (int i = 0; i < items.Length; i++)
                    {
                        items[i].Release();
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
                    items[i].Release();
                    items[i] = null;
                }
                this.availableSAEA.Clear();
            }
            this.availableSAEA = null;
            this.availableSlots = 0;
        }
    }
}
