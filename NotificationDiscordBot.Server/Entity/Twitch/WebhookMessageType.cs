namespace NotificationDiscordBot.Server.Entity.Twitch;

/// <summary>
/// TwitchからのWebhookリクエストのMessageTypeを表すクラス。
/// </summary>
public class WebhookMessageType
{
    /// <summary>
    /// TwitchからのWebhookリクエストのMessageTypeを表す定数。Twitchのイベントが発生した際に送信される通常の通知リクエストで使用される。
    /// </summary>
    public const string NOTIFICATION = "notification";

    /// <summary>
    /// TwitchからのWebhookリクエストのMessageTypeを表す定数。Webhookの登録や更新、削除の際にTwitchから送信される検証用のリクエストで使用される。
    /// </summary>
    public const string VERIFICATION = "webhook_callback_verification";

    /// <summary>
    /// TwitchからのWebhookリクエストのMessageTypeを表す定数。Webhookの購読が取り消しまたは失効された際にTwitchから送信される通知リクエストで使用される。
    /// </summary>
    public const string REVOCATION = "revocation";
}
