using System.Numerics;

namespace ReplayFiles.Core;

public class SpawnMinionS2CData
{
    public uint RoutingNetID { get; set; }
    public uint NetID { get; set; }
    public uint OwnerNetID { get; set; }
    public byte NetNodeID { get; set; }
    public Vector3 Position { get; set; }
    public int SkinID { get; set; }
    public uint CloneNetID { get; set; }
    public ushort TeamID { get; set; }
    public bool IgnoreCollision { get; set; }
    public bool IsWard { get; set; }
    public bool IsLaneMinion { get; set; }
    public bool IsBot { get; set; }
    public bool IsTargetable { get; set; }
    public uint IsTargetableToTeamSpellFlags { get; set; }
    public float VisibilitySize { get; set; }
    public string Name { get; set; } = "";
    public string SkinName { get; set; } = "";
    public ushort InitialLevel { get; set; }
    public uint OnlyVisibleToNetID { get; set; }
    public int BytesRemaining { get; set; }
}

public class SpawnMinionS2CParser : GamePacketParser
{
    protected override object ReadBody(uint routingNetId, ref SpanReader reader)
    {
        var data = new SpawnMinionS2CData { RoutingNetID = routingNetId };

        data.NetID = reader.ReadUInt32LittleEndian();
        data.OwnerNetID = reader.ReadUInt32LittleEndian();
        data.NetNodeID = reader.ReadByte();

        data.Position = new Vector3(
            reader.ReadSingleLittleEndian(),
            reader.ReadSingleLittleEndian(),
            reader.ReadSingleLittleEndian()
        );

        data.SkinID = reader.ReadInt32LittleEndian();
        data.CloneNetID = reader.ReadUInt32LittleEndian();
        data.TeamID = reader.ReadUInt16LittleEndian();

        byte bitfield = reader.ReadByte();
        data.IgnoreCollision = (bitfield & 0x01) != 0;
        data.IsWard = (bitfield & 0x02) != 0;
        data.IsLaneMinion = (bitfield & 0x04) != 0;
        data.IsBot = (bitfield & 0x08) != 0;
        data.IsTargetable = (bitfield & 0x10) != 0;

        data.IsTargetableToTeamSpellFlags = reader.ReadUInt32LittleEndian();
        data.VisibilitySize = reader.ReadSingleLittleEndian();

        data.Name = reader.ReadFixedString(64);
        data.SkinName = reader.ReadFixedString(64);

        data.InitialLevel = reader.ReadUInt16LittleEndian();
        data.OnlyVisibleToNetID = reader.ReadUInt32LittleEndian();

        data.BytesRemaining = reader.Remaining;
        return data;
    }
}