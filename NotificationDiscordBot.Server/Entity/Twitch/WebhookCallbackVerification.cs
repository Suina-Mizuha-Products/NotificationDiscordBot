using System.Text.Json.Serialization;

namespace NotificationDiscordBot.Server.Entity.Twitch;

/// <summary>
/// 検証リクエストのペイロードを表すクラス。
/// </summary>
public class WebhookCallbackVerification: WebhookCallbackBase
{
        [JsonPropertyName("challenge")]
        public string Challenge { get; set; } = string.Empty;
}
