namespace ReplayFiles.Core
{
    public record Packet
    {
        public required float Time { get; init; }
        public required byte Channel { get; init; }
        public required ENetPacketFlags Flags { get; init; }

        public string PacketName
        {
            get
            {
                if (Payload.Length == 0) return "Empty";

                uint id = Payload.Span[0];

                if (Enum.IsDefined(typeof(GamePacketID), id))
                {
                    return ((GamePacketID)id).ToString();
                }

                return $"Unknown_0x{id:X2}";
            }
        }
        public object? ParsedData { get; init; }
        public required ReadOnlyMemory<byte> Payload { get; init; }
    }
}
