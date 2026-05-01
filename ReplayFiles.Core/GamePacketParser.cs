namespace ReplayFiles.Core;

public abstract class GamePacketParser : IPacketParser
{
    public object? Parse(ReadOnlySpan<byte> payload)
    {
        var reader = new SpanReader(payload);

        if (reader.Remaining < 5)
            return null;

        byte marker = reader.ReadByte();
        uint routingNetId = reader.ReadUInt32LittleEndian();

        if (marker == 0xFE)
        {
            if (reader.Remaining < 2) return null;
            reader.Skip(2);
        }

        return ReadBody(routingNetId, ref reader);
    }

    protected abstract object ReadBody(uint routingNetId, ref SpanReader reader);
}