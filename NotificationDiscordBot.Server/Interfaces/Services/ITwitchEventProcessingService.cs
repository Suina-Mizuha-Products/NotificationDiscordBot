namespace NotificationDiscordBot.Server.Interfaces.Services;

/// <summary>
/// Twitch EventSub からのイベントを処理するサービスのインターフェースです。Twitch Webhook コントローラーから呼び出され、受信したイベントの内容に応じて適切な処理を行います。例えば、特定のイベントタイプに基づいて Discord への通知をトリガーするなどのビジネスロジックを実装します。
/// </summary>
public interface ITwitchEventProcessingService
{
    Task<bool> ProcessNotificationAsync(string messageId, string requestBody, CancellationToken cancellationToken);
    Task<string?> ProcessVerificationAsync(string messageId, string requestBody);
    Task<bool> ProcessRevocationAsync(string messageId, string requestBody, CancellationToken cancellationToken);
}
