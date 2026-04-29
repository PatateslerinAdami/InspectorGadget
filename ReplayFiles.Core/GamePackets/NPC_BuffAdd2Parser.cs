namespace ReplayFiles.Core;

public class NPC_BuffAdd2Data
{
    public uint RoutingNetID { get; set; }
    public byte BuffSlot { get; set; }
    public BuffType BuffType { get; set; }
    public byte Count { get; set; }
    public bool IsHidden { get; set; }
    public uint BuffNameHash { get; set; }
    public uint PackageHash { get; set; }
    public float RunningTime { get; set; }
    public float Duration { get; set; }
    public uint CasterNetID { get; set; }
    public int BytesRemaining { get; set; }
}

public class NPC_BuffAdd2Parser : GamePacketParser
{
    protected override object ReadBody(uint routingNetId, ref SpanReader reader)
    {
        return new NPC_BuffAdd2Data
        {
            RoutingNetID = routingNetId,
            BuffSlot = reader.ReadByte(),
            BuffType = (BuffType)reader.ReadByte(),
            Count = reader.ReadByte(),
            IsHidden = reader.ReadByte() != 0,
            BuffNameHash = reader.ReadUInt32LittleEndian(),
            PackageHash = reader.ReadUInt32LittleEndian(),
            RunningTime = reader.ReadSingleLittleEndian(),
            Duration = reader.ReadSingleLittleEndian(),
            CasterNetID = reader.ReadUInt32LittleEndian(),
            BytesRemaining = reader.Remaining
        };
    }
}