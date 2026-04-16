using NotificationDiscordBot.Server.Interfaces.Services;

namespace NotificationDiscordBot.Server.Test.Mock;

public class MockBackgroundQueueTask : IBackgroundTaskQueue
{
    public ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        throw new NotSupportedException("DequeueAsyncは現在使用されないためサポートされていません。");
    }

    public ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
    {
        // 本来バックグラウンドタスクをキューに追加するが、モックでは直列に実行する
        return workItem(CancellationToken.None);
    }
}
