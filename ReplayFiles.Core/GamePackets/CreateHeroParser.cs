namespace ReplayFiles.Core;

public class CreateHeroData
{
    public uint RoutingNetID { get; set; }
    public uint NetID { get; set; }
    public int ClientID { get; set; }
    public string Name { get; set; } = "";
    public string Skin { get; set; } = "";
    public int BytesRemaining { get; set; }
}

public class CreateHeroParser : GamePacketParser
{
    protected override object ReadBody(uint routingNetId, ref SpanReader reader)
    {
        uint netId = reader.ReadUInt32LittleEndian();
        int clientId = reader.ReadInt32LittleEndian();
        byte netNodeId = reader.ReadByte();
        byte skillLevel = reader.ReadByte();

        byte bitfield1 = reader.ReadByte();
        byte botRank = reader.ReadByte();
        byte spawnPositionIndex = reader.ReadByte();
        int skinId = reader.ReadInt32LittleEndian();

        string name = reader.ReadFixedString(128);
        string skin = reader.ReadFixedString(40);

        float deathDurationRemaining = reader.ReadSingleLittleEndian();
        float timeSinceDeath = reader.ReadSingleLittleEndian();
        uint createHeroDeath = reader.ReadUInt32LittleEndian();

        byte bitfield2 = reader.ReadByte();

        return new CreateHeroData
        {
            RoutingNetID = routingNetId,
            NetID = netId,
            ClientID = clientId,
            Name = name,
            Skin = skin,
            BytesRemaining = reader.Remaining
        };
    }
}