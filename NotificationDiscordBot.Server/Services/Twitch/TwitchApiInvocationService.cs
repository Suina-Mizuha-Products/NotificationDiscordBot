using NotificationDiscordBot.Server.Entity.Twitch;
using NotificationDiscordBot.Server.Interfaces.Services.Twitch;
using System.Net.Http.Headers;
using System.Text.Json;

namespace NotificationDiscordBot.Server.Services.Twitch;

public class TwitchApiInvocationService : ITwitchApiInvocationService
{
    private readonly ILogger<TwitchApiInvocationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private const string TwitchEndpoint = "https://api.twitch.tv/helix";

    public TwitchApiInvocationService(ILogger<TwitchApiInvocationService> logger, IConfiguration configuration, IHttpClientFactory clientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }
        _clientId = configuration["Twitch:ClientId"] ?? throw new ArgumentNullException("TwitchのClientIdが設定されていません。");
        _clientSecret = configuration["Twitch:ClientSecret"] ?? throw new ArgumentNullException("TwitchのClientSecretが設定されていません。");
    }

    public async Task<GetTwitchCredentialsResponse?> GetTwitchCredentialsAsync()
    {
        try
        {
            var requestUri = "https://id.twitch.tv/oauth2/token";

            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var httpClient = _httpClientFactory.CreateClient("Twitch Client");

            var response = await httpClient.PostAsync(requestUri, requestContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Twitchの認証情報の取得に失敗しました。ステータスコード: {StatusCode}, レスポンス: {ResponseContent}", response.StatusCode, await response.Content.ReadAsStringAsync());
                throw new Exception("Twitchの認証情報の取得に失敗しました。");
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            var credentials = JsonSerializer.Deserialize<GetTwitchCredentialsResponse>(responseContent);

            if (credentials == null)
            {
                _logger.LogError("Twitchの認証情報の取得に失敗しました。レスポンス: {ResponseContent}", responseContent);
                throw new Exception("Twitchの認証情報の取得に失敗しました。");
            }

            return credentials;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twitchの認証情報の取得中にエラーが発生しました。");
            return null;
        }
    }

    public async Task<GetStreamsResponse?> GetStreamsAsync(string accessToken, string userId)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{TwitchEndpoint}/streams?user_id={userId}");

            request.Headers.Add("Client-ID", _clientId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var httpClient = _httpClientFactory.CreateClient("Twitch Client");

            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Twitchのストリーム情報の取得に失敗しました。ステータスコード: {StatusCode}, レスポンス: {ResponseContent}", response.StatusCode, errorContent);
                throw new Exception("Twitchのストリーム情報の取得に失敗しました。");
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(responseContent);
            var streams = JsonSerializer.Deserialize<GetStreamsResponse>(responseContent);

            if (streams == null)
            {
                _logger.LogError("Twitchのストリーム情報の取得に失敗しました。レスポンス: {ResponseContent}", responseContent);
                throw new Exception("Twitchのストリーム情報の取得に失敗しました。");
            }

            return streams;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twitchのストリーム情報の取得中にエラーが発生しました。");
            return null;
        }
    }

    public async Task<GetUserResponse?> GetUserInfoAsync(string accessToken, string userId)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{TwitchEndpoint}/users?id={userId}");

            request.Headers.Add("Client-ID", _clientId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var httpClient = _httpClientFactory.CreateClient("Twitch Client");

            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Twitchのユーザー情報の取得に失敗しました。ステータスコード: {StatusCode}, レスポンス: {ResponseContent}", response.StatusCode, errorContent);
                throw new Exception("Twitchのユーザー情報の取得に失敗しました。");
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(responseContent);
            var userInfo = JsonSerializer.Deserialize<GetUserResponse>(responseContent);

            if (userInfo == null)
            {
                _logger.LogError("Twitchのユーザー情報の取得に失敗しました。レスポンス: {ResponseContent}", responseContent);
                throw new Exception("Twitchのユーザー情報の取得に失敗しました。");
            }

            _logger.LogInformation(JsonSerializer.Serialize(userInfo));

            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twitchのユーザー情報の取得中にエラーが発生しました。");
            return null;
        }
    }
}
