namespace ReplayFiles.Core
{
    [Flags]
    public enum ENetPacketFlags : byte
    {
        None = 0,
        Unsequenced = 1 << 6,
        Reliable = 1 << 7,
        ReliableUnsequenced = Reliable | Unsequenced
    }
    [Flags]
    public enum AnimationFlags : byte
    {
        UniqueOverride = 1 << 0,
        Unknown2 = 1 << 1,
        Override = 1 << 2,
        Lock = 1 << 3,
        Unknown5 = 1 << 4,
        Unknown6 = 1 << 5,
        Unknown7 = 1 << 6,
        Unknown8 = 1 << 7
    }
    [Flags]
    public enum FXFlags : ushort
    {
        GivenDirection = 1 << 4,
        BindDirection = 1 << 5,
        Unknown3 = GivenDirection | BindDirection,
        Unknown4 = 1 << 6,
        TargetDirection = 1 << 7,
        Unknown6 = BindDirection | TargetDirection,
        Unknown7 = 1 << 8,
        Unknown8 = GivenDirection | Unknown7,
        Unknown9 = BindDirection | TargetDirection | Unknown7,
        Unknown10 = BindDirection | Unknown4 | TargetDirection | Unknown7
    }
    public enum BuffType : byte
    {
        INTERNAL,
        AURA,
        COMBAT_ENCHANCER,
        COMBAT_DEHANCER,
        SPELL_SHIELD,
        STUN,
        INVISIBILITY,
        SILENCE,
        TAUNT,
        POLYMORPH,
        SLOW,
        SNARE,
        DAMAGE,
        HEAL,
        HASTE,
        SPELL_IMMUNITY,
        PHYSICAL_IMMUNITY,
        INVULNERABILITY,
        SLEEP,
        NEAR_SIGHT,
        FRENZY,
        FEAR,
        CHARM,
        POISON,
        SUPPRESSION,
        BLIND,
        COUNTER,
        SHRED,
        FLEE,
        KNOCKUP,
        KNOCKBACK,
        DISARM
    }
}
