using Discord;
using Discord.Rest;

namespace NotificationDiscordBot.Server.Interfaces.Services.Discord;

public interface ISocketTextChannel
{
    Task<RestUserMessage>? SendMessageAsync(string text, bool isTTS = false, Embed? embed = null, RequestOptions? options = null, AllowedMentions? allowedMentions = null, MessageReference? messageReference = null, MessageComponent? components = null, ISticker[]? stickers = null, Embed[]? embeds = null, MessageFlags flags = MessageFlags.None, PollProperties? pollProperties = null);
}
