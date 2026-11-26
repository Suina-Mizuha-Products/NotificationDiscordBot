using Discord;
using Discord.Rest;
using Discord.WebSocket;
using NotificationDiscordBot.Server.Interfaces.Services.Discord;

namespace NotificationDiscordBot.Server.Services.Discord;

public class SocketTextChannelWrapper : ISocketTextChannel
{
    private readonly SocketTextChannel _channel;

    public SocketTextChannelWrapper(SocketTextChannel channel)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    public Task<RestUserMessage>? SendMessageAsync(string text, bool isTTS = false, Embed? embed = null, RequestOptions? options = null, AllowedMentions? allowedMentions = null, MessageReference? messageReference = null, MessageComponent? components = null, ISticker[]? stickers = null, Embed[]? embeds = null, MessageFlags flags = MessageFlags.None, PollProperties? pollProperties = null)
     => _channel.SendMessageAsync(text, isTTS, embed, options, allowedMentions, messageReference, components, stickers, embeds, flags, pollProperties);
}
