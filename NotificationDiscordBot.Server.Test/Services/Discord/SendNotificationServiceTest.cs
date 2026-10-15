using Microsoft.Extensions.Logging;
using Moq;
using NotificationDiscordBot.Server.Services.Discord;
using Microsoft.Extensions.Configuration;
using Discord.WebSocket;
using Discord;
using Discord.Rest;

namespace NotificationDiscordBot.Server.Test.Services.Discord;

public class SendNotificationServiceTest
{
    private readonly Mock<ILogger<SendNotificationService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<DiscordSocketClient> _discordClientMock;
    private readonly Mock<SocketTextChannel> _socketTextChannelMock;

    private const string channelId = "123456";
    private const string userLogin = "test_user";
    private const string userName = "Test User";
    private const string profileImageUrl = "https://example.com/profile.jpg";
    private const string title = "Test Stream Title";
    private const string gameName = "Test Game";
    private const string startedAt = "2024-06-01T12:00:00Z";
    private const string thumnailUrl = "https://static-cdn.jtvnw.net/previews-ttv/live_user_test_user-{width}x{height}.jpg";

    public SendNotificationServiceTest()
    {
        _loggerMock = new Mock<ILogger<SendNotificationService>>();
        _configurationMock = new Mock<IConfiguration>();
        _discordClientMock = new Mock<DiscordSocketClient>();
        _socketTextChannelMock = new Mock<SocketTextChannel>();
    }

    #region コンストラクタのテスト
    [Fact]
    public void Success_コンストラクタ_全ての依存関係が提供された場合インスタンスが作成される()
    {
        // Arrange
        _configurationMock.Setup(c => c["Discord:ChannelId"]).Returns(channelId);
        // Act
        var service = new SendNotificationService(_loggerMock.Object, _configurationMock.Object, _discordClientMock.Object);
        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Failure_コンストラクタ_Loggerがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Arrange
        _configurationMock.Setup(c => c["Discord:ChannelId"]).Returns(channelId);
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SendNotificationService(null, _configurationMock.Object, _discordClientMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_Configurationがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SendNotificationService(_loggerMock.Object, null, _discordClientMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_DiscordClientがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Arrange
        _configurationMock.Setup(c => c["Discord:ChannelId"]).Returns(channelId);
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SendNotificationService(_loggerMock.Object, _configurationMock.Object, null));
    }

    [Fact]
    public void Failure_コンストラクタ_ChannelIdが設定されていない場合InvalidOperationExceptionがスローされる()
    {
        // Arrange
        _configurationMock.Setup(c => c["Discord:ChannelId"]).Returns((string)null);
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new SendNotificationService(_loggerMock.Object, _configurationMock.Object, _discordClientMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_ChannelIdが無効な場合InvalidOperationExceptionがスローされる()
    {
        // Arrange
        _configurationMock.Setup(c => c["Discord:ChannelId"]).Returns("invalid");
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new SendNotificationService(_loggerMock.Object, _configurationMock.Object, _discordClientMock.Object));
    }

    #endregion

    #region SendTwitchOnlineNotificationMessageAsyncのテスト

    #endregion
}
