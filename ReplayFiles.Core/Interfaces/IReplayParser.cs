namespace ReplayFiles.Core
{
    public interface IReplayParser
    {
        IEnumerable<Packet> Parse(Stream replayStream);
    }
}
