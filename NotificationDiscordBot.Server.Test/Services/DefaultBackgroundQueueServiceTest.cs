using NotificationDiscordBot.Server.Services;

namespace NotificationDiscordBot.Server.Test.Services;

public class DefaultBackgroundQueueServiceTest
{
    #region コンストラクタのテスト
    [Fact]
    public void Success_コンストラクタ_capacityが正の値の場合インスタンスが生成される()
    {
        // Act
        var service = new DefaultBackgroundQueueService(10);
        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Failure_コンストラクタ_capacityが0以下の場合ArgumentOutOfRangeExceptionがスローされる()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DefaultBackgroundQueueService(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DefaultBackgroundQueueService(-1));
    }
    #endregion

    #region QueueBackgroundWorkItemAsyncのテスト
    [Fact]
    public async Task Success_QueueBackgroundWorkItemAsync_追加したworkItemがDequeueAsyncで取得できる()
    {
        // Arrange
        var service = new DefaultBackgroundQueueService();
        var executed = false;
        Func<CancellationToken, ValueTask> workItem = _ =>
        {
            executed = true;
            return ValueTask.CompletedTask;
        };

        // Act
        await service.QueueBackgroundWorkItemAsync(workItem);
        var dequeued = await service.DequeueAsync(CancellationToken.None);
        await dequeued(CancellationToken.None);

        // Assert
        Assert.Same(workItem, dequeued);
        Assert.True(executed);
    }

    [Fact]
    public async Task Failure_QueueBackgroundWorkItemAsync_workItemがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Arrange
        var service = new DefaultBackgroundQueueService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await service.QueueBackgroundWorkItemAsync(null!);
        });
    }
    #endregion

    #region DequeueAsyncのテスト
    [Fact]
    public async Task Success_DequeueAsync_複数のworkItemをFIFO順で取得できる()
    {
        // Arrange
        var service = new DefaultBackgroundQueueService();

        Func<CancellationToken, ValueTask> first = _ => ValueTask.CompletedTask;
        Func<CancellationToken, ValueTask> second = _ => ValueTask.CompletedTask;

        await service.QueueBackgroundWorkItemAsync(first);
        await service.QueueBackgroundWorkItemAsync(second);

        // Act
        var dequeued1 = await service.DequeueAsync(CancellationToken.None);
        var dequeued2 = await service.DequeueAsync(CancellationToken.None);

        // Assert
        Assert.Same(first, dequeued1);
        Assert.Same(second, dequeued2);
    }

    [Fact]
    public async Task Failure_DequeueAsync_キャンセル済みトークンが渡された場合OperationCanceledExceptionがスローされる()
    {
        // Arrange
        var service = new DefaultBackgroundQueueService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await service.DequeueAsync(cts.Token);
        });
    }
    #endregion
}
