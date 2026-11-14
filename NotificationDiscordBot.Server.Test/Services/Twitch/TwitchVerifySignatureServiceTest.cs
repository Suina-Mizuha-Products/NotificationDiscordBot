using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationDiscordBot.Server.Services.Twitch;
using System.Security.Cryptography;
using System.Text;

namespace NotificationDiscordBot.Server.Test.Services.Twitch;

public class TwitchVerifySignatureServiceTest
{
    private readonly Mock<ILogger<TwitchVerifySignatureService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private const string TwitchClientSecret = "test_client_secret";

    public TwitchVerifySignatureServiceTest()
    {
        _loggerMock = new Mock<ILogger<TwitchVerifySignatureService>>();
        _configurationMock = new Mock<IConfiguration>();
    }

    private TwitchVerifySignatureService CreateService()
    {
        _configurationMock.Setup(c => c["Twitch-ClientSecret"]).Returns(TwitchClientSecret);
        return new TwitchVerifySignatureService(_loggerMock.Object, _configurationMock.Object);
    }

    private string ComputeSignature(string messageId, string messageTimestamp, string requestBody)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(TwitchClientSecret));
        var dataToHash = messageId + messageTimestamp + requestBody;
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
        return "sha256=" + Convert.ToHexStringLower(computedHash);
    }

    #region コンストラクタのテスト
    [Fact]
    public void Success_コンストラクタ_すべての依存関係が提供された場合インスタンスが作成される()
    {
        // Arrange
        _configurationMock.Setup(c => c["Twitch-ClientSecret"]).Returns(TwitchClientSecret);
        // Act
        var service = new TwitchVerifySignatureService(_loggerMock.Object, _configurationMock.Object);
        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Failure_コンストラクタ_ILoggerがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Arrange
        _configurationMock.Setup(c => c["Twitch-ClientSecret"]).Returns(TwitchClientSecret);
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchVerifySignatureService(null, _configurationMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_IConfigurationがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Arrange
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchVerifySignatureService(_loggerMock.Object, null));
    }

    #endregion

    #region VerifySignatureのテスト
    [Fact]
    public void Success_VerifySignature_正しい署名が提供された場合trueを返す()
    {
        // Arrange
        var service = CreateService();
        var messageId = "test_message_id";
        var messageTimestamp = "2026-10-01T12:00:00Z";
        var requestBody = "{\"type\":\"notification\"}";
        var signature = ComputeSignature(messageId, messageTimestamp, requestBody);
        // Act
        var result = service.VerifySignature(messageId, messageTimestamp, requestBody, signature);
        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Failure_VerifySignature_不正な署名が提供された場合falseを返す()
    {
        // Arrange
        var service = CreateService();
        var messageId = "test_message_id";
        var messageTimestamp = "2026-10-01T12:00:00Z";
        var requestBody = "{\"type\":\"notification\"}";
        var signature = ComputeSignature(messageId, messageTimestamp, requestBody);
        signature = signature[..^1] + (signature[^1] == '0' ? '1' : '0'); // 署名を不正に変更

        // Act
        var result = service.VerifySignature(messageId, messageTimestamp, requestBody, signature);
        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(null, "2026-10-01T12:00:00Z", "{\"type\":\"notification\"}", "sha256=some_signature")]
    [InlineData("test_message_id", null, "{\"type\":\"notification\"}", "sha256=some_signature")]
    [InlineData("test_message_id", "2026-10-01T12:00:00Z", null, "sha256=some_signature")]
    [InlineData("test_message_id", "2026-10-01T12:00:00Z", "{\"type\":\"notification\"}", null)]
    [InlineData("", "2026-10-01T12:00:00Z", "{\"type\":\"notification\"}", "sha256=some_signature")]
    [InlineData("test_message_id", "", "{\"type\":\"notification\"}", "sha256=some_signature")]
    [InlineData("test_message_id", "2026-10-01T12:00:00Z", "", "sha256=some_signature")]
    [InlineData("test_message_id", "2026-10-01T12:00:00Z", "{\"type\":\"notification\"}", "")]
    public void Failure_VerifySignature_必要なパラメータが不足している場合falseを返す(string messageId, string messageTimestamp, string requestBody, string signature)
    {
        // Arrange
        var service = CreateService();
        if (!string.IsNullOrEmpty(signature))
        {
            signature = ComputeSignature(messageId, messageTimestamp, requestBody);
        }
        // Act
        var result = service.VerifySignature(messageId, messageTimestamp, requestBody, signature);
        // Assert
        Assert.False(result);
    }
    #endregion
}
