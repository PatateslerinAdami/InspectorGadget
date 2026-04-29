using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReplayFiles.Core;

public record DataOffset
{
    [JsonPropertyName("offset")]
    public int Offset { get; init; }

    [JsonPropertyName("size")]
    public int Size { get; init; }
}
public record ReplayPlayer
{
    [JsonPropertyName("champion")]
    public string Champion { get; init; } = string.Empty;

    [JsonPropertyName("summoner")]
    public string Summoner { get; init; } = string.Empty;
}

public record ReplayMetadata
{
    [JsonPropertyName("replayVersion")]
    public string ReplayVersion { get; init; } = string.Empty;

    [JsonPropertyName("clientVersion")]
    public string ClientVersion { get; init; } = string.Empty;

    [JsonPropertyName("clientHash")]
    public string ClientHash { get; init; } = string.Empty;

    [JsonPropertyName("encryptionKey")]
    public byte[] EncryptionKey { get; init; } = [];

    [JsonPropertyName("matchID")]
    public long MatchId { get; init; }

    [JsonPropertyName("spectatorMode")]
    public bool SpectatorMode { get; init; }

    [JsonPropertyName("players")]
    public List<ReplayPlayer> Players { get; init; } = new List<ReplayPlayer>();

    [JsonPropertyName("dataIndex")]
    public JsonElement DataIndexRaw { get; init; }

    [JsonIgnore]
    public Dictionary<string, DataOffset> DataIndex
    {
        get
        {
            var dict = new Dictionary<string, DataOffset>();
            if (DataIndexRaw.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in DataIndexRaw.EnumerateArray())
                {
                    if (item.TryGetProperty("Key", out var keyProp) && item.TryGetProperty("Value", out var valProp))
                    {
                        dict[keyProp.GetString()!] = valProp.Deserialize<DataOffset>()!;
                    }
                }
            }
            else if (DataIndexRaw.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in DataIndexRaw.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.Deserialize<DataOffset>()!;
                }
            }
            return dict;
        }
    }
}