using System.Text.Json.Serialization;

namespace NotificationDiscordBot.Server.Entity.Twitch;

/// <summary>
/// Webhookのコールバックの基本クラス。TwitchからのWebhookコールバックは、サブスクリプション情報を含むため、Subscriptionプロパティを持つ。
/// </summary>
public abstract class WebhookCallbackBase
{
    [JsonPropertyName("subscription")]
    public Subscription Subscription { get; set; } = new();
}
