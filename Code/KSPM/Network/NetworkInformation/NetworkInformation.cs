/*
	<file name="NetworkInformation.cs"/>
	<author>Scr_Ra</author>
	<date>1/12/2015 1:09:17 PM</date>
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace KSPM.Network.NetworkInformation
{
    /// <sumary>
    /// Definition for NetworkInformation.
    ///	NetworkInformation is a:
    /// </sumary>
    public class NetworkInformation
    {
        /// <summary>
        /// Looks for those available and up NICs on the system.
        /// </summary>
        /// <param name="filter">Kindf of addresses to look for.</param>
        /// <returns>A list with the addresses on the system, by default it has the loopback Address</returns>
        public static List<ProtoNetworkInterface> GetUsableAddresses(AddressFamily filter)
        {
            NetworkInterface[] interfaces;
            UnicastIPAddressInformationCollection ipInformation;
            GatewayIPAddressInformation gateWay;
            ProtoNetworkInterface nic;
            List<ProtoNetworkInterface> nics = new List<ProtoNetworkInterface>();
            interfaces = NetworkInterface.GetAllNetworkInterfaces();
            int ipIndex;
            for (int i = 0; i < interfaces.Length; i++)
            {
                if (interfaces[i].OperationalStatus == OperationalStatus.Up)
                {
                    if (interfaces[i].GetIPProperties().GatewayAddresses.Count > 0)
                    {
                        gateWay = interfaces[i].GetIPProperties().GatewayAddresses[0];
                        ipInformation = interfaces[i].GetIPProperties().UnicastAddresses;
                        for (ipIndex = 0; ipIndex < ipInformation.Count; ipIndex++)
                        {
                            if (ipInformation[ipIndex].Address.AddressFamily == filter)
                            {
                                nic = new ProtoNetworkInterface(System.BitConverter.ToUInt32(gateWay.Address.GetAddressBytes(), 0), System.BitConverter.ToUInt32(ipInformation[ipIndex].Address.GetAddressBytes(), 0));
                                nic.name = interfaces[i].Name;
                                nics.Add(nic);
                            }
                        }
                    }
                }
            }
            return nics;
        }

        /// <summary>
        /// Looks for a proper NIC that can reach the IP given as parameter.
        /// </summary>
        /// <param name="target">IP address to be reached.</param>
        /// <returns>A reference to a NIC that could reach the given IP, if none NIC fits the first NIC on the system is returned.</returns>
        public static ProtoNetworkInterface TryToRouteIP(  List<ProtoNetworkInterface> nics, IPAddress target )
        {
            int targetIP = System.BitConverter.ToInt32(target.GetAddressBytes(), 0);
            for( int i = 0 ; i < nics.Count; i++)
            {
                if( nics[ i].TryAddress( targetIP) )
                {
                    return nics[i];
                }
            }
            return nics[0];
        }
    }
}
