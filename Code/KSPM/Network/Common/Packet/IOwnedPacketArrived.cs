namespace KSPM.Network.Common.Packet
{
    public interface IOwnedPacketArrived
    {
        void ProcessPacket(byte[] rawData, uint rawDataOffset, uint fixedLength, NetworkEntity packetOwner);
    }
}
