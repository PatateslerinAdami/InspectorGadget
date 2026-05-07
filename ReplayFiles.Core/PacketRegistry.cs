namespace ReplayFiles.Core
{
    public static class PacketRegistry
    {
        private static readonly Dictionary<uint, IPacketParser> _parsers = new();

        static PacketRegistry()
        {
            Register(0x87, new FXCreateGroupParser());
            Register(0x7C, new SpawnMinionS2CParser());
            Register(0x4C, new CreateHeroParser());
            Register(0x5C, new StartGameParser());
            Register(0x7B, new NPC_BuffRemove2Parser());
            Register(0xB7, new NPC_BuffAdd2Parser());
            Register(0xB5, new NPC_CastSpellAnsParser());
            Register(0x3B, new MissileReplicationParser());
            Register(0x10F, new S2C_UnitSetLookAtParser());
            Register(0x6C, new S2C_ChainMissileSyncParser());
            Register(0x6B, new S2C_SetAnimStatesParser());
            Register(0x29, new S2C_StopAnimationParser());
            Register(0xB0, new S2C_PlayAnimationParser());
            Register(0xCF, new SpawnBotS2CParser());
            Register(0xD0, new SpawnLevelPropS2CParser());
            Register(0x123, new S2C_SpawnTurretParser());
            Register(0xBA, new OnEnterVisibilityClientParser());
            Register(0x23, new AddRegionParser());
            Register(0x24, new S2C_MoveRegionParser());
            Register(0x33, new RemoveRegionParser());
            Register(0x12E, new AddConeRegionParser());
            Register(0x61, new WaypointGroupParser());
            Register(0x64, new WaypointGroupWithSpeedParser());
            Register(0xB9, new WaypointListParser());
            Register(0x83, new WaypointListHeroWithSpeedParser());
            Register(0x3C, new MovementDriverReplicationParser());
        }

        private static void Register(uint packetId, IPacketParser parser)
        {
            _parsers[packetId] = parser;
        }

        public static object? TryParse(uint packetId, ReadOnlySpan<byte> payload)
        {
            try
            {
                if (_parsers.TryGetValue(packetId, out var parser))
                {
                    return parser.Parse(payload);
                }
            }
            catch
            {

            }
            return null;
        }
    }
}
