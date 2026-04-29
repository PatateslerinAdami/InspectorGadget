using ReplayFiles.Core;

namespace ReplayFiles.Infrastructure;

public interface IChunkParser
{
    IEnumerable<Packet> ParseChunk(ReadOnlySpan<byte> chunkData, float chunkTime);
}