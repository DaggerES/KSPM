/*
	<file name="NetworkInterface.cs"/>
	<author>Scr_Ra</author>
	<date>1/12/2015 1:14:54 PM</date>
*/

using System;
using System.Collections.Generic;
using System.Net;

namespace KSPM.Network.NetworkInformation
{
    /// <sumary>
    /// Definition for NetworkInterface.
    ///	NetworkInterface is a: container of a network information which is configured to a determined NIC on the system.
    /// </sumary>
    public class ProtoNetworkInterface
    {
        public string name;
        protected uint networkMask;
        protected uint networkId;
        protected uint rawIpAdress;
        protected uint networkGateway;

        public IPAddress address;

        /// <summary>
        /// Creates a new reference with the given information, the netmask is tryed to get using the network gateway and the ip addresses.
        /// </summary>
        /// <param name="networkGateway"></param>
        /// <param name="ipAddress"></param>
        public ProtoNetworkInterface( uint networkGateway, uint ipAddress )
        {
            uint mask = 0xFF000000;
            this.networkGateway = networkGateway;
            this.rawIpAdress = ipAddress;
            this.networkMask = 0x0;

            for (int i = 0; i < 4; i++ )
            {
                if( ( mask & networkGateway ) == (mask & ipAddress ) )
                {
                    this.networkMask |= mask;
                }
                mask >>= 8;
            }

            this.networkId = this.networkMask & this.networkGateway;
            this.address = new IPAddress(this.rawIpAdress);
        }

        public bool TryAddress( int target )
        {
            return (this.networkMask & target) == this.networkId;
        }
    }
}
