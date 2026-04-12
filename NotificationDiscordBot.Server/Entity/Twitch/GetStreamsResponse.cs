using System.Text.Json.Serialization;

namespace NotificationDiscordBot.Server.Entity.Twitch;

public class GetStreamsResponse
{
    [JsonPropertyName("data")]
    public StreamData[] Data { get; set; } = [];

    [JsonPropertyName("pagination")]
    public Pagination Pagination { get; set; } = new();
}
