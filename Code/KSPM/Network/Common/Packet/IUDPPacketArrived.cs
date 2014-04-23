namespace KSPM.Network.Common.Packet
{
    public interface IUDPPacketArrived
    {
        void ProcessUDPPacket(byte[] rawData, uint fixedLegth);

        void ProcessUDPMessage(Messages.Message incomingMessage);
    }
}
