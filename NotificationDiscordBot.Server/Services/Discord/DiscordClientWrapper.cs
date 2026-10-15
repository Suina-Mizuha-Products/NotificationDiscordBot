using Discord;
using Discord.WebSocket;
using NotificationDiscordBot.Server.Interfaces.Services.Discord;
using IDiscordClient = NotificationDiscordBot.Server.Interfaces.Services.Discord.IDiscordClient;

namespace NotificationDiscordBot.Server.Services.Discord;

public class DiscordClientWrapper : IDiscordClient
{
    private readonly DiscordSocketClient _client;

    public DiscordClientWrapper(DiscordSocketClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));

        _client.Log += (msg) => Log?.Invoke(msg) ?? Task.CompletedTask;
        _client.Ready += () => Ready?.Invoke() ?? Task.CompletedTask;
        _client.Connected += () => Connected?.Invoke() ?? Task.CompletedTask;
        _client.Disconnected += (ex) => Disconnected?.Invoke(ex) ?? Task.CompletedTask;
    }

    public SocketSelfUser CurrentUser => _client.CurrentUser;

    public ConnectionState ConnectionState => _client.ConnectionState;

    public event Func<LogMessage, Task>? Log;
    public event Func<Task>? Ready;
    public event Func<Task>? Connected;
    public event Func<Exception, Task>? Disconnected;

    public Task LoginAsync(TokenType tokenType, string token, bool validateToken = true) 
        => _client.LoginAsync(tokenType, token, validateToken);

    public Task StartAsync()
        => _client.StartAsync();

    public ISocketTextChannel? GetTextChannel(ulong channelId)
    {
        var channel = _client.GetChannel(channelId) as SocketTextChannel;
        if (channel == null)
            return null;
        return new SocketTextChannelWrapper(channel);
    }
}
