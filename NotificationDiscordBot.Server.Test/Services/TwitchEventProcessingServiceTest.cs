using Microsoft.Extensions.Logging;
using Moq;
using NotificationDiscordBot.Server.Entity.Twitch;
using NotificationDiscordBot.Server.Interfaces.Services.Discord;
using NotificationDiscordBot.Server.Interfaces.Services.Twitch;
using NotificationDiscordBot.Server.Services;

namespace NotificationDiscordBot.Server.Test.Services;

public class TwitchEventProcessingServiceTest
{
    private readonly Mock<ILogger<TwitchEventProcessingService>> _loggerMock;
    private readonly Mock<ITwitchApiInvocationService> _twitchApiInvocationServiceMock;
    private readonly Mock<ISendNotificationService> _notificationServiceMock;

    public TwitchEventProcessingServiceTest()
    {
        _loggerMock = new Mock<ILogger<TwitchEventProcessingService>>();
        _twitchApiInvocationServiceMock = new Mock<ITwitchApiInvocationService>();
        _notificationServiceMock = new Mock<ISendNotificationService>();
    }

    #region コンストラクタのテスト

    [Fact]
    public void Success_コンストラクタ_全ての依存関係が提供された場合インスタンスが作成される()
    {
        // Act
        var service = new TwitchEventProcessingService(_loggerMock.Object, _twitchApiInvocationServiceMock.Object, _notificationServiceMock.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Failure_コンストラクタ_Loggerがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchEventProcessingService(null, _twitchApiInvocationServiceMock.Object, _notificationServiceMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_TwitchApiInvocationServiceがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchEventProcessingService(_loggerMock.Object, null, _notificationServiceMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_SendNotificationServiceがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchEventProcessingService(_loggerMock.Object, _twitchApiInvocationServiceMock.Object, null));
    }

    #endregion

    #region ProcessNotificationAsyncのテスト

    [Fact]
    public async Task Success_ProcessNotificationAsync_配信開始イベントを正常に処理できる()
    {
        // Arrange
        var service = CreateService();
        var requestBody = CreateStreamOnlineNotificationJson();
        SetupSuccessfulDependencies();

        // Act
        var result = await service.ProcessNotificationAsync("message-id", requestBody, CancellationToken.None);

        // Assert
        Assert.True(result);
        _notificationServiceMock.Verify(n => n.SendTwitchOnlineNotificationMessageAsync(
            "test_login",
            "test_name",
            "https://example.com/profile.jpg",
            "test_title",
            "test_game",
            "2026-04-16T21:44:36.3252297Z",
            "https://example.com/thumb-{width}x{height}.jpg"), Times.Once);
    }

    [Fact]
    public async Task Failure_ProcessNotificationAsync_RequestBodyが空の場合falseが返される()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ProcessNotificationAsync("message-id", string.Empty, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Failure_ProcessNotificationAsync_キャンセルされた場合OperationCanceledExceptionがスローされる()
    {
        // Arrange
        var service = CreateService();
        var requestBody = CreateStreamOnlineNotificationJson();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.ProcessNotificationAsync("message-id", requestBody, cts.Token));
    }

    [Fact]
    public async Task Failure_ProcessNotificationAsync_認証情報取得に失敗した場合falseが返される()
    {
        // Arrange
        var service = CreateService();
        var requestBody = CreateStreamOnlineNotificationJson();
        _twitchApiInvocationServiceMock.Setup(s => s.GetTwitchCredentialsAsync()).ReturnsAsync((GetTwitchCredentialsResponse?)null);

        // Act
        var result = await service.ProcessNotificationAsync("message-id", requestBody, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Failure_ProcessNotificationAsync_ストリーム情報配列がnullの場合falseが返される()
    {
        // Arrange
        var service = CreateService();
        var requestBody = CreateStreamOnlineNotificationJson();
        _twitchApiInvocationServiceMock.Setup(s => s.GetTwitchCredentialsAsync()).ReturnsAsync(new GetTwitchCredentialsResponse { AccessToken = "token" });
        _twitchApiInvocationServiceMock.Setup(s => s.GetStreamsAsync("token", "91724331")).ReturnsAsync(new GetStreamsResponse
        {
            Data = null!
        });

        // Act
        var result = await service.ProcessNotificationAsync("message-id", requestBody, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Failure_ProcessNotificationAsync_ユーザー情報配列がnullの場合falseが返される()
    {
        // Arrange
        var service = CreateService();
        var requestBody = CreateStreamOnlineNotificationJson();
        _twitchApiInvocationServiceMock.Setup(s => s.GetTwitchCredentialsAsync()).ReturnsAsync(new GetTwitchCredentialsResponse { AccessToken = "token" });
        _twitchApiInvocationServiceMock.Setup(s => s.GetStreamsAsync("token", "91724331")).ReturnsAsync(new GetStreamsResponse
        {
            Data =
            [
                new StreamData
                {
                    UserLogin = "test_login",
                    UserName = "test_name",
                    Title = "test_title",
                    GameName = "test_game",
                    StartedAt = "2026-04-16T21:44:36.3252297Z",
                    ThumbnailUrl = "https://example.com/thumb-{width}x{height}.jpg"
                }
            ]
        });
        _twitchApiInvocationServiceMock.Setup(s => s.GetUserInfoAsync("token", "91724331")).ReturnsAsync(new GetUserResponse
        {
            Data = null!
        });

        // Act
        var result = await service.ProcessNotificationAsync("message-id", requestBody, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Failure_ProcessNotificationAsync_通知送信に失敗した場合falseが返される()
    {
        // Arrange
        var service = CreateService();
        var requestBody = CreateStreamOnlineNotificationJson();
        SetupSuccessfulDependencies(notificationResult: false);

        // Act
        var result = await service.ProcessNotificationAsync("message-id", requestBody, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Failure_ProcessNotificationAsync_未処理イベントの場合falseが返される()
    {
        // Arrange
        var service = CreateService();
        var requestBody = CreateUnknownNotificationJson();

        // Act
        var result = await service.ProcessNotificationAsync("message-id", requestBody, CancellationToken.None);

        // Assert
        Assert.False(result);
        _twitchApiInvocationServiceMock.Verify(s => s.GetTwitchCredentialsAsync(), Times.Never);
    }

    [Fact]
    public async Task Failure_ProcessNotificationAsync_不正なJsonの場合falseが返される()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ProcessNotificationAsync("message-id", "{ invalid-json", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ProcessVerificationAsyncのテスト

    [Fact]
    public async Task Success_ProcessVerificationAsync_Challengeが返される()
    {
        // Arrange
        var service = CreateService();
        var requestBody = """
            {
              "subscription": { "id": "sub-id" },
              "challenge": "test-challenge"
            }
            """;

        // Act
        var result = await service.ProcessVerificationAsync("message-id", requestBody);

        // Assert
        Assert.Equal("test-challenge", result);
    }

    [Fact]
    public async Task Failure_ProcessVerificationAsync_Challengeが空の場合nullが返される()
    {
        // Arrange
        var service = CreateService();
        var requestBody = """
            {
              "subscription": { "id": "sub-id" },
              "challenge": ""
            }
            """;

        // Act
        var result = await service.ProcessVerificationAsync("message-id", requestBody);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Failure_ProcessVerificationAsync_不正なJsonの場合nullが返される()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ProcessVerificationAsync("message-id", "{ invalid-json");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region ProcessRevocationAsyncのテスト

    [Fact]
    public async Task Success_ProcessRevocationAsync_有効なデータの場合trueが返される()
    {
        // Arrange
        var service = CreateService();
        var requestBody = """
            {
              "subscription": { "id": "sub-id" }
            }
            """;

        // Act
        var result = await service.ProcessRevocationAsync("message-id", requestBody);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task Failure_ProcessRevocationAsync_不正なJsonの場合falseが返される()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ProcessRevocationAsync("message-id", "{ invalid-json");

        // Assert
        Assert.False(result);
    }

    #endregion

    private TwitchEventProcessingService CreateService()
    {
        return new TwitchEventProcessingService(_loggerMock.Object, _twitchApiInvocationServiceMock.Object, _notificationServiceMock.Object);
    }

    private void SetupSuccessfulDependencies(bool notificationResult = true)
    {
        _twitchApiInvocationServiceMock.Setup(s => s.GetTwitchCredentialsAsync()).ReturnsAsync(new GetTwitchCredentialsResponse
        {
            AccessToken = "token"
        });

        _twitchApiInvocationServiceMock.Setup(s => s.GetStreamsAsync("token", "91724331")).ReturnsAsync(new GetStreamsResponse
        {
            Data =
            [
                new StreamData
                {
                    UserLogin = "test_login",
                    UserName = "test_name",
                    Title = "test_title",
                    GameName = "test_game",
                    StartedAt = "2026-04-16T21:44:36.3252297Z",
                    ThumbnailUrl = "https://example.com/thumb-{width}x{height}.jpg"
                }
            ]
        });

        _twitchApiInvocationServiceMock.Setup(s => s.GetUserInfoAsync("token", "91724331")).ReturnsAsync(new GetUserResponse
        {
            Data =
            [
                new UserInfo
                {
                    ProfileImageUrl = "https://example.com/profile.jpg"
                }
            ]
        });

        _notificationServiceMock.Setup(n => n.SendTwitchOnlineNotificationMessageAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>())).ReturnsAsync(notificationResult);
    }

    private static string CreateStreamOnlineNotificationJson()
    {
        return """
            {
              "subscription": {
                "id": "test-subscription-id",
                "status": "enabled",
                "type": "stream.online",
                "version": "1",
                "condition": { "broadcaster_user_id": "91724331" },
                "transport": { "method": "webhook", "callback": "null" },
                "created_at": "2026-04-16T21:44:36.3252297Z",
                "cost": 0
              },
              "event": {
                "id": "49461215",
                "broadcaster_user_id": "91724331",
                "broadcaster_user_login": "testBroadcaster",
                "broadcaster_user_name": "testBroadcaster",
                "type": "live",
                "started_at": "2026-04-16T21:44:36.3252297Z"
              }
            }
            """;
    }

    private static string CreateUnknownNotificationJson()
    {
        return """
            {
              "subscription": {
                "id": "test-subscription-id",
                "status": "enabled",
                "type": "channel.follow",
                "version": "1",
                "condition": { "broadcaster_user_id": "91724331" },
                "transport": { "method": "webhook", "callback": "null" },
                "created_at": "2026-04-16T21:44:36.3252297Z",
                "cost": 0
              },
              "event": {
                "user_id": "19117137"
              }
            }
            """;
    }
}
