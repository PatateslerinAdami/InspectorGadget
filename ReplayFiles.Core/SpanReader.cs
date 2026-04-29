using System.Buffers.Binary;

namespace ReplayFiles.Core;

public ref struct SpanReader
{
    private ReadOnlySpan<byte> _span;
    public int Position { get; private set; }

    public SpanReader(ReadOnlySpan<byte> span)
    {
        _span = span;
        Position = 0;
    }

    public int Remaining => _span.Length - Position;
    public bool IsEof => Remaining <= 0;

    public byte ReadByte()
    {
        byte value = _span[Position];
        Position += 1;
        return value;
    }

    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        var slice = _span.Slice(Position, count);
        Position += count;
        return slice;
    }

    public ushort ReadUInt16BigEndian()
    {
        ushort value = BinaryPrimitives.ReadUInt16BigEndian(_span.Slice(Position));
        Position += 2;
        return value;
    }

    public uint ReadUInt32BigEndian()
    {
        uint value = BinaryPrimitives.ReadUInt32BigEndian(_span.Slice(Position));
        Position += 4;
        return value;
    }

    public int ReadInt32BigEndian()
    {
        int value = BinaryPrimitives.ReadInt32BigEndian(_span.Slice(Position));
        Position += 4;
        return value;
    }
    public int ReadInt32LittleEndian()
    {
        int value = BinaryPrimitives.ReadInt32LittleEndian(_span.Slice(Position));
        Position += 4;
        return value;
    }
    public float ReadSingleLittleEndian()
    {
        float value = BinaryPrimitives.ReadSingleLittleEndian(_span.Slice(Position));
        Position += 4;
        return value;
    }
    public sbyte ReadSByte()
    {
        sbyte value = (sbyte)_span[Position];
        Position += 1;
        return value;
    }
    public uint ReadUInt32LittleEndian()
    {
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(_span.Slice(Position));
        Position += 4;
        return value;
    }
    public ushort ReadUInt16LittleEndian()
    {
        ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_span.Slice(Position));
        Position += 2;
        return value;
    }

    public short ReadInt16LittleEndian()
    {
        short value = BinaryPrimitives.ReadInt16LittleEndian(_span.Slice(Position));
        Position += 2;
        return value;
    }
    public string ReadFixedString(int length)
    {
        int actualLength = Math.Min(length, Remaining);
        var slice = _span.Slice(Position, actualLength);

        Position += actualLength;

        int nullIndex = slice.IndexOf((byte)0);
        if (nullIndex != -1)
        {
            slice = slice.Slice(0, nullIndex);
        }

        return System.Text.Encoding.UTF8.GetString(slice);
    }
    public void Skip(int count)
    {
        Position += count;
    }
}