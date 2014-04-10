namespace KSPM.Network.Common.Packet
{
    public interface IUDPPacketArrived
    {
        void ProcessUDPPacket(byte[] rawData, uint fixedLegth);
    }
}
