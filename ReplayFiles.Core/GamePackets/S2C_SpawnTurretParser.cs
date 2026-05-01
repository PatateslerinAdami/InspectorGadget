using System.Numerics;

namespace ReplayFiles.Core;

public class S2C_SpawnTurretData
{
    public uint RoutingNetID { get; set; }
    public uint NetID { get; set; }
    public uint OwnerNetID { get; set; }
    public byte NetNodeID { get; set; }
    public string Name { get; set; } = "";
    public string SkinName { get; set; } = "";
    public int SkinID { get; set; }
    public Vector3 Position { get; set; }
    public float ModelDisappearOnDeathTime { get; set; }
    public bool IsInvulnerable { get; set; }
    public bool IsTargetable { get; set; }
    public ushort TeamID { get; set; }
    public uint IsTargetableToTeamSpellFlags { get; set; }
    public int BytesRemaining { get; set; }
}

public class S2C_SpawnTurretParser : GamePacketParser
{
    protected override object ReadBody(uint routingNetId, ref SpanReader reader)
    {
        var data = new S2C_SpawnTurretData { RoutingNetID = routingNetId };

        data.NetID = reader.ReadUInt32LittleEndian();
        data.OwnerNetID = reader.ReadUInt32LittleEndian();
        data.NetNodeID = reader.ReadByte();

        data.Name = reader.ReadFixedString(64);
        data.SkinName = reader.ReadFixedString(64);
        data.SkinID = reader.ReadInt32LittleEndian();

        data.Position = new Vector3(
            reader.ReadSingleLittleEndian(),
            reader.ReadSingleLittleEndian(),
            reader.ReadSingleLittleEndian()
        );

        data.ModelDisappearOnDeathTime = reader.ReadSingleLittleEndian();

        byte bitfield = reader.ReadByte();
        data.IsInvulnerable = (bitfield & 0x01) != 0;
        data.IsTargetable = (bitfield & 0x02) != 0;

        data.TeamID = reader.ReadUInt16LittleEndian();
        data.IsTargetableToTeamSpellFlags = reader.ReadUInt32LittleEndian();

        data.BytesRemaining = reader.Remaining;
        return data;
    }
}