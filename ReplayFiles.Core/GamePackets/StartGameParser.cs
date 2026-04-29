namespace ReplayFiles.Core;

public class StartGameData
{
    public bool EnablePause { get; set; }
}

public class StartGameParser : GamePacketParser
{
    protected override object ReadBody(uint routingNetId, ref SpanReader reader)
    {
        byte bitfield = reader.ReadByte();
        bool enablePause = (bitfield & 1) != 0;

        return new StartGameData
        {
            EnablePause = enablePause
        };
    }
}