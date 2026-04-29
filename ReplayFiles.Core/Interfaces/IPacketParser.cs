namespace ReplayFiles.Core
{
    public interface IPacketParser
    {
        object Parse(ReadOnlySpan<byte> payload);
    }
}
