using System.Buffers.Binary;

namespace ReplayFiles.Infrastructure;

public static class BdoDecompressor
{
    public static byte[] Decompress(ReadOnlySpan<byte> input)
    {
        if (input.IsEmpty) return [];

        byte flags = input[0];
        bool isLargeHeader = (flags & 0x2) != 0;

        // Parse header sizes based on the flag
        int compressedSize = isLargeHeader ? BinaryPrimitives.ReadInt32LittleEndian(input.Slice(1)) : input[1];
        int decompressedSize = isLargeHeader ? BinaryPrimitives.ReadInt32LittleEndian(input.Slice(5)) : input[2];
        int inputIndex = isLargeHeader ? 9 : 3;

        if (compressedSize != input.Length)
        {
            throw new InvalidDataException($"Compressed size mismatch. Expected {compressedSize}, got {input.Length}");
        }

        byte[] outputArray = new byte[decompressedSize];
        Span<byte> output = outputArray;

        // If the uncompressed flag is set, just copy the data directly
        if ((flags & 0x1) == 0)
        {
            input.Slice(inputIndex, decompressedSize).CopyTo(output);
            return outputArray;
        }

        int outputIndex = 0;

        // LZ77 Decompression loop
        for (uint block = 1; outputIndex < decompressedSize; block >>= 1)
        {
            if (block == 1)
            {
                block = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(inputIndex));
                inputIndex += 4;
            }

            if ((block & 1) != 0)
            {
                // It's a compressed sequence
                ParseCommand(input.Slice(inputIndex), out int length, out int distance, out int commandSize);

                for (int i = 0; i < length; i++)
                {
                    output[outputIndex + i] = output[outputIndex + i - distance];
                }

                inputIndex += commandSize;
                outputIndex += length;
            }
            else
            {
                // It's a literal byte
                output[outputIndex] = input[inputIndex];
                inputIndex += 1;
                outputIndex += 1;
            }
        }

        return outputArray;
    }

    /// <summary>
    /// Parses the LZ77 command to determine how many bytes to copy and from how far back.
    /// </summary>
    private static void ParseCommand(ReadOnlySpan<byte> input, out int length, out int distance, out int commandSize)
    {
        uint raw = BinaryPrimitives.ReadUInt32LittleEndian(input);

        switch (raw & 0x03)
        {
            case 0:
                length = 3;
                distance = (int)((raw >> 2) & 0x3F);
                commandSize = 1;
                break;
            case 1:
                length = 3;
                distance = (int)((raw >> 2) & 0x3FFF);
                commandSize = 2;
                break;
            case 2:
                length = (int)((raw >> 2) & 0xF) + 3;
                distance = (int)((raw >> 6) & 0x3FF);
                commandSize = 2;
                break;
            default: 
                int tempLength = (int)((raw >> 2) & 0x1F);
                if (tempLength != 0)
                {
                    length = tempLength + 2;
                    distance = (int)((raw >> 7) & 0x1FFFF);
                    commandSize = 3;
                }
                else
                {
                    length = (int)((raw >> 7) & 0xFF) + 3;
                    distance = (int)((raw >> 15) & 0x1FFFF);
                    commandSize = 4;
                }
                break;
        }
    }
}