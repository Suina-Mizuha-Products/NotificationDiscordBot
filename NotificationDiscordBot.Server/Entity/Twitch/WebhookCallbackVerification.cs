using System.Text.Json.Serialization;

namespace NotificationDiscordBot.Server.Entity.Twitch;

public class WebhookCallbackVerification: WebhookCallbackBase
{
        [JsonPropertyName("challenge")]
        public string Challenge { get; set; } = string.Empty;
}
