using Microsoft.Extensions.Logging;
using Moq;
using NotificationDiscordBot.Server.Services.Discord;
using Microsoft.Extensions.Configuration;
using Discord.WebSocket;
using Discord;
using IDiscordClient = NotificationDiscordBot.Server.Interfaces.Services.Discord.IDiscordClient;
using NotificationDiscordBot.Server.Interfaces.Services.Discord;
using Discord.Rest;

namespace NotificationDiscordBot.Server.Test.Services.Discord;

public class SendNotificationServiceTest
{
    private readonly Mock<ILogger<SendNotificationService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IDiscordClient> _discordClientMock;
    private readonly Mock<ISocketTextChannel> _socketTextChannelMock;

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
        _discordClientMock = new Mock<IDiscordClient>();
        _socketTextChannelMock = new Mock<ISocketTextChannel>();
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

    [Fact]
    public async Task Success_SendTwitchOnlineNotificationMessageAsync_メッセージが正常に送信される()
    {
        // Arrange
        _configurationMock.Setup(c => c["Discord:ChannelId"]).Returns(channelId);
        var service = new SendNotificationService(_loggerMock.Object, _configurationMock.Object, _discordClientMock.Object);
        _discordClientMock.Setup(c => c.GetTextChannel(It.IsAny<ulong>())).Returns(_socketTextChannelMock.Object);
        _socketTextChannelMock.Setup(c => c.SendMessageAsync(It.IsAny<string>(), false, It.IsAny<Embed>(), null, null, null, null, null, null, MessageFlags.None, null)).Returns(Task.FromResult((RestUserMessage)null));

        // Act
        var result = await service.SendTwitchOnlineNotificationMessageAsync(userLogin, userName, profileImageUrl, title, gameName, startedAt, thumnailUrl);

        // Assert
        Assert.True(result);
        _discordClientMock.Verify(c => c.GetTextChannel(It.IsAny<ulong>()), Times.Once);
        _socketTextChannelMock.Verify(c => c.SendMessageAsync(It.IsAny<string>(), false, It.IsAny<Embed>(), null, null, null, null, null, null, MessageFlags.None, null), Times.Once);
    }

    [Fact]
    public async Task Failure_SendTwitchOnlineNotificationMessageAsync_チャンネルの取得に失敗した場合falseが返される()
    {
        // Arrange
        _configurationMock.Setup(c => c["Discord:ChannelId"]).Returns(channelId);
        var service = new SendNotificationService(_loggerMock.Object, _configurationMock.Object, _discordClientMock.Object);
        _discordClientMock.Setup(c => c.GetTextChannel(It.IsAny<ulong>())).Returns((ISocketTextChannel)null);

        // Act
        var result = await service.SendTwitchOnlineNotificationMessageAsync(userLogin, userName, profileImageUrl, title, gameName, startedAt, thumnailUrl);
        
        // Assert
        Assert.False(result);
        _discordClientMock.Verify(c => c.GetTextChannel(It.IsAny<ulong>()), Times.Once);
    }

    #endregion
}
