using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotificationDiscordBot.Server.Entity.Twitch;
using NotificationDiscordBot.Server.Interfaces.Services.Twitch;
using System.Text.Json;

namespace NotificationDiscordBot.Server.Controllers
{
    [Route("api/webhook/twitch")]
    [ApiController]
    public class TwitchWebhookController : ControllerBase
    {
        private readonly ILogger<TwitchWebhookController> _logger;
        private readonly ITwitchVerifySignatureService _twitchVerifySignatureService;

        public TwitchWebhookController(
            ILogger<TwitchWebhookController> logger,
            ITwitchVerifySignatureService twitchVerifySignatureService)
        {
            _logger = logger;
            _twitchVerifySignatureService = twitchVerifySignatureService;
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

            if (messageType == WebhookMessageType.Verification)
            {
                try
                {
                    var verificationData = JsonSerializer.Deserialize<WebhookCallbackVerification>(requestBody);
                    if(verificationData is null || string.IsNullOrEmpty(verificationData.Challenge))
                    {
                        _logger.LogWarning("Twitchからの検証リクエストのChallengeが無効です。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}",
                            messageId,
                            messageTimestamp);
                        return BadRequest("Challengeが無効です。");
                    }

                    return Content(verificationData.Challenge, "text/plain");
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Twitchからの検証リクエストの処理中にエラーが発生しました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}",
                        messageId,
                        messageTimestamp);
                    return BadRequest("検証リクエストの処理中にエラーが発生しました。");
                }
            }
            else if (messageType == WebhookMessageType.Notification)
            {
                // 通知の処理
                _logger.LogInformation("Twitchからの通知を受信しました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}",
                    messageId,
                    messageTimestamp);

                // TODO: 一旦レスポンスを返してから、非同期で通知の処理を行う
                return Ok();
            }
            else if (messageType == WebhookMessageType.Revocation)
            {
                // 購読の取り消しの処理
                _logger.LogInformation("Twitchからの購読取り消しを受信しました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}",
                    messageId,
                    messageTimestamp);

                // TODO: 一旦レスポンスを返してから、非同期で購読取り消しの処理を行う
                return Ok();
            }
            else
            {
                _logger.LogWarning("TwitchからのリクエストのMessageTypeが不明です。MessageType: {MessageType}",
                    messageType);
                return BadRequest("不明なMessageTypeです。");
            }
        }
    }
}
