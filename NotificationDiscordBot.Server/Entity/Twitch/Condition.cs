using System.Text.Json.Serialization;

namespace NotificationDiscordBot.Server.Entity.Twitch;

public class Condition
{
    [JsonPropertyName("broadcaster_user_id")]
    public string BroadcasterUserId { get; set; } = string.Empty;
}
