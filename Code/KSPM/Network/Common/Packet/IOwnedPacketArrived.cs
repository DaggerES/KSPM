namespace KSPM.Network.Common.Packet
{
    /// <summary>
    /// Interface used to receive a packet and set it to a NetworkEntity as owner of the packet.
    /// </summary>
    public interface IOwnedPacketArrived
    {
        /// <summary>
        /// Process a packet and set as owner the NetworkEntity.
        /// </summary>
        /// <param name="rawData">Byte array containing the info.</param>
        /// <param name="rawDataOffset">Starting position of the message.</param>
        /// <param name="fixedLength">Amount of bytes composing the usable data.</param>
        /// <param name="packetOwner">Network entityt who is owner of the packet.</param>
        void ProcessPacket(byte[] rawData, uint rawDataOffset, uint fixedLength, NetworkEntity packetOwner);
    }
}
