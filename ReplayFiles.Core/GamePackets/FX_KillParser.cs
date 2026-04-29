namespace ReplayFiles.Core.GamePackets;

public class FX_KillData
{
    public uint RoutingNetID { get; set; }
    public uint NetID { get; set; }
    public int BytesRemaining { get; set; }
}

public class FX_KillParser : GamePacketParser
{
    protected override object ReadBody(uint routingNetId, ref SpanReader reader)
    {
        return new FX_KillData
        {
            RoutingNetID = routingNetId,
            NetID = reader.ReadUInt32LittleEndian(),
            BytesRemaining = reader.Remaining
        };
    }
}