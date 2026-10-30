using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NotificationDiscordBot.Server.Entity.Twitch;
using NotificationDiscordBot.Server.Services.Twitch;
using System.Net;
using System.Text.Json;

namespace NotificationDiscordBot.Server.Test.Services.Twitch;

public class TwitchApiInvocationServiceTest
{
    private readonly Mock<ILogger<TwitchApiInvocationService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private const string testAccessToken = "test_access_token";
    private const string testClientId = "test_client_id";
    private const string testClientSecret = "test_client_secret";
    private const string testUserId = "test_user_id";

    public TwitchApiInvocationServiceTest()
    {
        _loggerMock = new Mock<ILogger<TwitchApiInvocationService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
    }

    private void SetupConfigurationMock()
    {
        _configurationMock.Setup(c => c["Twitch:ClientId"]).Returns(testClientId);
        _configurationMock.Setup(c => c["Twitch:ClientSecret"]).Returns(testClientSecret);
    }

    #region コンストラクタのテスト

    [Fact]
    public void Success_コンストラクタ_すべての依存関係が提供された場合インスタンスが作成される()
    {
        // Arrange
        SetupConfigurationMock();

        // Act
        var service = new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Failure_コンストラクタ_Loggerがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Arrange
        SetupConfigurationMock();
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchApiInvocationService(null, _configurationMock.Object, _httpClientFactoryMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_Configurationがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchApiInvocationService(_loggerMock.Object, null, _httpClientFactoryMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_HttpClientFactoryがnullの場合ArgumentNullExceptionがスローされる()
    {
        // Arrange
        SetupConfigurationMock();
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, null));
    }

    [Fact]
    public void Failure_コンストラクタ_ClientIdが設定されていない場合ArgumentNullExceptionがスローされる()
    {
        // Arrange
        _configurationMock.Setup(c => c["Twitch:ClientId"]).Returns((string)null);
        _configurationMock.Setup(c => c["Twitch:ClientSecret"]).Returns(testClientSecret);
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object));
    }

    [Fact]
    public void Failure_コンストラクタ_ClientSecretが設定されていない場合ArgumentNullExceptionがスローされる()
    {
        // Arrange
        _configurationMock.Setup(c => c["Twitch:ClientId"]).Returns(testClientId);
        _configurationMock.Setup(c => c["Twitch:ClientSecret"]).Returns((string)null);
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object));
    }

    #endregion

    #region GetTwitchCredentialsAsyncのテスト

    [Fact]
    public async Task Success_GetTwitchCredentialsAsync_有効な認証情報が返される()
    {
        // Arrange
        SetupConfigurationMock();

        var expectedResponse = new GetTwitchCredentialsResponse
        {
            AccessToken = testAccessToken,
            ExpiresIn = 3600,
            TokenType = "bearer"
        };
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(c => c.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var service = new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);
        var result = await service.GetTwitchCredentialsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.AccessToken, result.AccessToken);
        Assert.Equal(expectedResponse.ExpiresIn, result.ExpiresIn);
        Assert.Equal(expectedResponse.TokenType, result.TokenType);
    }

    [Fact]
    public async Task Failure_GetTwitchCredentialsAsync_ステータスコードが異常の場合nullが返される()
    {
        // Arrange
        SetupConfigurationMock();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("bad request")
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(c => c.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var service = new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);
        var result = await service.GetTwitchCredentialsAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Failure_GetTwitchCredentialsAsync_レスポンスがnullの場合nullが返される()
    {
        // Arrange
        SetupConfigurationMock();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(string.Empty) // 空のレスポンス
            });
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(c => c.CreateClient(It.IsAny<string>())).Returns(httpClient);
        // Act
        var service = new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);
        var result = await service.GetTwitchCredentialsAsync();
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Failure_GetTwitchCredentialsAsync_例外が発生した場合nullが返される()
    {
        // Arrange
        SetupConfigurationMock();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Simulated network error"));
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(c => c.CreateClient(It.IsAny<string>())).Returns(httpClient);
        // Act
        var service = new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);
        var result = await service.GetTwitchCredentialsAsync();
        // Assert
        Assert.Null(result);
    }
    #endregion

    #region GetStreamsAsyncのテスト

    [Fact]
    public async Task Success_GetStreamsAsync_有効なストリーム情報が返される()
    {
        // Arrange
        SetupConfigurationMock();

        var expectedResponse = new GetStreamsResponse
        {
            Data =
            [
                new StreamData
                {
                    Id = "stream-id",
                    UserId = testUserId,
                    UserLogin = "test_login",
                    UserName = "test_name",
                    Type = "live",
                    Title = "test title",
                    ViewerCount = 100
                }
            ]
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString() == $"https://api.twitch.tv/helix/streams?user_id={testUserId}" &&
                    req.Headers.Contains("Client-ID") &&
                    req.Headers.Authorization != null &&
                    req.Headers.Authorization.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == testAccessToken),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(c => c.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var service = new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);
        var result = await service.GetStreamsAsync(testAccessToken, testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(expectedResponse.Data[0].Id, result.Data[0].Id);
        Assert.Equal(expectedResponse.Data[0].UserId, result.Data[0].UserId);
        Assert.Equal(expectedResponse.Data[0].Title, result.Data[0].Title);
    }

    [Fact]
    public async Task Failure_GetStreamsAsync_ステータスコードが異常の場合nullが返される()
    {
        // Arrange
        SetupConfigurationMock();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("internal server error")
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(c => c.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var service = new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);
        var result = await service.GetStreamsAsync(testAccessToken, testUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Failure_GetStreamsAsync_レスポンスがnullの場合nullが返される()
    {
        // Arrange
        SetupConfigurationMock();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(string.Empty) // 空のレスポンス
            });
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(c => c.CreateClient(It.IsAny<string>())).Returns(httpClient);
        // Act
        var service = new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);
        var result = await service.GetStreamsAsync(testAccessToken, testUserId);
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Failure_GetStreamsAsync_例外が発生した場合nullが返される()
    {
        // Arrange
        SetupConfigurationMock();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Simulated network error"));
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(c => c.CreateClient(It.IsAny<string>())).Returns(httpClient);
        // Act
        var service = new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);
        var result = await service.GetStreamsAsync(testAccessToken, testUserId);
        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetUserInfoAsyncのテスト

    [Fact]
    public async Task Success_GetUserInfoAsync_有効なユーザー情報が返される()
    {
        // Arrange
        SetupConfigurationMock();

        var expectedResponse = new GetUserResponse
        {
            Data =
            [
                new UserInfo
                {
                    Id = testUserId,
                    Login = "test_login",
                    DisplayName = "test_display_name",
                    Description = "test description"
                }
            ]
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString() == $"https://api.twitch.tv/helix/users?id={testUserId}" &&
                    req.Headers.Contains("Client-ID") &&
                    req.Headers.Authorization != null &&
                    req.Headers.Authorization.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == testAccessToken),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(c => c.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var service = new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);
        var result = await service.GetUserInfoAsync(testAccessToken, testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(expectedResponse.Data[0].Id, result.Data[0].Id);
        Assert.Equal(expectedResponse.Data[0].Login, result.Data[0].Login);
        Assert.Equal(expectedResponse.Data[0].DisplayName, result.Data[0].DisplayName);
    }

    [Fact]
    public async Task Failure_GetUserInfoAsync_ステータスコードが異常の場合nullが返される()
    {
        // Arrange
        SetupConfigurationMock();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadGateway,
                Content = new StringContent("bad gateway")
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(c => c.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var service = new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);
        var result = await service.GetUserInfoAsync(testAccessToken, testUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Failure_GetUserInfoAsync_レスポンスがnullの場合nullが返される()
    {
        // Arrange
        SetupConfigurationMock();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(string.Empty) // 空のレスポンス
            });
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(c => c.CreateClient(It.IsAny<string>())).Returns(httpClient);
        // Act
        var service = new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);
        var result = await service.GetUserInfoAsync(testAccessToken, testUserId);
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Failure_GetUserInfoAsync_例外が発生した場合nullが返される()
    {
        // Arrange
        SetupConfigurationMock();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Simulated network error"));
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(c => c.CreateClient(It.IsAny<string>())).Returns(httpClient);
        // Act
        var service = new TwitchApiInvocationService(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);
        var result = await service.GetUserInfoAsync(testAccessToken, testUserId);
        // Assert
        Assert.Null(result);
    }

    #endregion
}
