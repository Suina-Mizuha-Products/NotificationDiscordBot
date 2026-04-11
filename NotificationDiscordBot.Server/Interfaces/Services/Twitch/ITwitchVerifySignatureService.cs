namespace NotificationDiscordBot.Server.Interfaces.Services.Twitch;

/// <summary>
/// Twitch から送信されたリクエストの署名を検証するサービスを定義します。リクエストが改ざんされていないことを確認するために使用します。
/// </summary>
/// <remarks>このインターフェイスは、Twitch Webhook や EventSub
/// などのセキュリティ検証が必要なシナリオで利用されます。署名の検証に失敗した場合、リクエストは信頼できないものとして扱う必要があります。</remarks>
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
