namespace KSPM.Network.Common.Packet
{
    /// <summary>
    /// Interface used to process the incoming UDP packets.
    /// </summary>
    public interface IUDPPacketArrived
    {
        /// <summary>
        /// Process an UDP packet.
        /// </summary>
        /// <param name="rawData">Byte array with the incoming information.</param>
        /// <param name="fixedLegth">Amount of usable bytes in the packet.</param>
        void ProcessUDPPacket(byte[] rawData, uint fixedLegth);

        /// <summary>
        /// Process an UDP packet.
        /// </summary>
        /// <param name="incomingMessage">Message loaded with the packet.</param>
        void ProcessUDPMessage(Messages.Message incomingMessage);
    }
}
