using Discord;
using Discord.WebSocket;

namespace NotificationDiscordBot.Server.Interfaces.Services.Discord;

public interface IDiscordClient
{
    Task LoginAsync(TokenType tokenType, string token, bool validateToken = true);
    Task StartAsync();
    ISocketTextChannel? GetTextChannel(ulong channelId);

    event Func<LogMessage, Task>? Log;
    event Func<Task>? Ready;
    event Func<Task>? Connected;
    event Func<Exception, Task>? Disconnected;

    SocketSelfUser CurrentUser { get; }
    ConnectionState ConnectionState { get; }
}
