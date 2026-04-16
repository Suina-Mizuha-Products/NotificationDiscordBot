using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationDiscordBot.Server.Controllers;
using NotificationDiscordBot.Server.Entity.Twitch;
using NotificationDiscordBot.Server.Interfaces.Services;
using NotificationDiscordBot.Server.Interfaces.Services.Twitch;
using NotificationDiscordBot.Server.Test.Mock;
using System.Text;

namespace NotificationDiscordBot.Server.Test;

public class TwitchWebhookControllerTest
{
    private readonly Mock<ILogger<TwitchWebhookController>> _loggerMock;
    private readonly Mock<ITwitchVerifySignatureService> _twitchVerifySignatureServiceMock;
    private readonly IBackgroundTaskQueue _backgroundTaskQueueMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<ITwitchEventProcessingService> _eventProcessingServiceMock;

    public TwitchWebhookControllerTest()
    {
        _loggerMock = new Mock<ILogger<TwitchWebhookController>>();
        _twitchVerifySignatureServiceMock = new Mock<ITwitchVerifySignatureService>();
        _backgroundTaskQueueMock = new MockBackgroundQueueTask();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _eventProcessingServiceMock = new Mock<ITwitchEventProcessingService>();
        
        // IServiceScopeFactoryのモック設定
        var serviceScopeMock = new Mock<IServiceScope>();
        serviceScopeMock.Setup(s => s.ServiceProvider).Returns(new ServiceCollection()
            .AddSingleton(_eventProcessingServiceMock.Object)
            .BuildServiceProvider());
        // CreateScope を返すように設定（CreateAsyncScope は内部で CreateScope を使用するためこれで対応）
        _serviceScopeFactoryMock.Setup(f => f.CreateScope()).Returns(serviceScopeMock.Object);
    }

    #region コンストラクタのテスト

    [Fact]
    public void Success_コンストラクタ_全ての依存関係が提供されている場合インスタンスが作成される()
    {
        // Arrange & Act
        var controller = new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object);
        // Assert
        Assert.NotNull(controller);
    }

    [Fact]
    public void Failure_コンストラクタ_ILoggerがnullの場合例外がスローされる()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchWebhookController(
            null,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_ITwitchVerifySignatureServiceがnullの場合例外がスローされる()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchWebhookController(
            _loggerMock.Object,
            null,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_IBackgroundTaskQueueがnullの場合例外がスローされる()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            null,
            _serviceScopeFactoryMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_IServiceScopeFactoryがnullの場合例外がスローされる()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            null));
    }

    #endregion

    #region 共通のテスト

    [Fact]
    public async Task Failure_共通_messageIdが不足している場合BadRequestが返される()
    {
        // Arrange
        string? messageId = null; // ヘッダーが不足している場合
        var messageTimestamp = DateTime.UtcNow.ToString("o");
        var messageType = WebhookMessageType.Verification;
        var signature = "test-signature";
        var controller = new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object);
        // Act
        var result = await controller.ReceiveWebhookAsync(messageId, messageTimestamp, messageType, signature);
        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Failure_共通_messageTimestampが不足している場合BadRequestが返される()
    {
        // Arrange
        var messageId = "test-message-id";
        string? messageTimestamp = null; // ヘッダーが不足している場合
        var messageType = WebhookMessageType.Verification;
        var signature = "test-signature";
        var controller = new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object);
        // Act
        var result = await controller.ReceiveWebhookAsync(messageId, messageTimestamp, messageType, signature);
        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Failure_共通_messageTypeが不足している場合BadRequestが返される()
    {
        // Arrange
        var messageId = "test-message-id";
        var messageTimestamp = DateTime.UtcNow.ToString("o");
        string? messageType = null; // 不明なメッセージタイプ
        var signature = "test-signature";
        var controller = new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object);
        // Act
        var result = await controller.ReceiveWebhookAsync(messageId, messageTimestamp, messageType, signature);
        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Failure_共通_signatureが不足している場合BadRequestが返される()
    {
        // Arrange
        var messageId = "test-message-id";
        var messageTimestamp = DateTime.UtcNow.ToString("o");
        var messageType = WebhookMessageType.Verification;
        string? signature = null; // ヘッダーが不足している場合
        var controller = new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object);
        // Act
        var result = await controller.ReceiveWebhookAsync(messageId, messageTimestamp, messageType, signature);
        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Failure_共通_不明なメッセージタイプの場合BadRequestが返される()
    {
        // Arrange
        var messageId = "test-message-id";
        var messageTimestamp = DateTime.UtcNow.ToString("o");
        var messageType = "unknown-type"; // 不明なメッセージタイプ
        var signature = "test-signature";

        _twitchVerifySignatureServiceMock
            .Setup(service => service.VerifySignature(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(true);

        var controller = new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"challenge\":\"test-challenge\"}"))
                }
            }
        };
        // Act
        var result = await controller.ReceiveWebhookAsync(messageId, messageTimestamp, messageType, signature);
        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Failure_共通_タイムスタンプが無効な場合BadRequestが返される()
    {
        // Arrange
        var messageId = "test-message-id";
        var messageTimestamp = "invalid-timestamp"; // 無効なタイムスタンプ
        var messageType = WebhookMessageType.Verification;
        var signature = "test-signature";
        _twitchVerifySignatureServiceMock
            .Setup(service => service.VerifySignature(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(true);
        var controller = new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"challenge\":\"test-challenge\"}"))
                }
            }
        };
        // Act
        var result = await controller.ReceiveWebhookAsync(messageId, messageTimestamp, messageType, signature);
        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Failure_共通_タイムスタンプが5分以上前の場合BadRequestが返される()
    {
        // Arrange
        var messageId = "test-message-id";
        var messageTimestamp = DateTime.UtcNow.AddMinutes(-6).ToString("o"); // 5分以上前のタイムスタンプ
        var messageType = WebhookMessageType.Verification;
        var signature = "test-signature";
        _twitchVerifySignatureServiceMock
            .Setup(service => service.VerifySignature(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(true);
        var controller = new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"challenge\":\"test-challenge\"}"))
                }
            }
        };
        // Act
        var result = await controller.ReceiveWebhookAsync(messageId, messageTimestamp, messageType, signature);
        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Failure_共通_署名が無効な場合Unauthorizedが返される()
    {
        // Arrange
        var messageId = "test-message-id";
        var messageTimestamp = DateTime.UtcNow.ToString("o");
        var messageType = WebhookMessageType.Verification;
        var signature = "invalid-signature";

        // 署名が無効と仮定してモックを設定
        _twitchVerifySignatureServiceMock
            .Setup(service => service.VerifySignature(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(false);

        var controller = new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"challenge\":\"test-challenge\"}"))
                }
            }
        };

        // Act
        var result = await controller.ReceiveWebhookAsync(messageId, messageTimestamp, messageType, signature);
        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    #endregion

    #region Verificationのテスト

    [Fact]
    public async Task Success_Verification_検証リクエストが正常に処理される()
     {
        // Arrange
        var messageId = "test-message-id";
        var messageTimestamp = DateTime.UtcNow.ToString("o");
        var messageType = WebhookMessageType.Verification;
        var signature = "test-signature";

        // 署名が正しいと仮定してモックを設定
        _twitchVerifySignatureServiceMock
            .Setup(service => service.VerifySignature(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .Returns(true);

        // ITwitchEventProcessingService のモックを検証レスポンスを返すよう設定
        _eventProcessingServiceMock
            .Setup(service => service.ProcessVerificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync("test-challenge");

        // インスタンスを作成し、HttpContextを設定
        var controller = new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"challenge\":\"test-challenge\"}"))
                }
            }
        };

        // Act
        var result = await controller.ReceiveWebhookAsync(messageId, messageTimestamp, messageType, signature);
        // Assert
        Assert.IsType<ContentResult>(result);
    }

    [Fact]
    public async Task Failure_Verification_検証リクエストの処理が失敗した場合BadRequestが返される()
    {
        // Arrange
        var messageId = "test-message-id";
        var messageTimestamp = DateTime.UtcNow.ToString("o");
        var messageType = WebhookMessageType.Verification;
        var signature = "test-signature";
        // 署名が正しいと仮定してモックを設定
        _twitchVerifySignatureServiceMock
            .Setup(service => service.VerifySignature(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .Returns(true);
        // ITwitchEventProcessingService のモックを null を返すよう設定（処理失敗をシミュレート）
        _eventProcessingServiceMock
            .Setup(service => service.ProcessVerificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync((string?)null);
        // インスタンスを作成し、HttpContextを設定
        var controller = new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"challenge\":\"test-challenge\"}"))
                }
            }
        };
        // Act
        var result = await controller.ReceiveWebhookAsync(messageId, messageTimestamp, messageType, signature);
        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Eventの処理のテスト

    [Fact]
    public async Task Success_Event_イベントリクエストが正常に処理される()
    {
        // Arrange
        var messageId = "test-message-id";
        var messageTimestamp = DateTime.UtcNow.ToString("o");
        var messageType = WebhookMessageType.Notification;
        var signature = "test-signature";
        // 署名が正しいと仮定してモックを設定
        _twitchVerifySignatureServiceMock
            .Setup(service => service.VerifySignature(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .Returns(true);
        // ITwitchEventProcessingService のモックを正常に処理するよう設定
        _eventProcessingServiceMock
            .Setup(service => service.ProcessNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // インスタンスを作成し、HttpContextを設定
        var controller = new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"event\":\"test-event\"}"))
                }
            }
        };
        // Act
        var result = await controller.ReceiveWebhookAsync(messageId, messageTimestamp, messageType, signature);
        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Success_Event_イベント処理が失敗した場合もAPIに波及せずNoContentが返される()
    {
        // Arrange
        var messageId = "test-message-id";
        var messageTimestamp = DateTime.UtcNow.ToString("o");
        var messageType = WebhookMessageType.Notification;
        var signature = "test-signature";
        // 署名が正しいと仮定してモックを設定
        _twitchVerifySignatureServiceMock
            .Setup(service => service.VerifySignature(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>()))
            .Returns(true);
        // ITwitchEventProcessingService のモックを処理失敗するよう設定
        _eventProcessingServiceMock
            .Setup(service => service.ProcessNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        // インスタンスを作成し、HttpContextを設定
        var controller = new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"event\":\"test-event\"}"))
                }
            }
        };
        // Act
        var result = await controller.ReceiveWebhookAsync(messageId, messageTimestamp, messageType, signature);
        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Success_Event_イベント処理がキャンセルされた場合でも例外がAPIに波及せずNoContentが返される()
    {
        // Arrange
        var messageId = "test-message-id";
        var messageTimestamp = DateTime.UtcNow.ToString("o");
        var messageType = WebhookMessageType.Notification;
        var signature = "test-signature";
        // 署名が正しいと仮定してモックを設定
        _twitchVerifySignatureServiceMock
            .Setup(service => service.VerifySignature(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(true);
        // ITwitchEventProcessingService のモックを処理失敗するよう設定
        _eventProcessingServiceMock
            .Setup(service => service.ProcessNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("Operation was canceled."));
        // インスタンスを作成し、HttpContextを設定
        var controller = new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"event\":\"test-event\"}"))
                }
            }
        };
        // Act
        var result = await controller.ReceiveWebhookAsync(messageId, messageTimestamp, messageType, signature);
        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Success_Event_イベント処理中に例外が発生してもAPIに波及せずNoContentが返される()
    {
        // Arrange
        var messageId = "test-message-id";
        var messageTimestamp = DateTime.UtcNow.ToString("o");
        var messageType = WebhookMessageType.Notification;
        var signature = "test-signature";
        // 署名が正しいと仮定してモックを設定
        _twitchVerifySignatureServiceMock
            .Setup(service => service.VerifySignature(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(true);
        // ITwitchEventProcessingService のモックを処理中に例外をスローするよう設定
        _eventProcessingServiceMock
            .Setup(service => service.ProcessNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error occurred."));
        // インスタンスを作成し、HttpContextを設定
        var controller = new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"event\":\"test-event\"}"))
                }
            }
        };
        // Act
        var result = await controller.ReceiveWebhookAsync(messageId, messageTimestamp, messageType, signature);
        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    #endregion

    #region Revocationのテスト

    [Fact]
    public async Task Success_Revocation_リクエストが正常に処理される()
    {
        // Arrange
        var messageId = "test-message-id";
        var messageTimestamp = DateTime.UtcNow.ToString("o");
        var messageType = WebhookMessageType.Revocation;
        var signature = "test-signature";
        // 署名が正しいと仮定してモックを設定
        _twitchVerifySignatureServiceMock
            .Setup(service => service.VerifySignature(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(true);
        // ITwitchEventProcessingService のモックを正常に処理するよう設定
        _eventProcessingServiceMock
            .Setup(service => service.ProcessRevocationAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);
        // インスタンスを作成し、HttpContextを設定
        var controller = new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"subscription\":{\"status\":\"revoked\"}}"))
                }
            }
        };
        // Act
        var result = await controller.ReceiveWebhookAsync(messageId, messageTimestamp, messageType, signature);
        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Failure_Revocation_リクエスト処理が失敗した場合BadRequestが返される()
    {
        // Arrange
        var messageId = "test-message-id";
        var messageTimestamp = DateTime.UtcNow.ToString("o");
        var messageType = WebhookMessageType.Revocation;
        var signature = "test-signature";
        // 署名が正しいと仮定してモックを設定
        _twitchVerifySignatureServiceMock
            .Setup(service => service.VerifySignature(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(true);
        // ITwitchEventProcessingService のモックを処理失敗するよう設定
        _eventProcessingServiceMock
            .Setup(service => service.ProcessRevocationAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(false);
        // インスタンスを作成し、HttpContextを設定
        var controller = new TwitchWebhookController(
            _loggerMock.Object,
            _twitchVerifySignatureServiceMock.Object,
            _backgroundTaskQueueMock,
            _serviceScopeFactoryMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"subscription\":{\"status\":\"revoked\"}}"))
                }
            }
        };
        // Act
        var result = await controller.ReceiveWebhookAsync(messageId, messageTimestamp, messageType, signature);
        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion
}
