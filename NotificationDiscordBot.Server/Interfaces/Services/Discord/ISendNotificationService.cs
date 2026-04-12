namespace NotificationDiscordBot.Server.Interfaces.Services.Discord;

/// <summary>
/// Discrodチャンネルに通知を送信するサービスクラス
/// </summary>
public interface ISendNotificationService
{
    Task<bool> SendTwitchOnlineNotificationMessageAsync(string userLogin, string userName, string profileImageUrl, string title, string gameName, string startedAt, string thumnailUrl);
}
