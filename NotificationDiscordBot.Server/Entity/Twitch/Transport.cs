using System.Text.Json.Serialization;

namespace NotificationDiscordBot.Server.Entity.Twitch;

public class Transport
{
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("callback")]
    public string Callback { get; set; } = string.Empty;
}
