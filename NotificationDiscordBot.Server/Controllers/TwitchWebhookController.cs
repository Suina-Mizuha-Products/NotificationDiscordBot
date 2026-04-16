using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotificationDiscordBot.Server.Entity.Twitch;
using NotificationDiscordBot.Server.Interfaces.Services;
using NotificationDiscordBot.Server.Interfaces.Services.Twitch;
using System.Text.Json;

namespace NotificationDiscordBot.Server.Controllers;

/// <summary>
/// Twitch Webhook イベントの受信および検証を処理する API コントローラーです。Twitch EventSub からの HTTP POST リクエストを受け付け、署名検証やイベント種別ごとの応答を行います。Web
/// サービスが Twitch からの通知や検証リクエストを安全に受け取るためのエンドポイントを提供します。
/// </summary>
[Route("api/webhook/twitch")]
[ApiController]
public class TwitchWebhookController : ControllerBase
{
    private readonly ILogger<TwitchWebhookController> _logger;
    private readonly ITwitchVerifySignatureService _twitchVerifySignatureService;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public TwitchWebhookController(
        ILogger<TwitchWebhookController> logger,
        ITwitchVerifySignatureService twitchVerifySignatureService,
        IBackgroundTaskQueue backgroundTaskQueue,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _twitchVerifySignatureService = twitchVerifySignatureService ?? throw new ArgumentNullException(nameof(twitchVerifySignatureService));
        _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<IActionResult> ReceiveWebhookAsync(
        [FromHeader(Name = "Twitch-Eventsub-Message-Id")] string messageId,
        [FromHeader(Name = "Twitch-Eventsub-Message-Timestamp")] string messageTimestamp,
        [FromHeader(Name = "Twitch-Eventsub-Message-Type")] string messageType,
        [FromHeader(Name = "Twitch-Eventsub-Message-Signature")] string signature)
    {
        if (string.IsNullOrEmpty(messageId) ||
            string.IsNullOrEmpty(messageTimestamp) ||
            string.IsNullOrEmpty(messageType) ||
            string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("Twitchからのリクエストに必要なヘッダーが不足しています。");
            return BadRequest("必要なヘッダーが不足しています。");
        }

        // タイムスタンプの検証（5分以上前のリクエストは拒否）
        if (!DateTime.TryParse(messageTimestamp, out var timestamp))
        {
            _logger.LogWarning("Twitchからのリクエストのタイムスタンプが無効です。MessageTimestamp: {MessageTimestamp}",
                messageTimestamp);
            return BadRequest("タイムスタンプが無効です。");
        }
        var currentTime = DateTime.UtcNow;
        if (currentTime - timestamp > TimeSpan.FromMinutes(5))
        {
            _logger.LogWarning("Twitchからのリクエストのタイムスタンプが古すぎます。CurrentTime: {CurrentTime}, RequestTime: {RequestTime}",
                currentTime,
                timestamp);
            return BadRequest("タイムスタンプが古すぎます。");
        }

        // リクエストボディの読み取り
        Request.EnableBuffering();
        using var render = new StreamReader(Request.Body);
        var requestBody = await render.ReadToEndAsync();
        Request.Body.Position = 0;

        // 署名の検証
        var isValid = _twitchVerifySignatureService.VerifySignature(messageId, messageTimestamp, requestBody, signature);
        if (!isValid)
        {
            _logger.LogWarning("Twitchからのリクエストの署名が無効です。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}",
                messageId,
                messageTimestamp);
            return Unauthorized("署名が無効です。");
        }
        using var scope = _serviceScopeFactory.CreateAsyncScope();
        var twitchEventProcessingService = scope.ServiceProvider.GetRequiredService<ITwitchEventProcessingService>();

        if (messageType == WebhookMessageType.Verification)
        {
            var response = await twitchEventProcessingService.ProcessVerificationAsync(messageId, requestBody);
            if (response == null)
            {
                _logger.LogWarning("Twitchからの検証リクエストの処理に失敗しました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}",
                    messageId,
                    messageTimestamp);
                return BadRequest("検証リクエストの処理に失敗しました。");
            }

            return Content(response, "text/plain");
        }
        else if (messageType == WebhookMessageType.Notification)
        {
            // 通知の処理
            _logger.LogInformation("Twitchからの通知を受信しました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}",
                messageId,
                messageTimestamp);

            await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async token =>
            {
                try
                {
                    _logger.LogInformation("Twitchからの通知の処理を開始します。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}",
                        messageId,
                        messageTimestamp);

                    var success = await twitchEventProcessingService.ProcessNotificationAsync(messageId, requestBody, token);
                    if (!success)
                    {
                        _logger.LogWarning("Twitchからの通知の処理に失敗しました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}",
                            messageId,
                            messageTimestamp);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Twitchからの通知の処理がキャンセルされました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}",
                        messageId,
                        messageTimestamp);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Twitchからの通知の処理中にエラーが発生しました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}",
                        messageId,
                        messageTimestamp);
                }
            });

            return NoContent();
        }
        else if (messageType == WebhookMessageType.Revocation)
        {
            // 購読の取り消しの処理
            _logger.LogInformation("Twitchからの購読取り消しを受信しました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}",
                messageId,
                messageTimestamp);

            var success = await twitchEventProcessingService.ProcessRevocationAsync(messageId, requestBody);

            if (!success)
            {
                _logger.LogWarning("Twitchからの購読取り消しの処理に失敗しました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}",
                    messageId,
                    messageTimestamp);

                return BadRequest("購読取り消しの処理に失敗しました。");
            }

            return NoContent();
        }
        else
        {
            _logger.LogWarning("TwitchからのリクエストのMessageTypeが不明です。MessageType: {MessageType}",
                messageType);
            return BadRequest("不明なMessageTypeです。");
        }
    }
}
