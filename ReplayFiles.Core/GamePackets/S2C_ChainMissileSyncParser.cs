using System;
using System.Collections.Generic;
using System.Text;

namespace ReplayFiles.Core;

public class S2C_ChainMissileSyncData
{
    public uint RoutingNetID { get; set; }
    public int TargetCount { get; set; }
    public uint OwnerNetworkID { get; set; }
    public List<uint> TargetNetIDs { get; set; } = new List<uint>();
}

public class S2C_ChainMissileSyncParser : GamePacketParser
{
    protected override object ReadBody(uint routingNetId, ref SpanReader reader)
    {
        var data = new S2C_ChainMissileSyncData { RoutingNetID = routingNetId };
        data.TargetCount = reader.ReadInt32LittleEndian();
        data.OwnerNetworkID = reader.ReadUInt32LittleEndian();

        int toRead = data.TargetCount;

        if (reader.Remaining > (toRead * 4) && reader.Remaining == (32 * 4))
        {
            toRead = 32;
        }

        for (int i = 0; i < toRead; i++)
        {
            data.TargetNetIDs.Add(reader.ReadUInt32LittleEndian());
        }

        return data;
    }
}
