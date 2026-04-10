using NotificationDiscordBot.Server.Interfaces.Services.Twitch;
using System.Security.Cryptography;
using System.Text;

namespace NotificationDiscordBot.Server.Services.Twitch;

public class TwitchVerifySignatureService : ITwitchVerifySignatureService
{
    private readonly ILogger<TwitchVerifySignatureService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _twitchSecret;
    private const string hmacPrefix = "sha256=";

    public TwitchVerifySignatureService(ILogger<TwitchVerifySignatureService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _twitchSecret = _configuration["Twitch:ClientSecret"] ?? throw new InvalidOperationException("TwitchのClientSecretが構成されていません。");
    }

    public bool VerifySignature(string messageId, string messageTimestamp, string requestBody, string signature)
    {
        var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_twitchSecret));
        var dataToHash = messageId + messageTimestamp + requestBody;
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
        var computedSignature = hmacPrefix + Convert.ToHexStringLower(computedHash);

        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedSignature),
            Encoding.UTF8.GetBytes(signature));

        return isValid;
    }
}
