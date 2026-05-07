using System.Numerics;

namespace ReplayFiles.Core;

public class SpawnBotS2CData
{
    public uint RoutingNetID { get; set; }
    public uint NetID { get; set; }
    public byte NetNodeID { get; set; }
    public Vector3 Position { get; set; }
    public byte BotRank { get; set; }
    public ushort TeamID { get; set; }
    public int SkinID { get; set; }
    public string Name { get; set; } = "";
    public string SkinName { get; set; } = "";
    public int BytesRemaining { get; set; }
}

public class SpawnBotS2CParser : GamePacketParser
{
    protected override object ReadBody(uint routingNetId, ref SpanReader reader)
    {
        var data = new SpawnBotS2CData { RoutingNetID = routingNetId };

        data.NetID = reader.ReadUInt32LittleEndian();
        data.NetNodeID = reader.ReadByte();
        data.Position = new Vector3(
            reader.ReadSingleLittleEndian(),
            reader.ReadSingleLittleEndian(),
            reader.ReadSingleLittleEndian()
        );
        data.BotRank = reader.ReadByte();

        ushort bitfield = reader.ReadUInt16LittleEndian();
        data.TeamID = (ushort)(bitfield & 0x1FF);

        data.SkinID = reader.ReadInt32LittleEndian();
        data.Name = reader.ReadFixedString(64).TrimEnd('\0');
        data.SkinName = reader.ReadFixedString(64).TrimEnd('\0');

        data.BytesRemaining = reader.Remaining;
        return data;
    }
}