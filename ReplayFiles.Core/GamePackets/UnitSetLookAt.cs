using System.Numerics;

namespace ReplayFiles.Core;

public class S2C_UnitSetLookAtData
{
    public uint RoutingNetID { get; set; }
    public byte LookAtType { get; set; }
    public Vector3 TargetPosition { get; set; }
    public uint TargetNetID { get; set; }
}

public class S2C_UnitSetLookAtParser : GamePacketParser
{
    protected override object ReadBody(uint routingNetId, ref SpanReader reader)
    {
        return new S2C_UnitSetLookAtData
        {
            RoutingNetID = routingNetId,
            LookAtType = reader.ReadByte(),
            TargetPosition = new Vector3(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian()),
            TargetNetID = reader.ReadUInt32LittleEndian()
        };
    }
}
