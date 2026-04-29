using ReplayFiles.Core;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
namespace ReplayFiles.Infrastructure;

public class SpectatorChunkParser : IChunkParser
{
    private readonly Blowfish _blowfish;

    private readonly List<byte> _fragmentBuffer = new();
    private int _expectedFragmentLength = 0;
    private float _fragmentTime = 0;

    private static readonly byte[] HttpEndMarker = { 0x0D, 0x0A, 0x0D, 0x0A }; // \r\n\r\n
    private static readonly byte[] ContentLengthHeader = Encoding.ASCII.GetBytes("Content-Length: ");

    public SpectatorChunkParser(long matchId, byte[] encryptedKey)
    {
        byte[] matchIdBytes = Encoding.ASCII.GetBytes(matchId.ToString());
        Blowfish keyDerivationBlowfish = new Blowfish(matchIdBytes);

        byte[] decryptedKey = keyDerivationBlowfish.Decrypt(encryptedKey);

        _blowfish = new Blowfish(decryptedKey.AsSpan(0, 16));
    }

    public IEnumerable<Packet> ParseChunk(ReadOnlySpan<byte> chunkData, float chunkTime)
    {
        if (_expectedFragmentLength > 0)
        {
            _fragmentBuffer.AddRange(chunkData.ToArray());

            if (_fragmentBuffer.Count >= _expectedFragmentLength)
            {
                byte[] completePayload = _fragmentBuffer.ToArray();
                _fragmentBuffer.Clear();
                _expectedFragmentLength = 0;

                return ProcessPayload(completePayload, _fragmentTime);
            }
            return Array.Empty<Packet>(); 
        }

        if (chunkData.Length > 4 && chunkData.Slice(0, 4).SequenceEqual("HTTP"u8))
        {
            int headerEndIndex = chunkData.IndexOf(HttpEndMarker);
            if (headerEndIndex == -1) return Array.Empty<Packet>();

            ReadOnlySpan<byte> header = chunkData.Slice(0, headerEndIndex);
            ReadOnlySpan<byte> body = chunkData.Slice(headerEndIndex + 4);

            if (header.IndexOf("application/octet-stream"u8) == -1)
                return Array.Empty<Packet>();

            int contentLength = ExtractContentLength(header);

            if (body.Length < contentLength)
            {
                _expectedFragmentLength = contentLength;
                _fragmentTime = chunkTime;
                _fragmentBuffer.AddRange(body.ToArray());
                return Array.Empty<Packet>();
            }

            return ProcessPayload(body, chunkTime);
        }

        return Array.Empty<Packet>();
    }

    private int ExtractContentLength(ReadOnlySpan<byte> header)
    {
        int index = header.IndexOf(ContentLengthHeader);
        if (index == -1) return 0;

        ReadOnlySpan<byte> slice = header.Slice(index + ContentLengthHeader.Length);
        int end = slice.IndexOf((byte)'\r');
        if (end == -1) end = slice.Length;

        string lengthStr = Encoding.ASCII.GetString(slice.Slice(0, end));
        return int.TryParse(lengthStr, out int length) ? length : 0;
    }

    private IEnumerable<Packet> ProcessPayload(ReadOnlySpan<byte> encryptedBody, float time)
    {
        byte[] decrypted = _blowfish.Decrypt(encryptedBody);

        using MemoryStream compressedStream = new MemoryStream(decrypted);
        using GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using MemoryStream decompressedStream = new MemoryStream();
        gzipStream.CopyTo(decompressedStream);

        SpanReader reader = new SpanReader(decompressedStream.ToArray());
        List<Packet> packets = new List<Packet>();

        float currentTime = 0.0f;
        byte currentPacketType = 0;
        int currentBlockParam = 0;

        while (!reader.IsEof)
        {
            byte marker = reader.ReadByte();
            byte flags = (byte)(marker >> 4);
            byte channel = (byte)(marker & 0x0F);

            if ((flags & 0x8) == 0) currentTime = reader.ReadSingleLittleEndian();
            else currentTime += reader.ReadByte() / 1000.0f;

            int length = (flags & 0x1) == 0 ? reader.ReadInt32LittleEndian() : reader.ReadByte();

            if ((flags & 0x4) == 0) currentPacketType = reader.ReadByte();

            if ((flags & 0x2) == 0) currentBlockParam = reader.ReadInt32LittleEndian();
            else currentBlockParam += reader.ReadByte();

            ReadOnlySpan<byte> packetData = reader.ReadBytes(length);

            byte[] finalPayload = new byte[1 + length];
            finalPayload[0] = currentPacketType;
            packetData.CopyTo(finalPayload.AsSpan(1));

            packets.Add(new Packet
            {
                Time = currentTime,
                Channel = channel,
                Flags = ENetPacketFlags.None,
                Payload = finalPayload,
              
            });
            //BinaryPrimitives.WriteInt32LittleEndian(finalPayload.AsSpan(1, 4), currentBlockParam);
            //packetData.CopyTo(finalPayload.AsSpan(5));

            packets.Add(new Packet
            {
                Time = currentTime,
                Channel = channel,
                Flags = ENetPacketFlags.None,
                Payload = finalPayload
            });
        }

        return packets;
    }
}