using Discord;
using Discord.WebSocket;

namespace NotificationDiscordBot.Server.Services.Discord;

public class DiscordBotService : BackgroundService, IDisposable
{
    private readonly ILogger<DiscordBotService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _discordToken;
    private readonly DiscordSocketClient _client;

    public DiscordBotService(
        ILogger<DiscordBotService> logger,
        IConfiguration configuration,
        DiscordSocketClient client)
    {
        _logger = logger;
        _configuration = configuration;
        _discordToken = _configuration["Discord:Token"] ?? throw new InvalidOperationException("DiscordのTokenが構成されていません。");
        _client = client;

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
    }

    private async Task LogAsync(LogMessage logMessage)
    {
        _logger.LogInformation(logMessage.ToString());
    }

    private async Task ReadyAsync()
    {
        _logger.LogInformation("DiscordBotの準備完了。CurrentUserId: {CurrentUserId}, CurrentUserName: {CirrentUserName}", 
            _client.CurrentUser.Id,
            _client.CurrentUser.Username);
    }

    private async Task OnConnectedAsync()
    {
        _logger.LogInformation("Discordへの接続完了。");
    }

    private async Task OnExceptionAsync(Exception exception)
    {
        _logger.LogError(exception, "Discord Clientで予期せぬエラーが発生しました。");
    }
}
