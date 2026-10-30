using Microsoft.Extensions.Logging;
using Moq;
using NotificationDiscordBot.Server.Interfaces.Services;
using NotificationDiscordBot.Server.Services;

namespace NotificationDiscordBot.Server.Test.Services;

public class QueueHostedServiceTest
{
    private readonly Mock<ILogger<QueueHostedService>> _loggerMock;
    private readonly Mock<IBackgroundTaskQueue> _taskQueueMock;

    public QueueHostedServiceTest()
    {
        _loggerMock = new Mock<ILogger<QueueHostedService>>();
        _taskQueueMock = new Mock<IBackgroundTaskQueue>();
    }

    #region コンストラクタのテスト

    [Fact]
    public void Success_コンストラクタ_全ての依存関係が提供された場合インスタンスが作成される()
    {
        // Act
        var service = new QueueHostedService(_loggerMock.Object, _taskQueueMock.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Failure_コンストラクタ_ILoggerがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new QueueHostedService(null, _taskQueueMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_IBackgroundTaskQueueがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new QueueHostedService(_loggerMock.Object, null));
    }

    #endregion

    #region ExecuteAsyncのテスト

    [Fact]
    public async Task Success_ExecuteAsync_キューから取得したworkItemが実行される()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var executed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        Func<CancellationToken, ValueTask> workItem = _ =>
        {
            executed.TrySetResult(true);
            cts.Cancel();
            return ValueTask.CompletedTask;
        };

        _taskQueueMock
            .Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<Func<CancellationToken, ValueTask>>(workItem));

        var service = new QueueHostedService(_loggerMock.Object, _taskQueueMock.Object);

        // Act
        await service.StartAsync(cts.Token);
        await executed.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await service.StopAsync(CancellationToken.None);

        // Assert
        _taskQueueMock.Verify(q => q.DequeueAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Success_ExecuteAsync_workItemが例外をスローしても次のworkItemの処理を継続する()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var recovered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dequeueCount = 0;

        Func<CancellationToken, ValueTask> failureWorkItem = _ =>
        {
            throw new InvalidOperationException("work item failed");
        };

        Func<CancellationToken, ValueTask> successWorkItem = _ =>
        {
            recovered.TrySetResult(true);
            cts.Cancel();
            return ValueTask.CompletedTask;
        };

        _taskQueueMock
            .Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                dequeueCount++;

                return dequeueCount switch
                {
                    1 => new ValueTask<Func<CancellationToken, ValueTask>>(failureWorkItem),
                    2 => new ValueTask<Func<CancellationToken, ValueTask>>(successWorkItem),
                    _ => ValueTask.FromException<Func<CancellationToken, ValueTask>>(new OperationCanceledException()),
                };
            });

        var service = new QueueHostedService(_loggerMock.Object, _taskQueueMock.Object);

        // Act
        await service.StartAsync(cts.Token);
        await recovered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await service.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(dequeueCount >= 2);
    }

    #endregion
}
