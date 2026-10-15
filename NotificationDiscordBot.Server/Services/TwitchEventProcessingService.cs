using NotificationDiscordBot.Server.Entity.Twitch;
using NotificationDiscordBot.Server.Infrastructure.Serializer.Converters;
using NotificationDiscordBot.Server.Interfaces.Services;
using NotificationDiscordBot.Server.Interfaces.Services.Discord;
using NotificationDiscordBot.Server.Interfaces.Services.Twitch;
using System.Text.Json;

namespace NotificationDiscordBot.Server.Services;

public class TwitchEventProcessingService : ITwitchEventProcessingService
{
    private readonly ILogger<TwitchEventProcessingService> _logger;
    private readonly ITwitchApiInvocationService _twitchApiInvocationService;
    private readonly ISendNotificationService _notificationService;

    public TwitchEventProcessingService(
        ILogger<TwitchEventProcessingService> logger,
        ITwitchApiInvocationService twitchApiInvocationService,
        ISendNotificationService notificationService)
    {
        _logger = logger;
        _twitchApiInvocationService = twitchApiInvocationService;
        _notificationService = notificationService;
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
                _logger.LogInformation("配信開始イベントが受信されました。Broadcaster: {BroadcasterUserName}, StreamId: {StreamId}", streamOnlineEvent.BroadcasterUserName, streamOnlineEvent.Id);

                //認証情報の取得
                var credentials = await _twitchApiInvocationService.GetTwitchCredentialsAsync();
                if (credentials == null)
                {
                    _logger.LogError("Twitchの認証情報の取得に失敗しました。MessageId: {MessageId}", messageId);
                    return false;
                }

                // Twitch CLIでのテスト用にBroadcasterUserIdを固定値に設定
                // streamOnlineEvent.BroadcasterUserId = "BroadcasterUserId";

                // ストリーム情報の取得
                var streamData = await _twitchApiInvocationService.GetStreamsAsync(credentials.AccessToken, streamOnlineEvent.BroadcasterUserId);
                if(streamData == null)
                {
                    _logger.LogError("Twitchのストリーム情報の取得に失敗しました。");
                    return false;
                }

                var currentStreamData = streamData.Data.FirstOrDefault();
                if (currentStreamData == null) 
                {
                    _logger.LogError("Twitchのストリーム情報の取得に失敗しました。");
                    return false;
                }

                var userInfo = await _twitchApiInvocationService.GetUserInfoAsync(credentials.AccessToken, streamOnlineEvent.BroadcasterUserId);
                if (userInfo == null)
                {
                    _logger.LogError("Twitchのユーザー一覧情報の取得に失敗しました。");
                    return false;
                }

                var currentUser = userInfo.Data.FirstOrDefault();
                if(currentUser == null)
                {
                    _logger.LogWarning("該当の配信が見つかりませんでした。");
                    // デプロイ環境で問題があればリトライ処理を追加
                    return false;
                }

                var notificationResult = await _notificationService.SendTwitchOnlineNotificationMessageAsync(
                    currentStreamData.UserLogin,
                    currentStreamData.UserName,
                    currentUser.ProfileImageUrl,
                    currentStreamData.Title,
                    currentStreamData.GameName,
                    currentStreamData.StartedAt,
                    currentStreamData.ThumbnailUrl);

                if (!notificationResult)
                {
                    _logger.LogWarning("通知送信処理に失敗しました。");
                    return false;
                }

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

    public async Task<bool> ProcessRevocationAsync(string messageId, string requestBody)
    {
        try
        {
            var revocationData = JsonSerializer.Deserialize<WebhookCallbackRevocation>(requestBody);
            if (revocationData == null)
            {
                _logger.LogWarning("無効な失効データが受信されました。MessageId: {MessageId}", messageId);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "失効データの処理中にエラーが発生しました。MessageId: {MessageId}", messageId);
            return false;
        }
    }
}
