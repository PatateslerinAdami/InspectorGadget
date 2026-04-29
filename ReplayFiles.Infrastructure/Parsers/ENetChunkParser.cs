using ReplayFiles.Core;
using System.Buffers.Binary;

namespace ReplayFiles.Infrastructure;

public class ENetChunkParser : IChunkParser
{
    private readonly Blowfish _blowfish;

    // State for reassembling fragmented packets.
    private readonly Dictionary<byte, Dictionary<ushort, FragmentBuffer>> _fragmentBuffers = new();

    private class FragmentBuffer
    {
        public int FragmentsLeft;
        public byte[] Buffer = [];
    }

    public ENetChunkParser(byte[] key)
    {
        _blowfish = new Blowfish(key);

        for (int i = 0; i < 256; i++)
        {
            _fragmentBuffers[(byte)i] = new Dictionary<ushort, FragmentBuffer>();
        }
    }

    public IEnumerable<Packet> ParseChunk(ReadOnlySpan<byte> chunkData, float chunkTime)
    {
        var reader = new SpanReader(chunkData);
        var packets = new List<Packet>();

        if (!ParseProtocolHeader(ref reader))
            return packets;

        while (reader.Remaining >= 4)
        {
            byte commandAndFlags = reader.ReadByte();
            byte command = (byte)(commandAndFlags & 0x0F);
            byte channel = reader.ReadByte();
            ushort reliableSequenceNumber = reader.ReadUInt16BigEndian();

            switch (command)
            {
                case 0x06: // SEND_RELIABLE
                    if (reader.Remaining < 2) return packets;
                    ushort relDataLength = reader.ReadUInt16BigEndian();
                    if (reader.Remaining < relDataLength) return packets;

                    var relData = reader.ReadBytes(relDataLength);
                    ProcessPayload(relData, chunkTime, channel, ENetPacketFlags.Reliable, packets);
                    break;

                case 0x07: // SEND_UNRELIABLE
                    if (reader.Remaining < 4) return packets;
                    reader.Skip(2); // Skip UnreliableSequenceNumber
                    ushort unrelDataLength = reader.ReadUInt16BigEndian();
                    if (reader.Remaining < unrelDataLength) return packets;

                    var unrelData = reader.ReadBytes(unrelDataLength);
                    ProcessPayload(unrelData, chunkTime, channel, ENetPacketFlags.None, packets);
                    break;

                case 0x09: // SEND_UNSEQUENCED
                    if (reader.Remaining < 4) return packets;
                    reader.Skip(2); // Skip UnsequencedGroup
                    ushort unseqDataLength = reader.ReadUInt16BigEndian();
                    if (reader.Remaining < unseqDataLength) return packets;

                    var unseqData = reader.ReadBytes(unseqDataLength);
                    ProcessPayload(unseqData, chunkTime, channel, ENetPacketFlags.Unsequenced, packets);
                    break;

                case 0x08: // SEND_FRAGMENT
                    if (reader.Remaining < 20) return packets;
                    ushort startSeqNum = reader.ReadUInt16BigEndian();
                    ushort fragDataLength = reader.ReadUInt16BigEndian();
                    uint fragCount = reader.ReadUInt32BigEndian();
                    uint fragNumber = reader.ReadUInt32BigEndian();
                    uint totalLength = reader.ReadUInt32BigEndian();
                    uint fragOffset = reader.ReadUInt32BigEndian();

                    if (reader.Remaining < fragDataLength) return packets;
                    var fragData = reader.ReadBytes(fragDataLength);

                    HandleFragment(channel, startSeqNum, fragCount, totalLength, fragOffset, fragData, chunkTime, packets);
                    break;

                // Skip commands that don't contain game data
                case 0x01: // ACKNOWLEDGE
                    if (reader.Remaining < 4) return packets;
                    reader.Skip(4);
                    break;
                case 0x02: // CONNECT
                    if (reader.Remaining < 36) return packets;
                    reader.Skip(36);
                    break;
                case 0x03: // VERIFY_CONNECT
                    if (reader.Remaining < 32) return packets;
                    reader.Skip(32);
                    break;
                case 0x04: // DISCONNECT
                    if (reader.Remaining < 4) return packets;
                    reader.Skip(4);
                    break;
                case 0x05: // PING
                    break;
                case 0x0A: // BANDWIDTH_LIMIT
                    if (reader.Remaining < 8) return packets;
                    reader.Skip(8);
                    break;
                case 0x0B: // THROTTLE_CONFIGURE
                    if (reader.Remaining < 12) return packets;
                    reader.Skip(12);
                    break;
                default:
                    // Unknown command, abort parsing this chunk to prevent reading out of alignment
                    return packets;
            }
        }

        return packets;
    }

    private bool ParseProtocolHeader(ref SpanReader reader)
    {
        if (reader.Remaining < 6) return false;
        reader.Skip(4); // Checksum
        reader.Skip(1); // SessionID
        byte peerId420 = reader.ReadByte();
        if ((peerId420 & 0x80) > 0)
        {
            if (reader.Remaining < 2) return false;
            reader.Skip(2); // TimeSent
        }
        return true;
    }

    private void HandleFragment(byte channel, ushort startSeqNum, uint fragCount, uint totalLength, uint fragOffset, ReadOnlySpan<byte> fragData, float time, List<Packet> packets)
    {
        const uint MaxReasonablePacketSize = 10 * 1024 * 1024;

        if (totalLength > MaxReasonablePacketSize ||
            fragOffset > totalLength ||
            fragOffset + (uint)fragData.Length > totalLength ||
            fragCount > 10000) // Sanity check on fragment count
        {
            return; // Drop corrupted fragment
        }

        var channelDict = _fragmentBuffers[channel];

        if (!channelDict.TryGetValue(startSeqNum, out var buffer))
        {
            buffer = new FragmentBuffer
            {
                FragmentsLeft = (int)fragCount,
                Buffer = new byte[totalLength]
            };
            channelDict[startSeqNum] = buffer;
        }

        // Safety check to prevent out of bounds on the reassembly buffer
        if (fragOffset + fragData.Length <= buffer.Buffer.Length)
        {
            fragData.CopyTo(buffer.Buffer.AsSpan((int)fragOffset));
            buffer.FragmentsLeft--;
        }

        // If we have received all fragments, process the complete payload
        if (buffer.FragmentsLeft <= 0)
        {
            ProcessPayload(buffer.Buffer, time, channel, ENetPacketFlags.Reliable, packets);
            channelDict.Remove(startSeqNum);
        }
    }
    private void ProcessPayload(ReadOnlySpan<byte> data, float time, byte channel, ENetPacketFlags flags, List<Packet> packets)
    {
        // Channels 0-7 are encrypted
        byte[] decryptedData = channel <= 7 ? _blowfish.Decrypt(data) : data.ToArray();

        // Check for Ubatch
        if (decryptedData.Length > 0 && decryptedData[0] == 0xFF && channel > 0 && channel < 5)
        {
            ParseUbatch(decryptedData, time, channel, flags, packets);
        }
        else
        {
            object? parsedObject = null;
            if (decryptedData.Length > 0)
            {
                uint packetId = decryptedData[0];
                parsedObject = PacketRegistry.TryParse(packetId, decryptedData);
            }

            packets.Add(new Packet
            {
                Time = time,
                Channel = channel,
                Flags = flags,
                Payload = decryptedData,
                ParsedData = parsedObject
            });
        }
    }

    private void ParseUbatch(ReadOnlySpan<byte> data, float time, byte channel, ENetPacketFlags flags, List<Packet> packets)
    {
        var reader = new SpanReader(data);
        reader.Skip(1); // Skip the 0xFF marker

        if (reader.Remaining < 1) return;
        int count = reader.ReadByte();
        if (reader.Remaining < 3 || count == 0) return;

        byte packetLastId = 0;
        int packetLastNetId = 0;

        for (int i = 0; i < count; i++)
        {
            if (reader.Remaining < 1) break;
            byte packetSize = reader.ReadByte();
            ReadOnlySpan<byte> packetData;

            if (i == 0)
            {
                if (reader.Remaining < 5) break;
                packetLastId = reader.ReadByte();
                packetLastNetId = reader.ReadInt32LittleEndian(); 
            }
            else
            {
                if ((packetSize & 1) == 0)
                {
                    if (reader.Remaining < 1) break;
                    packetLastId = reader.ReadByte();
                }

                if ((packetSize & 2) == 0)
                {
                    if (reader.Remaining < 4) break;
                    packetLastNetId = reader.ReadInt32LittleEndian();
                }
                else
                {
                    if (reader.Remaining < 1) break;
                    packetLastNetId += reader.ReadSByte();
                }

                if ((packetSize >> 2) == 0x3F)
                {
                    if (reader.Remaining < 1) break;
                    packetSize = reader.ReadByte();
                }
                else
                {
                    packetSize = (byte)(packetSize >> 2);
                }
            }

            int actualSize = i == 0 ? packetSize - 5 : packetSize;

            // Safety check
            if (actualSize < 0 || reader.Remaining < actualSize) break;

            packetData = reader.ReadBytes(actualSize);

            byte[] reconstructed = new byte[1 + 4 + actualSize];
            reconstructed[0] = packetLastId;
            BinaryPrimitives.WriteInt32LittleEndian(reconstructed.AsSpan(1, 4), packetLastNetId);
            packetData.CopyTo(reconstructed.AsSpan(5));

            object? parsedObject = null;
            if (reconstructed.Length > 0)
            {
                uint packetId = reconstructed[0];
                parsedObject = PacketRegistry.TryParse(packetId, reconstructed);
            }

            packets.Add(new Packet
            {
                Time = time,
                Channel = channel,
                Flags = flags,
                Payload = reconstructed,
                ParsedData = parsedObject
            });
        }
    }
}