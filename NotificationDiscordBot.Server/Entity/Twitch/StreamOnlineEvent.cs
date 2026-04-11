using System.Text.Json.Serialization;

namespace NotificationDiscordBot.Server.Entity.Twitch;

/// <summary>
/// 配信開始イベントクラス
/// </summary>
public class StreamOnlineEvent: EventBase
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("broadcaster_user_id")]
    public string BroadcasterUserId { get; set; } = string.Empty;

    [JsonPropertyName("broadcaster_user_login")]
    public string BroadcasterUserLogin { get; set; } = string.Empty;

    [JsonPropertyName("broadcaster_user_name")]
    public string BroadcasterUserName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("started_at")]
    public string StartedAt { get; set; } = string.Empty;
}
