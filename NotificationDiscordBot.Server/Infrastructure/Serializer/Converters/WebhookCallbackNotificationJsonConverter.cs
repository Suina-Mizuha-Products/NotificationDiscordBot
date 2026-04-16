using NotificationDiscordBot.Server.Entity.Twitch;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NotificationDiscordBot.Server.Infrastructure.Serializer.Converters;

/// <summary>
/// 不明なイベントの生データを取得または設定します。イベントの型が既知でない場合に、元の JSON データを保持します。
/// </summary>
///
/// <remarks>このプロパティは、シリアル化時に無視されます。イベントの内容を直接解析する必要がある場合に使用してください。型安全なプロパティが存在しないイベントに対して、柔軟なデータアクセスを提供します。</remarks>
public class UnknownEvent: EventBase
{
    [JsonIgnore]
    public JsonElement RawData { get; set; }
}

/// <summary>
/// WebhookCallbackNotificationクラスのカスタムJsonConverter。TwitchからのWebhookコールバックは、サブスクリプション情報とイベントデータを含むため、イベントの種類に応じて適切なEventBase派生クラスにデシリアライズする必要がある。
/// </summary>
public class WebhookCallbackNotificationJsonConverter : JsonConverter<WebhookCallbackNotification>
{
    /// <summary>
    /// JSON データから WebhookCallbackNotification オブジェクトをデシリアライズします。
    /// </summary>
    /// <remarks>イベントの種類に応じて EventBase の派生型が選択されます。未知のイベントの場合は UnknownEvent 型が使用されます。</remarks>
    /// <param name="reader">JSON データを読み取るための Utf8JsonReader への参照。</param>
    /// <param name="typeToConvert">デシリアライズする型。通常は WebhookCallbackNotification 型です。</param>
    /// <param name="options">デシリアライズ時に使用する JsonSerializerOptions。</param>
    /// <returns>デシリアライズされた WebhookCallbackNotification オブジェクト。データが無効な場合は null を返します。</returns>
    /// <exception cref="JsonException">JSON データに 'subscription' または 'event' プロパティが存在しない場合、またはデシリアライズに失敗した場合にスローされます。</exception>
    public override WebhookCallbackNotification? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // 'subscription'プロパティを取得してデシリアライズ
        if (!root.TryGetProperty("subscription", out var subscriptionElement))
        {
            throw new JsonException("'subscription'プロパティが見つかりませんでした。");
        }

        var subscription = subscriptionElement.Deserialize<Subscription>(options) ?? throw new JsonException("サブスクリプションのデシリアライズに失敗しました。");

        // イベントの種類に応じて適切なEventBase派生クラスを選択
        Type eventType = subscription.Type switch
        {
            "stream.online" => typeof(StreamOnlineEvent),
            _ => typeof(UnknownEvent)
        };

        // 'event'プロパティを取得してデシリアライズ
        EventBase? eventData;
        JsonElement eventJsonElement;
        if (!root.TryGetProperty("event", out var eventElement))
        {
            throw new JsonException("'event'プロパティが見つかりませんでした。");
        }

        eventJsonElement = eventElement.Clone();

        if (eventType == typeof(UnknownEvent))
        {
            eventData = new UnknownEvent { RawData = eventJsonElement };
        }
        else
        {
            eventData = (EventBase?)eventJsonElement.Deserialize(eventType, options);
        }

        if (eventData == null)
        {
            throw new JsonException("イベントのデシリアライズに失敗しました。");
        }

        return new WebhookCallbackNotification
        {
            Subscription = subscription,
            Event = eventData
        };
    }

    public override void Write(Utf8JsonWriter writer, WebhookCallbackNotification value, JsonSerializerOptions options)
    {
        // WebhookCallbackNotification型からのシリアル化を行うユースケースは想定されていないため、NotSupportedExceptionをスローします。
        throw new NotSupportedException("WebhookCallbackNotification のシリアル化はサポートされていません。");
    }
}
