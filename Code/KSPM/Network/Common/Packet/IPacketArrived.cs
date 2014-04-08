using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPM.Network.Common.Packet
{
    public interface IPacketArrived
    {
        void ProcessPacket(byte[] rawData, uint fixedLegth);
        void ProcessPacket(byte[] rawData, uint rawDataOffset, uint fixedLength);
    }
}
