using System.Numerics;
using System.Collections.Generic;

namespace ReplayFiles.Core;

public class CharacterStackData
{
    public string SkinName { get; set; } = "";
    public uint SkinID { get; set; }
    public bool OverrideSpells { get; set; }
    public bool ModelOnly { get; set; }
    public bool ReplaceCharacterPackage { get; set; }
    public uint NetID { get; set; }
}

public class OnEnterVisibilityClientData
{
    public uint RoutingNetID { get; set; }
    public List<object> EmbeddedPackets { get; set; } = new();
    public List<CharacterStackData> CharacterDataStack { get; set; } = new();
}

public class OnEnterVisibilityClientParser : GamePacketParser
{
    protected override object ReadBody(uint routingNetId, ref SpanReader reader)
    {
        var data = new OnEnterVisibilityClientData { RoutingNetID = routingNetId };

        int totalSize = reader.ReadUInt16LittleEndian() & 0x1FFF;
        while (totalSize > 0 && reader.Remaining >= 2)
        {
            ushort size = reader.ReadUInt16LittleEndian();
            totalSize -= 2;

            if (reader.Remaining < size) break;

            var packetData = reader.ReadBytes(size);
            totalSize -= size;

            if (packetData.Length > 0)
            {
                uint packetId = packetData[0];
                if (packetId == 0xFE && packetData.Length >= 7)
                {
                    packetId = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(packetData.Slice(5, 2));
                }

                var parsed = PacketRegistry.TryParse(packetId, packetData);
                if (parsed != null)
                {
                    data.EmbeddedPackets.Add(parsed);
                }
            }
        }

        if (reader.Remaining < 8) return data;
        if (reader.Remaining == 1) { reader.Skip(1); return data; }

        byte itemCount = reader.ReadByte();
        reader.Skip(itemCount * 7); 

        if (reader.Remaining < 1) return data;
        bool hasShield = reader.ReadByte() != 0;
        if (hasShield)
        {
            if (reader.Remaining < 12) return data;
            reader.Skip(12);
        }

        if (reader.Remaining < 4) return data;
        int countCharStack = reader.ReadInt32LittleEndian();
        for (int i = 0; i < countCharStack; i++)
        {
            if (reader.Remaining < 4) break;
            int strLen = reader.ReadInt32LittleEndian();
            if (reader.Remaining < strLen) break;

            string skinName = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(strLen));

            if (reader.Remaining < 9) break;
            uint skinId = reader.ReadUInt32LittleEndian();
            byte bitfield = reader.ReadByte();
            uint netId = reader.ReadUInt32LittleEndian();

            data.CharacterDataStack.Add(new CharacterStackData
            {
                SkinName = skinName,
                SkinID = skinId,
                OverrideSpells = (bitfield & 1) != 0,
                ModelOnly = (bitfield & 2) != 0,
                ReplaceCharacterPackage = (bitfield & 4) != 0,
                NetID = netId
            });
        }

        return data;
    }
}