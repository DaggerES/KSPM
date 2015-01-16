/*
	<file name="HostInformation.cs"/>
	<author>Scr_Ra</author>
	<date>1/7/2015 6:24:11 PM</date>
*/

using System;
using System.Collections.Generic;
using System.Net;

namespace KSPM.Network.Server.HostsManagement
{
    /// <sumary>
    /// Definition for HostInformation.
    ///	HostInformation is a: 
    /// </sumary>
    public class HostInformation
    {

        /// <summary>
        /// Unique Id.
        /// </summary>
        protected internal int Id;

        /// <summary>
        /// IPEndPoint used to send the information.
        /// </summary>
        protected IPEndPoint endPoint;

        /// <summary>
        /// Address in raw format, it is useful to fast comparing methods.
        /// </summary>
        protected long[] address;

        /// <summary>
        /// Port used by the host.
        /// </summary>
        protected int port;

        /// <summary>
        /// Creates a HostInformation instance with the given parameters. SETS IPEndPoint property to null.
        /// </summary>
        /// <param name="addressBuffer">Byte array with the addres</param>
        /// <param name="offset">Start position where the system should read bytes.</param>
        /// <param name="count">Number of bytes to be read.</param>
        /// <param name="port">Port number used by the host.</param>
        public HostInformation( byte[] addressBuffer, int offset, int count, int port)
        {
            ///Ready to hold Ipv6 addresses.
            this.address = new long[2];
            
            if( addressBuffer == null)
            {
                throw new NullReferenceException("addresBuffer is set to null");
            }

            if( port > IPEndPoint.MaxPort || port < IPEndPoint.MinPort )
            {
                throw new ArgumentOutOfRangeException("Invalid port");
            }

            this.port = port;

            for( int i = 0 ; i < count - 1; i++)
            {
                this.address[count / 8] |= addressBuffer[offset + i];
                this.address[count / 8] <<= 8;///Shift 8 bits
            }

            this.endPoint = null;
        }

        /// <summary>
        /// Determines if a HostInformation reference is equals to another.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            HostInformation comparand;
            if (!obj.GetType().Equals(this.GetType()))
                return false;
            comparand = (HostInformation)obj;
            return this.address[ 0 ] == comparand.address[ 0 ] && this.address[ 1 ] == comparand.address[ 1 ] && this.port == comparand.port;
        }

        /// <summary>
        /// Overrides the GetHashCode.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.address.GetHashCode();
        }

        /// <summary>
        /// Gets the address array.
        /// </summary>
        public long[] Address
        {
            get
            {
                return this.address;
            }
        }

        /// <summary>
        /// Gets the port used by the host.
        /// </summary>
        public int Port
        {
            get
            {
                return this.port;
            }
        }

        /// <summary>
        /// Gets the IPEndPoint that represents this host information.
        /// </summary>
        public IPEndPoint EndPoint
        {
            get
            {
                return this.endPoint;
            }
        }
    }
}
