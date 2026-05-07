using System.Numerics;

namespace ReplayFiles.Core;

public class SpawnLevelPropS2CData
{
    public uint RoutingNetID { get; set; }
    public uint NetID { get; set; }
    public byte NetNodeID { get; set; }
    public int SkinID { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 FacingDirection { get; set; }
    public Vector3 PositionOffset { get; set; }
    public Vector3 Scale { get; set; }
    public ushort TeamID { get; set; }
    public byte Rank { get; set; }
    public byte SkillLevel { get; set; }
    public byte Type { get; set; }
    public string Name { get; set; } = "";
    public string PropName { get; set; } = "";
    public int BytesRemaining { get; set; }
}

public class SpawnLevelPropS2CParser : GamePacketParser
{
    protected override object ReadBody(uint routingNetId, ref SpanReader reader)
    {
        var data = new SpawnLevelPropS2CData { RoutingNetID = routingNetId };

        data.NetID = reader.ReadUInt32LittleEndian();
        data.NetNodeID = reader.ReadByte();
        data.SkinID = reader.ReadInt32LittleEndian();

        data.Position = new Vector3(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian());
        data.FacingDirection = new Vector3(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian());
        data.PositionOffset = new Vector3(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian());
        data.Scale = new Vector3(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian());

        data.TeamID = reader.ReadUInt16LittleEndian();
        data.Rank = reader.ReadByte();
        data.SkillLevel = reader.ReadByte();

        data.Type = (byte)reader.ReadUInt32LittleEndian();

        data.Name = reader.ReadFixedString(64).TrimEnd('\0');
        data.PropName = reader.ReadFixedString(64).TrimEnd('\0');

        data.BytesRemaining = reader.Remaining;
        return data;
    }
}