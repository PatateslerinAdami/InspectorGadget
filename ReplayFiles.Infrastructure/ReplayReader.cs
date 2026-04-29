using ReplayFiles.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace ReplayFiles.Infrastructure;

public class ReplayReader : IReplayParser
{
    public IEnumerable<Packet> Parse(Stream replayStream)
    {
        using BinaryReader reader = new BinaryReader(replayStream, Encoding.UTF8, leaveOpen: true);

        byte unused = reader.ReadByte();
        byte headerVersion = reader.ReadByte();
        byte compressed = reader.ReadByte();
        byte reserved = reader.ReadByte();

        int jsonLength = reader.ReadInt32();
        byte[] jsonBytes = reader.ReadBytes(jsonLength);

        if (jsonBytes.Length != jsonLength)
            throw new EndOfStreamException("Unexpected end of file while reading JSON metadata.");

        var jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        };
        ReplayMetadata? metadata = JsonSerializer.Deserialize<ReplayMetadata>(jsonBytes, jsonOptions);

        if (metadata == null || !metadata.DataIndex.TryGetValue("stream", out DataOffset? streamOffset))
            throw new InvalidDataException("Invalid replay metadata or missing 'stream' data index.");

        long offsetStartPosition = replayStream.Position;

        replayStream.Position = offsetStartPosition + streamOffset.Offset;
        byte[] streamData = reader.ReadBytes(streamOffset.Size);

        if (streamData.Length != streamOffset.Size)
            throw new EndOfStreamException("Unexpected end of file while reading stream data.");

        ReadOnlySpan<byte> processedData = streamData;
        if (streamData.Length > 0 && (streamData[0] & 0x4C) != 0)
        {
            processedData = BdoDecompressor.Decompress(streamData);
        }

        IChunkParser chunkParser = metadata.SpectatorMode
            ? new SpectatorChunkParser(metadata.MatchId, metadata.EncryptionKey)
            : new ENetChunkParser(metadata.EncryptionKey);

        return ParseChunks(processedData, chunkParser);
    }

    private IEnumerable<Packet> ParseChunks(ReadOnlySpan<byte> data, IChunkParser parser)
    {
        var reader = new SpanReader(data);
        var allPackets = new List<Packet>();

        while (reader.Remaining >= 9) 
        {
            float chunkTime = reader.ReadSingleLittleEndian();
            int chunkLength = reader.ReadInt32LittleEndian();

            if (reader.Remaining < chunkLength + 1)
                break;

            ReadOnlySpan<byte> chunkData = reader.ReadBytes(chunkLength);

            IEnumerable<Packet> parsedPackets = parser.ParseChunk(chunkData, chunkTime);
            allPackets.AddRange(parsedPackets);

            byte chunkUnk = reader.ReadByte();
        }

        return allPackets;
    }
}