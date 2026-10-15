using Discord;
using Discord.WebSocket;
using NotificationDiscordBot.Server.Interfaces.Services.Discord;

namespace NotificationDiscordBot.Server.Services.Discord;

public class SendNotificationService : ISendNotificationService
{
    private readonly ILogger<SendNotificationService> _logger;
    private readonly DiscordSocketClient _discordClient;
    private readonly ulong _channelId;

    public SendNotificationService(ILogger<SendNotificationService> logger, IConfiguration configuration, DiscordSocketClient discordClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
        configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        var channelIdString = configuration["Discord:ChannelId"] ?? throw new InvalidOperationException("チャンネルIdが設定されていません。");
        var isUlong = ulong.TryParse(channelIdString, out _channelId);
        if (!isUlong)
        {
            throw new InvalidOperationException("無効なチャンネルIdが設定されています。");
        }
    }

    public async Task<bool> SendTwitchOnlineNotificationMessageAsync(
        string userLogin,
        string userName, 
        string profileImageUrl,
        string title, 
        string gameName, 
        string startedAt, 
        string thumnailUrl)
    {
        var twitchChannelUrl = $"https://www.twitch.tv/{userLogin}";
        DateTimeOffset.TryParse(startedAt, out var startedDateTimeOffset);
        _logger.LogInformation(thumnailUrl); //https://static-cdn.jtvnw.net/previews-ttv/live_user_mizuha_hk-{width}x{height}.jpg
        var thumnailUrlResult = thumnailUrl.Replace("{width}", "1920").Replace("{height}", "1080");

        var embedBuilder = new EmbedBuilder()
            .WithAuthor(userName, iconUrl: profileImageUrl, url: twitchChannelUrl)
            .WithTitle("🔴 配信が開始されました！")
            .WithDescription(title)
            .WithUrl(twitchChannelUrl)
            .WithImageUrl(thumnailUrlResult)
            .AddField("ゲーム", gameName, inline: false)
            .WithColor(new Color(0x6441A5))
            .WithTimestamp(startedDateTimeOffset);

        var channel = _discordClient.GetChannel(_channelId) as SocketTextChannel;
        if(channel == null)
        {
            _logger.LogWarning("チャンネルの取得に失敗しました。");
            return false;
        }

        await channel.SendMessageAsync("@here Stream Online!", embed: embedBuilder.Build());

        return true;
    }
}
