using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationDiscordBot.Server.Entity.Twitch;
using NotificationDiscordBot.Server.Services.Twitch;

namespace NotificationDiscordBot.Server.Test.Services.Twitch;

public class TwitchApiInvocationServiceTest
{
    private readonly Mock<ILogger<TwitchApiInvocationService>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private const string testClientId = "test_client_id";
    private const string testClientSecret = "test_client_secret";

    public TwitchApiInvocationServiceTest()
    {
        _loggerMock = new Mock<ILogger<TwitchApiInvocationService>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
    }

    private void SetupConfigurationMock()
    {
        _configurationMock.Setup(c => c["Twitch:ClientId"]).Returns(testClientId);
        _configurationMock.Setup(c => c["Twitch:ClientSecret"]).Returns(testClientSecret);
    }

    #region コンストラクタのテスト

    [Fact]
    public void Success_コンストラクタ_すべての依存関係が提供された場合インスタンスが作成される()
    {
        // Arrange
        _configurationMock.Setup(c => c["Twitch:ClientId"]).Returns(testClientId);
        _configurationMock.Setup(c => c["Twitch:ClientSecret"]).Returns(testClientSecret);

        // Act
        var service = new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Failure_コンストラクタ_Loggerがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Arrange
        _configurationMock.Setup(c => c["Twitch:ClientId"]).Returns(testClientId);
        _configurationMock.Setup(c => c["Twitch:ClientSecret"]).Returns(testClientSecret);
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchApiInvocationService(null, _configurationMock.Object, _httpClientFactoryMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_Configurationがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchApiInvocationService(_loggerMock.Object, null, _httpClientFactoryMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_HttpClientFactoryがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Arrange
        _configurationMock.Setup(c => c["Twitch:ClientId"]).Returns(testClientId);
        _configurationMock.Setup(c => c["Twitch:ClientSecret"]).Returns(testClientSecret);
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, null));
    }

    [Fact]
    public void Failure_コンストラクタ_ClientIdが設定されていない場合ArgumentNullExceptionがスローされる()
    {
        // Arrange
        _configurationMock.Setup(c => c["Twitch:ClientId"]).Returns((string)null);
        _configurationMock.Setup(c => c["Twitch:ClientSecret"]).Returns(testClientSecret);
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_ClientSecretが設定されていない場合ArgumentNullExceptionがスローされる()
    {
        // Arrange
        _configurationMock.Setup(c => c["Twitch:ClientId"]).Returns(testClientId);
        _configurationMock.Setup(c => c["Twitch:ClientSecret"]).Returns((string)null);
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object));
    }

    #endregion

    #region GetTwitchCredentialsAsyncのテスト

    #endregion
}
