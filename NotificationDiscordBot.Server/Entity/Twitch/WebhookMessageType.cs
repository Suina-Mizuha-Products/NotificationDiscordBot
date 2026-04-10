namespace NotificationDiscordBot.Server.Entity.Twitch;

/// <summary>
/// TwitchからのWebhookリクエストのMessageTypeを表すクラス。
/// </summary>
public class WebhookMessageType
{
    /// <summary>
    /// TwitchからのWebhookリクエストのMessageTypeを表す定数。Twitchのイベントが発生した際に送信される通常の通知リクエストで使用される。
    /// </summary>
    public const string Notification = "notification";

    /// <summary>
    /// TwitchからのWebhookリクエストのMessageTypeを表す定数。Webhookの登録や更新、削除の際にTwitchから送信される検証用のリクエストで使用される。
    /// </summary>
    public const string Verification = "webhook_callback_verification";

    /// <summary>
    /// TwitchからのWebhookリクエストのMessageTypeを表す定数。Webhookの登録や更新、削除の際にTwitchから送信される検証用のリクエストで使用される。
    /// </summary>
    public const string Revocation = "revocation";
}
