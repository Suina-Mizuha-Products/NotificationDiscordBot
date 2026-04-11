using NotificationDiscordBot.Server.Entity.Twitch;
using NotificationDiscordBot.Server.Infrastructure.Serializer.Converters;
using NotificationDiscordBot.Server.Interfaces.Services;
using System.Text.Json;

namespace NotificationDiscordBot.Server.Services;

public class TwitchEventProcessingService : ITwitchEventProcessingService
{
    private readonly ILogger<TwitchEventProcessingService> _logger;

    public TwitchEventProcessingService(ILogger<TwitchEventProcessingService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ProcessNotificationAsync(string messageId, string requestBody, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Twitchイベント通知の処理を開始します。MessageId: {MessageId}", messageId);
        try
        {
            _logger.LogInformation("受信したイベントデータ: {RequestBody}", requestBody);
            var serializer = new WebhookCallbackNotificationJsonConverter();
            var options = new JsonSerializerOptions
            {
                Converters = { serializer },
                PropertyNameCaseInsensitive = true
            };

            var eventData = JsonSerializer.Deserialize<WebhookCallbackNotification>(requestBody, options);

            if (eventData == null)
            {
                _logger.LogWarning("無効なイベントデータが受信されました。MessageId: {MessageId}", messageId);
                return false;
            }

            if(eventData.Event == null)
            {
                _logger.LogWarning("イベントデータにイベント情報が含まれていません。MessageId: {MessageId}", messageId);
                return false;
            }

            if(eventData.Event is StreamOnlineEvent streamOnlineEvent)
            {
                // TODO: 配信開始イベントの処理を実装
                _logger.LogInformation("配信開始イベントが受信されました。Broadcaster: {BroadcasterUserName}, StreamId: {StreamId}", streamOnlineEvent.BroadcasterUserName, streamOnlineEvent.Id);
                return true;
            }

            _logger.LogInformation("処理されていないイベントタイプが受信されました。MessageId: {MessageId}", messageId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "イベント通知の処理中にエラーが発生しました。MessageId: {MessageId}", messageId);
            return false;
        }
    }

    public async Task<string?> ProcessVerificationAsync(string messageId, string requestBody)
    {
        try
        {
            var verificationData = JsonSerializer.Deserialize<WebhookCallbackVerification>(requestBody);
            if (verificationData == null || string.IsNullOrEmpty(verificationData.Challenge))
            {
                _logger.LogWarning("無効な検証データが受信されました。MessageId: {MessageId}", messageId);
                return null;
            }

            return verificationData.Challenge;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "検証データの処理中にエラーが発生しました。MessageId: {MessageId}", messageId);
            return null;
        }
    }

    public Task<bool> ProcessRevocationAsync(string messageId, string requestBody)
    {
        throw new NotImplementedException();
    }
}
