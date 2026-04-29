namespace ReplayFiles.Core;

public class NPC_BuffRemove2Data
{
    public uint RoutingNetID { get; set; }
    public byte BuffSlot { get; set; }
    public uint BuffNameHash { get; set; }
    public float RunTimeRemove { get; set; }
    public int BytesRemaining { get; set; }
}

public class NPC_BuffRemove2Parser : GamePacketParser
{
    protected override object ReadBody(uint routingNetId, ref SpanReader reader)
    {
        return new NPC_BuffRemove2Data
        {
            RoutingNetID = routingNetId,
            BuffSlot = reader.ReadByte(),
            BuffNameHash = reader.ReadUInt32LittleEndian(),
            RunTimeRemove = reader.ReadSingleLittleEndian(),
            BytesRemaining = reader.Remaining
        };
    }
}