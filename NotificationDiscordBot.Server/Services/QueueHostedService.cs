using NotificationDiscordBot.Server.Interfaces.Services;

namespace NotificationDiscordBot.Server.Services;

public sealed class QueueHostedService : BackgroundService
{
    private readonly ILogger<QueueHostedService> _logger;
    private readonly IBackgroundTaskQueue _taskQueue;

    public QueueHostedService(
        ILogger<QueueHostedService> logger,
        IBackgroundTaskQueue taskQueue)
    {
        _logger = logger;
        _taskQueue = taskQueue;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("バックグラウンドタスクキューサービスが開始されました。");

        return ProcessTaskQueueAsync(stoppingToken);
    }

    private async Task ProcessTaskQueueAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Func<CancellationToken, ValueTask>? workItem = await _taskQueue.DequeueAsync(stoppingToken);

                await workItem(stoppingToken);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "バックグラウンドタスクの処理がキャンセルされました。");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "バックグラウンドタスクの処理中にエラーが発生しました。");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("バックグラウンドタスクキューサービスが停止されます。");
        await base.StopAsync(cancellationToken);
    }
}
