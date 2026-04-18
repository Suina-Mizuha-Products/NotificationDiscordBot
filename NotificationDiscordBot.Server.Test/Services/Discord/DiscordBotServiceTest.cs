using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationDiscordBot.Server.Services.Discord;
using NotificationDiscordBot.Server.Test.Mock;
using IDiscordClient = NotificationDiscordBot.Server.Interfaces.Services.Discord.IDiscordClient;

namespace NotificationDiscordBot.Server.Test.Services.Discord;

public class DiscordBotServiceTest
{
    private readonly Mock<ILogger<DiscordBotService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IDiscordClient> _discordClientMock;

    public DiscordBotServiceTest()
    {
        _loggerMock = new Mock<ILogger<DiscordBotService>>();
        _configurationMock = new Mock<IConfiguration>();
        _discordClientMock = new Mock<IDiscordClient>();
    }

    #region コンストラクタのテスト

    [Fact]
    public void Success_コンストラクタ_全ての依存関係が提供された場合インスタンスが作成される()
    {
        // Arrange
        _configurationMock.Setup(c => c["Discord:Token"]).Returns("dummy token");
        var logSubscribed = false;
        var readySubscribed = false;
        var connectedSubscribed = false;
        var disconectedSubscribed = false;

        _discordClientMock.SetupAdd(c => c.Log += It.IsAny<Func<LogMessage, Task>>())
            .Callback(() => logSubscribed = true);
        _discordClientMock.SetupAdd(c => c.Ready += It.IsAny<Func<Task>>())
            .Callback(() => readySubscribed = true);
        _discordClientMock.SetupAdd(c => c.Connected += It.IsAny<Func<Task>>())
            .Callback(() => connectedSubscribed = true);
        _discordClientMock.SetupAdd(c => c.Disconnected += It.IsAny<Func<Exception, Task>>())
            .Callback(() => disconectedSubscribed = true);

        // Act
        var service = new DiscordBotService(
            _loggerMock.Object,
            _configurationMock.Object,
            _discordClientMock.Object);

        // Assert
        Assert.NotNull(service);

        Assert.True(logSubscribed);
        Assert.True(readySubscribed);
        Assert.True(connectedSubscribed);
        Assert.True(disconectedSubscribed);
    }

    [Fact]
    public void Failure_コンストラクタ_ILoggerがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Arrange
        _configurationMock.Setup(c => c["Discord:Token"]).Returns("dummy token");
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DiscordBotService(
            null,
            _configurationMock.Object,
            _discordClientMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_IConfigurationがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Arrange
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DiscordBotService(
            _loggerMock.Object,
            null,
            _discordClientMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_DiscordSocketClientがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Arrange
        _configurationMock.Setup(c => c["Discord:Token"]).Returns("dummy token");
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DiscordBotService(
            _loggerMock.Object,
            _configurationMock.Object,
            null));
    }

    [Fact]
    public void Failure_コンストラクタ_DiscordのTokenが構成されていない場合InvalidOperationExceptionがスローされる()
    {
        // Arrange
        _configurationMock.Setup(c => c["Discord:Token"]).Returns((string)null);
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new DiscordBotService(
            _loggerMock.Object,
            _configurationMock.Object,
            _discordClientMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_DiscordのTokenが空文字の場合InvalidOperationExceptionがスローされる()
    {
        // Arrange
        _configurationMock.Setup(c => c["Discord:Token"]).Returns(string.Empty);
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new DiscordBotService(
            _loggerMock.Object,
            _configurationMock.Object,
            _discordClientMock.Object));
    }

    #endregion

    #region ExecuteAsyncのテスト

    [Fact]
    public async Task Success_ExecuteAsync_正常にログインと接続が行われる()
    {
        // Arrange
        _configurationMock.Setup(c => c["Discord:Token"]).Returns("dummy token");
        
        var logInCalled = false;
        var startCalled = false;

        _discordClientMock.Setup(c => c.LoginAsync(TokenType.Bot, It.IsAny<string>()))
            .Callback(() => logInCalled = true)
            .Returns(Task.CompletedTask);

        _discordClientMock.Setup(c => c.StartAsync())
            .Callback(() => startCalled = true)
            .Returns(Task.CompletedTask);

        var service = new DiscordBotService(
            _loggerMock.Object,
            _configurationMock.Object,
            _discordClientMock.Object);
        // Act
        var executeTask = service.StartAsync(CancellationToken.None);
        
        // ExecuteAsyncが実行されるまで待機
        await Task.Delay(100, TestContext.Current.CancellationToken);
        
        // Assert
        Assert.True(executeTask.IsCompleted);
        Assert.True(logInCalled);
        Assert.True(startCalled);

        // Cleanup
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Failure_ExecuteAsync_LoginAsyncが例外をスローした場合例外が伝播する()
    {
        // Arrange
        _configurationMock.Setup(c => c["Discord:Token"]).Returns("dummy token");
        _discordClientMock.Setup(c => c.LoginAsync(TokenType.Bot, It.IsAny<string>()))
            .ThrowsAsync(new Exception("Login failed"));

        var service = new DiscordBotService(
            _loggerMock.Object,
            _configurationMock.Object,
            _discordClientMock.Object);

        // Act & Assert
        await service.StartAsync(CancellationToken.None);
        await Assert.ThrowsAsync<Exception>(async () => await service.ExecuteTask);
    }

    [Fact]
    public async Task Failure_ExecuteAsync_StartAsyncが例外をスローした場合例外が伝播する()
    {
        // Arrange
        _configurationMock.Setup(c => c["Discord:Token"]).Returns("dummy token");
        _discordClientMock.Setup(c => c.StartAsync())
            .ThrowsAsync(new Exception("Start failed"));
        
        var service = new DiscordBotService(
            _loggerMock.Object,
            _configurationMock.Object,
            _discordClientMock.Object);

        // Act & Assert
        await service.StartAsync(CancellationToken.None);
        await Assert.ThrowsAsync<Exception>(async () => await service.ExecuteTask);
    }

    #endregion

    #region Disposeのテスト

    [Fact]
    public void Success_Dispose_イベントが解除される()
    {
        // Arrange
        _configurationMock.Setup(c => c["Discord:Token"]).Returns("dummy token");
        var logUnsubscribed = false;
        var readyUnsubscribed = false;
        var connectedUnsubscribed = false;
        var disconectedUnsubscribed = false;
        _discordClientMock.SetupRemove(c => c.Log -= It.IsAny<Func<LogMessage, Task>>())
            .Callback(() => logUnsubscribed = true);
        _discordClientMock.SetupRemove(c => c.Ready -= It.IsAny<Func<Task>>())
            .Callback(() => readyUnsubscribed = true);
        _discordClientMock.SetupRemove(c => c.Connected -= It.IsAny<Func<Task>>())
            .Callback(() => connectedUnsubscribed = true);
        _discordClientMock.SetupRemove(c => c.Disconnected -= It.IsAny<Func<Exception, Task>>())
            .Callback(() => disconectedUnsubscribed = true);
        var service = new DiscordBotService(
            _loggerMock.Object,
            _configurationMock.Object,
            _discordClientMock.Object);
        // Act
        service.Dispose();
        // Assert
        Assert.True(logUnsubscribed);
        Assert.True(readyUnsubscribed);
        Assert.True(connectedUnsubscribed);
        Assert.True(disconectedUnsubscribed);
    }

    #endregion
}
