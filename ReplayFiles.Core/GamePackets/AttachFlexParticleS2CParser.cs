namespace ReplayFiles.Core.GamePackets;

public class AttachFlexParticleS2CData
{
    public uint RoutingNetID { get; set; }
    public uint NetID { get; set; }
    public byte ParticleFlexID { get; set; }
    public byte CpIndex { get; set; }
    public uint ParticleAttachType { get; set; }
    public int BytesRemaining { get; set; }
}

public class AttachFlexParticleS2CParser : GamePacketParser
{
    protected override object ReadBody(uint routingNetId, ref SpanReader reader)
    {
        uint netId = reader.ReadUInt32LittleEndian();
        byte particleFlexId = reader.ReadByte();
        byte cpIndex = reader.ReadByte();
        uint particleAttachType = reader.ReadUInt32LittleEndian();

        return new AttachFlexParticleS2CData
        {
            RoutingNetID = routingNetId,
            NetID = netId,
            ParticleFlexID = particleFlexId,
            CpIndex = cpIndex,
            ParticleAttachType = particleAttachType,
            BytesRemaining = reader.Remaining
        };
    }
}