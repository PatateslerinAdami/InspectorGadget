using System.Numerics;

namespace ReplayFiles.Core;

public class NPC_CastSpellAnsData
{
    public uint RoutingNetID { get; set; }
    public int CasterPositionSyncID { get; set; }
    public bool Unknown1 { get; set; }
    // CastInfo
    public uint SpellHash { get; set; }
    public uint SpellNetID { get; set; }
    public byte SpellLevel { get; set; }
    public float AttackSpeedModifier { get; set; }
    public uint CasterNetID { get; set; }
    public uint SpellChainOwnerNetID { get; set; }
    public uint PackageHash { get; set; }
    public uint MissileNetID { get; set; }
    public Vector3 TargetPosition { get; set; }
    public Vector3 TargetPositionEnd { get; set; }
    public List<uint> TargetNetIDs { get; set; } = new List<uint>();

    public float DesignerCastTime { get; set; }
    public float ExtraCastTime { get; set; }
    public float DesignerTotalTime { get; set; }
    public float Cooldown { get; set; }
    public float StartCastTime { get; set; }

    public bool IsAutoAttack { get; set; }
    public bool IsSecondAutoAttack { get; set; }
    public bool IsForceCastingOrChannel { get; set; }
    public bool IsOverrideCastPosition { get; set; }
    public bool IsClickCasted { get; set; }

    public byte SpellSlot { get; set; }
    public float ManaCost { get; set; }
    public Vector3 SpellCastLaunchPosition { get; set; }
    public int AmmoUsed { get; set; }
    public float AmmoRechargeTime { get; set; }

    public int BytesRemaining { get; set; }
}

public class NPC_CastSpellAnsParser : GamePacketParser
{
    protected override object ReadBody(uint routingNetId, ref SpanReader reader)
    {
        var data = new NPC_CastSpellAnsData { RoutingNetID = routingNetId };

        data.CasterPositionSyncID = reader.ReadInt32LittleEndian();
        byte bitfield1 = reader.ReadByte();
        data.Unknown1 = (bitfield1 & 1) != 0;

        ushort castInfoSize = reader.ReadUInt16LittleEndian();

        data.SpellHash = reader.ReadUInt32LittleEndian();
        data.SpellNetID = reader.ReadUInt32LittleEndian();
        data.SpellLevel = reader.ReadByte();
        data.AttackSpeedModifier = reader.ReadSingleLittleEndian();
        data.CasterNetID = reader.ReadUInt32LittleEndian();
        data.SpellChainOwnerNetID = reader.ReadUInt32LittleEndian();
        data.PackageHash = reader.ReadUInt32LittleEndian();
        data.MissileNetID = reader.ReadUInt32LittleEndian();

        data.TargetPosition = new Vector3(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian());
        data.TargetPositionEnd = new Vector3(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian());

        byte targetCount = reader.ReadByte();
        for (int i = 0; i < targetCount; i++)
        {
            data.TargetNetIDs.Add(reader.ReadUInt32LittleEndian());
            byte hitResult = reader.ReadByte();
        }

        data.DesignerCastTime = reader.ReadSingleLittleEndian();
        data.ExtraCastTime = reader.ReadSingleLittleEndian();
        data.DesignerTotalTime = reader.ReadSingleLittleEndian();
        data.Cooldown = reader.ReadSingleLittleEndian();
        data.StartCastTime = reader.ReadSingleLittleEndian();

        byte bitfield2 = reader.ReadByte();
        data.IsAutoAttack = (bitfield2 & 1) != 0;
        data.IsSecondAutoAttack = (bitfield2 & 2) != 0;
        data.IsForceCastingOrChannel = (bitfield2 & 4) != 0;
        data.IsOverrideCastPosition = (bitfield2 & 8) != 0;
        data.IsClickCasted = (bitfield2 & 16) != 0;

        data.SpellSlot = reader.ReadByte();
        data.ManaCost = reader.ReadSingleLittleEndian();
        data.SpellCastLaunchPosition = new Vector3(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian());
        data.AmmoUsed = reader.ReadInt32LittleEndian();
        data.AmmoRechargeTime = reader.ReadSingleLittleEndian();

        data.BytesRemaining = reader.Remaining;
        return data;
    }
}