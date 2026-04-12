using NotificationDiscordBot.Server.Entity.Twitch;

namespace NotificationDiscordBot.Server.Interfaces.Services.Twitch;

public interface ITwitchApiInvocationService
{
    Task<GetTwitchCredentialsResponse?> GetTwitchCredentialsAsync();

    Task<GetStreamsResponse?> GetStreamsAsync(string accessToken, string userId);

    Task<GetUserResponse?> GetUserInfoAsync(string accessToken, string userId);
}
