using System.Text.Json.Serialization;

namespace NotificationDiscordBot.Server.Entity.Twitch;

public class WebhookCallbackNotification: WebhookCallbackBase
{
    [JsonPropertyName("event")]
    public EventBase Event { get; set; } = null!;
}
