namespace NotificationDiscordBot.Server.Entity;

public static class Constants
{
    public static class TwitchWebhookController
    {
        public const string REQUEST_HEADER_MISSING = "Twitchからのリクエストに必要なヘッダーが不足しています。";
        public const string REQUEST_TIMESTAMP_INVALID = "Twitchからのリクエストのタイムスタンプが無効です。MessageTimestamp: {MessageTimestamp}";
        public const string REQUEST_TIMESTAMP_TOO_OLD = "Twitchからのリクエストのタイムスタンプが古すぎます。CurrentTime: {CurrentTime}, RequestTime: {RequestTime}";
        public const string REQUEST_SIGNATURE_INVALID = "Twitchからのリクエストの署名が無効です。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}";
        public const string VERIFICATION_FAILED = "Twitchからの検証リクエストの処理に失敗しました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}";
        public const string EVENT_NOTIFICATION_RECEIVED = "Twitchからのイベント通知を受信しました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}";
        public const string EVENT_NOTIFICATION_PROCESSING_STARTED = "Twitchからの通知の処理を開始します。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}";
        public const string EVENT_NOTIFICATION_PROCESSING_FAILED = "Twitchからの通知の処理に失敗しました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}";
        public const string EVENT_NOTIFICATION_PROCESSING_CANCELED = "Twitchからの通知の処理がキャンセルされました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}";
        public const string EVENT_NOTIFICATION_PROCESSING_THROWN_EXCEPTION = "Twitchからの通知の処理中に例外が発生しました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}";
        public const string EVENT_NOTIFICATION_PROCESSING_COMPLETED = "Twitchからの通知の処理が完了しました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}";
        public const string REVOCATION_RECEIVED = "Twitchからの購読取り消しを受信しました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}";
        public const string REVOCATION_PROCESSING_FAILED = "Twitchからの購読取り消しの処理に失敗しました。MessageId: {MessageId}, MessageTimestamp: {MessageTimestamp}";
        public const string REQUEST_UNKNOWN_MESSAGE_TYPE = "TwitchからのリクエストのMessageTypeが不明です。MessageType: {MessageType}";
    }
}
