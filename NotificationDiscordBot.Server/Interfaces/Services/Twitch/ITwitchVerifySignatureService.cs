namespace NotificationDiscordBot.Server.Interfaces.Services.Twitch;

public interface ITwitchVerifySignatureService
{
    /// <summary>
    /// Twitchからのリクエストの署名を検証するメソッド。
    /// </summary>
    /// <param name="messageId">TwitchからのメッセージId</param>
    /// <param name="messageTimestamp">Twitchからのタイムスタンプ</param>
    /// <param name="requestBody">Twitchからのリクエストボディ</param>
    /// <param name="signature">Twitchからの署名</param>
    /// <returns></returns>
    bool VerifySignature(string messageId, string messageTimestamp, string requestBody, string signature);
}
