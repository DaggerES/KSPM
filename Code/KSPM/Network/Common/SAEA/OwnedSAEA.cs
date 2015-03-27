/*
	<file name="OwnedSAEA.cs"/>
	<author>Scr_Ra</author>
	<date>3/26/2015 5:18:00 PM</date>
*/

using System.Net.Sockets;

namespace KSPM.Network.Common.SAEA
{
    /// <sumary>
    /// Definition for OwnedSAEA.
    ///	OwnedSAEA is a:
    /// </sumary>
    public class OwnedSAEA
    {
        /// <summary>
        /// Defined as object because it can be anyone.
        /// </summary>
        public object Owner;

        /// <summary>
        /// SocketAsyncEventArg reference.
        /// </summary>
        internal protected SocketAsyncEventArgs saeaReference;

        /// <summary>
        /// Creates an empty SAEA object with no owner.
        /// </summary>
        public OwnedSAEA()
        {
            this.Owner = null;
            this.saeaReference = new SocketAsyncEventArgs();
        }

        /// <summary>
        /// Disposes the reference, sets the owner to null.
        /// </summary>
        public void Dispose()
        {
            this.Owner = null;
        }

        /// <summary>
        /// Releases the object setting everything to null.
        /// </summary>
        public void Release()
        {
            this.Owner = null;
            this.saeaReference.Dispose();
            this.saeaReference = null;
        }

        /// <summary>
        /// Gets the SocketAsyncEventArgs object.
        /// </summary>
        public SocketAsyncEventArgs SAEAObject
        {
            get
            {
                return this.saeaReference;
            }
        }
    }
}
