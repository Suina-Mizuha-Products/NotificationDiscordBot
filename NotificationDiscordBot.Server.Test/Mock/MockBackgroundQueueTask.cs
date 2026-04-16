using NotificationDiscordBot.Server.Interfaces.Services;

namespace NotificationDiscordBot.Server.Test.Mock;

public class MockBackgroundQueueTask : IBackgroundTaskQueue
{
    /// <summary>
    /// バックグラウンドタスクキューのモック実装。テスト側で任意のタイミングで実行するために、キューに追加されたタスクを保持するプロパティです。
    /// </summary>
    public ValueTask<Func<CancellationToken, ValueTask>> BackgroundWorkItem { get; set; }

    public ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        throw new NotSupportedException("DequeueAsyncは現在使用されないためサポートされていません。");
    }

    public ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
    {
        BackgroundWorkItem = new ValueTask<Func<CancellationToken, ValueTask>>(workItem);

        return new ValueTask();
    }
}
