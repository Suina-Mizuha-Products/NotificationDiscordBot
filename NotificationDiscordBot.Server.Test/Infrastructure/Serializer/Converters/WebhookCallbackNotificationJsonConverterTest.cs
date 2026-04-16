using NotificationDiscordBot.Server.Entity.Twitch;
using NotificationDiscordBot.Server.Infrastructure.Serializer.Converters;
using System.Text.Json;

namespace NotificationDiscordBot.Server.Test.Infrastructure.Serializer.Converters;

public class WebhookCallbackNotificationJsonConverterTest
{
    private readonly JsonSerializerOptions _options;

    public WebhookCallbackNotificationJsonConverterTest()
    {
        _options = new JsonSerializerOptions
        {
            Converters = { new WebhookCallbackNotificationJsonConverter() }
        };
    }

    [Fact]
    public void Success_Read_ストリームオンラインイベントが来た場合StreamOnlineEvent型で処理される()
    {
        // Arrange
        // サンプルのJsonデータ(Twitch CLIを元に作成)
        var json = @"{
            ""subscription"": {
            ""id"": ""test-subscription-id"",
            ""status"": ""enabled"",
            ""type"": ""stream.online"",
            ""version"": ""1"",
            ""condition"": {
                ""broadcaster_user_id"": ""91724331""
            },
            ""transport"": {
                ""method"": ""webhook"",
                ""callback"": ""null""
            },
            ""created_at"": ""2026-04-16T21:44:36.3252297Z"",
            ""cost"": 0
            },
            ""event"": {
            ""id"": ""49461215"",
            ""broadcaster_user_id"": ""91724331"",
            ""broadcaster_user_login"": ""testBroadcaster"",
            ""broadcaster_user_name"": ""testBroadcaster"",
            ""type"": ""live"",
            ""started_at"": ""2026-04-16T21:44:36.3252297Z""
            }
        }";
        // Act
        var notification = JsonSerializer.Deserialize<WebhookCallbackNotification>(json, _options);
        // Assert
        Assert.NotNull(notification);
        Assert.Equal("test-subscription-id", notification.Subscription.Id);
        Assert.Equal("stream.online", notification.Subscription.Type);
        Assert.IsType<StreamOnlineEvent>(notification.Event);
    }

    [Fact]
    public void Success_Read_未処理のイベントが来た場合UnknownEvent型で処理される()
    {
        // Arrange
        // サンプルのJsonデータ
        var json = @"{
          ""subscription"": {
            ""id"": ""test-subscription-id"",
            ""status"": ""enabled"",
            ""type"": ""unknown.event"",
            ""version"": ""1"",
            ""condition"": {
              ""broadcaster_user_id"": ""88915298""
            },
            ""transport"": {
              ""method"": ""webhook"",
              ""callback"": ""null""
            },
            ""created_at"": ""2026-04-16T21:55:34.2180993Z"",
            ""cost"": 0
          },
          ""event"": {
            ""user_id"": ""19117137"",
            ""user_login"": ""testFromUser"",
            ""user_name"": ""testFromUser"",
            ""broadcaster_user_id"": ""88915298"",
            ""broadcaster_user_login"": ""testBroadcaster"",
            ""broadcaster_user_name"": ""testBroadcaster"",
            ""tier"": ""1000"",
            ""is_gift"": false
          }
        }";
        // Act
        var notification = JsonSerializer.Deserialize<WebhookCallbackNotification>(json, _options);
        // Assert
        Assert.NotNull(notification);
        Assert.Equal("test-subscription-id", notification.Subscription.Id);
        Assert.Equal("unknown.event", notification.Subscription.Type);

        Assert.IsType<UnknownEvent>(notification.Event);
    }

    [Fact]
    public void Failure_Read_サブスクリプション情報がない場合JsonExceptionが発生する()
    {
        // Arrange
        var json = @"{
            ""event"": {
                ""id"": ""49461215"",
                ""broadcaster_user_id"": ""91724331"",
                ""broadcaster_user_login"": ""testBroadcaster"",
                ""broadcaster_user_name"": ""testBroadcaster"",
                ""type"": ""live"",
                ""started_at"": ""2026-04-16T21:44:36.3252297Z""
            }
        }";
        // Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WebhookCallbackNotification>(json, _options));
    }

    [Fact]
    public void Failure_Read_イベント情報がない場合JsonExceptionが発生する()
    {
        // Arrange
        var json = @"{
            ""subscription"": {
                ""id"": ""test-subscription-id"",
                ""status"": ""enabled"",
                ""type"": ""stream.online"",
                ""version"": ""1"",
                ""condition"": {
                    ""broadcaster_user_id"": ""91724331""
                },
                ""transport"": {
                    ""method"": ""webhook"",
                    ""callback"": ""null""
                },
                ""created_at"": ""2026-04-16T21:44:36.3252297Z"",
                ""cost"": 0
            }
        }";
        // Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WebhookCallbackNotification>(json, _options));
    }

    [Fact]
    public void Failure_Write_シリアル化はサポートされていないためNotSupportedExceptionが発生する()
    {
        // Arrange
        var notification = new WebhookCallbackNotification
        {
            Subscription = new Subscription
            {
                Id = "test-subscription-id",
                Type = "stream.online"
            },
            Event = new StreamOnlineEvent
            {
                Id = "49461215",
                BroadcasterUserId = "91724331",
                BroadcasterUserLogin = "testBroadcaster",
                BroadcasterUserName = "testBroadcaster",
                Type = "live",
                StartedAt = DateTime.UtcNow.ToString("o")
            }
        };
        // Act & Assert
        Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(notification, _options));
    }
}