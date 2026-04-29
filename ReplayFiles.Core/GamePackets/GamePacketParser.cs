namespace ReplayFiles.Core.GamePackets;

public abstract class GamePacketParser : IPacketParser
{
    public object? Parse(ReadOnlySpan<byte> payload)
    {
        var reader = new SpanReader(payload);

        if (reader.Remaining < 5)
            return null;

        byte packetId = reader.ReadByte();
        uint routingNetId = reader.ReadUInt32LittleEndian();

        return ReadBody(routingNetId, ref reader);
    }

    protected abstract object ReadBody(uint routingNetId, ref SpanReader reader);
}