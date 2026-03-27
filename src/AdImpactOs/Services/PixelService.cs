using System.Security.Cryptography;
using System.Text;

namespace AdImpactOs.Services;

/// <summary>
/// Service for generating pixel responses and tracking utilities.
/// </summary>
public static class PixelService
{
    /// <summary>
    /// Base64-encoded 1x1 transparent GIF (43 bytes when decoded).
    /// This minimal GIF ensures the browser terminates the request quickly.
    /// </summary>
    private const string TransparentGifBase64 = "R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==";

    /// <summary>
    /// MIME type for GIF responses.
    /// </summary>
    public const string GifContentType = "image/gif";

    /// <summary>
    /// Gets the byte array for the 1x1 transparent GIF.
    /// </summary>
    public static byte[] GetTransparentGif()
    {
        return Convert.FromBase64String(TransparentGifBase64);
    }

    /// <summary>
    /// Generates a tracking hash for deduplication.
    /// Combines campaign ID, creative ID, user ID, and timestamp.
    /// </summary>
    public static string GenerateTrackingHash(string? campaignId, string? creativeId, string? userId, DateTime timestamp)
    {
        var input = $"{campaignId}|{creativeId}|{userId}|{timestamp:O}";
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes).Substring(0, 16); // First 16 chars for brevity
        }
    }
}
