using Discord;
using IDiscordClient = NotificationDiscordBot.Server.Interfaces.Services.Discord.IDiscordClient;

namespace NotificationDiscordBot.Server.Services.Discord;

/// <summary>
/// Discord Botの接続とイベント管理を行うサービスクラス。
/// </summary>
public class DiscordBotService : BackgroundService, IDisposable
{
    private readonly ILogger<DiscordBotService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _discordToken;
    private readonly IDiscordClient _client;

    public DiscordBotService(
        ILogger<DiscordBotService> logger,
        IConfiguration configuration,
        IDiscordClient client)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        var token = _configuration["Discord-Token"];
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Discordのトークンが設定されていません。");
        }
        _discordToken = token;
        _client = client ?? throw new ArgumentNullException(nameof(client));

        #region イベント登録
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.Connected += OnConnectedAsync;
        _client.Disconnected += OnExceptionAsync;
        #endregion
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.LoginAsync(TokenType.Bot, _discordToken);
        await _client.StartAsync();

        // サーバーがストップするまで接続を維持
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        #region イベント解除
        _client.Log -= LogAsync;
        _client.Ready -= ReadyAsync;
        _client.Connected -= OnConnectedAsync;
        _client.Disconnected -= OnExceptionAsync;
        #endregion

        base.Dispose();
        GC.SuppressFinalize(this);
    }

    private Task LogAsync(LogMessage logMessage)
    {
        switch (logMessage.Severity)
        {
            case LogSeverity.Critical:
                _logger.LogCritical(logMessage.Exception, "{Message}", logMessage.Message);
                break;
            case LogSeverity.Error:
                _logger.LogError(logMessage.Exception, "{Message}", logMessage.Message);
                break;
            case LogSeverity.Warning:
                _logger.LogWarning("{Message}", logMessage.Message);
                break;
            case LogSeverity.Info:
                _logger.LogInformation("{Message}", logMessage.Message);
                break;
            case LogSeverity.Verbose:
                _logger.LogTrace("{Message}", logMessage.Message);
                break;
            case LogSeverity.Debug:
                _logger.LogDebug("{Message}", logMessage.Message);
                break;
            default:
                _logger.LogInformation("{Message}", logMessage.Message);
                break;
        }
        return Task.CompletedTask;
    }

    private Task ReadyAsync()
    {
        _logger.LogInformation("DiscordBotの準備完了。CurrentUserId: {CurrentUserId}, CurrentUserName: {CurrentUserName}",
            _client.CurrentUser.Id,
            _client.CurrentUser.Username);
        return Task.CompletedTask;
    }

    private Task OnConnectedAsync()
    {
        _logger.LogInformation("Discordへの接続完了。");
        return Task.CompletedTask;
    }

    private Task OnExceptionAsync(Exception exception)
    {
        _logger.LogError(exception, "Discord Clientで予期せぬエラーが発生しました。");
        return Task.CompletedTask;
    }
}
