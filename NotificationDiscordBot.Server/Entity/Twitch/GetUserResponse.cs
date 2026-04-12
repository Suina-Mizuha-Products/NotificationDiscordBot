using System.Text.Json.Serialization;

namespace NotificationDiscordBot.Server.Entity.Twitch;

public class GetUserResponse
{
    [JsonPropertyName("data")]
    public UserInfo[] Data { get; set; } = [];
}
