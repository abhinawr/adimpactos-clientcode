using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace AdImpactOs.Survey.Services;

/// <summary>
/// Generates and validates HMAC-signed tokens for panelist survey links.
/// Token format: base64url( JSON payload + "." + HMAC-SHA256 signature )
/// </summary>
public class SurveyTokenService
{
    private readonly byte[] _key;
    private readonly ILogger<SurveyTokenService> _logger;

    public SurveyTokenService(IConfiguration configuration, ILogger<SurveyTokenService> logger)
    {
        var secret = configuration["SurveyToken:Secret"] ?? "AdImpactOs-Survey-Token-Secret-Key-2024!";
        _key = Encoding.UTF8.GetBytes(secret);
        _logger = logger;
    }

    public string GenerateToken(string surveyId, string panelistId, string cohortType, string responseId, int expiryHours = 168)
    {
        var payload = new SurveyTokenPayload
        {
            SurveyId = surveyId,
            PanelistId = panelistId,
            CohortType = cohortType,
            ResponseId = responseId,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(expiryHours).ToUnixTimeSeconds()
        };

        var payloadJson = JsonConvert.SerializeObject(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var payloadBase64 = Base64UrlEncode(payloadBytes);

        using var hmac = new HMACSHA256(_key);
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64));
        var signatureBase64 = Base64UrlEncode(signatureBytes);

        return $"{payloadBase64}.{signatureBase64}";
    }

    public SurveyTokenPayload? ValidateToken(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 2)
            {
                _logger.LogWarning("Invalid token format: expected 2 parts, got {Count}", parts.Length);
                return null;
            }

            var payloadBase64 = parts[0];
            var signatureBase64 = parts[1];

            using var hmac = new HMACSHA256(_key);
            var expectedSignature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64)));

            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(signatureBase64)))
            {
                _logger.LogWarning("Invalid token signature");
                return null;
            }

            var payloadBytes = Base64UrlDecode(payloadBase64);
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);
            var payload = JsonConvert.DeserializeObject<SurveyTokenPayload>(payloadJson);

            if (payload == null)
            {
                _logger.LogWarning("Failed to deserialize token payload");
                return null;
            }

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > payload.ExpiresAt)
            {
                _logger.LogWarning("Token expired for survey {SurveyId}, panelist {PanelistId}", payload.SurveyId, payload.PanelistId);
                return null;
            }

            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating survey token");
            return null;
        }
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}

public class SurveyTokenPayload
{
    [JsonProperty("sid")]
    public string SurveyId { get; set; } = string.Empty;

    [JsonProperty("pid")]
    public string PanelistId { get; set; } = string.Empty;

    [JsonProperty("coh")]
    public string CohortType { get; set; } = "exposed";

    [JsonProperty("rid")]
    public string ResponseId { get; set; } = string.Empty;

    [JsonProperty("exp")]
    public long ExpiresAt { get; set; }
}
