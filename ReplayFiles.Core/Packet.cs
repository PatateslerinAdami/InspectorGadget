using System.Buffers.Binary;

namespace ReplayFiles.Core
{
    public record Packet
    {
        public required float Time { get; init; }
        public required byte Channel { get; init; }
        public required ENetPacketFlags Flags { get; init; }
        public uint PacketId
        {
            get
            {
                if (Payload.Length == 0) return 0;

                uint id = Payload.Span[0];
                if (id == 0xFE && Payload.Length >= 7)
                {
                    id = BinaryPrimitives.ReadUInt16LittleEndian(Payload.Span.Slice(5, 2));
                }
                return id;
            }
        }
        public string PacketName
        {
            get
            {
                if (Payload.Length == 0) return "Empty";

                uint id = PacketId;

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
