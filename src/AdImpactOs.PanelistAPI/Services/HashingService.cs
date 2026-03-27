using System.Security.Cryptography;
using System.Text;

namespace AdImpactOs.PanelistAPI.Services;

/// <summary>
/// Service for hashing sensitive PII data
/// </summary>
public static class HashingService
{
    /// <summary>
    /// Hash email address using SHA256
    /// </summary>
    public static string HashEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;

        var normalized = email.Trim().ToLowerInvariant();
        return ComputeSha256Hash(normalized);
    }

    /// <summary>
    /// Hash phone number using SHA256
    /// </summary>
    public static string HashPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var normalized = NormalizePhoneNumber(phone);
        return ComputeSha256Hash(normalized);
    }

    /// <summary>
    /// Normalize phone number by removing all non-digit characters
    /// </summary>
    private static string NormalizePhoneNumber(string phone)
    {
        return new string(phone.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// Compute SHA256 hash of input string
    /// </summary>
    private static string ComputeSha256Hash(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Derive age range from exact age
    /// </summary>
    public static string GetAgeRange(int age)
    {
        if (age < 18) return "<18";
        if (age >= 18 && age <= 24) return "18-24";
        if (age >= 25 && age <= 34) return "25-34";
        if (age >= 35 && age <= 44) return "35-44";
        if (age >= 45 && age <= 54) return "45-54";
        if (age >= 55 && age <= 64) return "55-64";
        if (age >= 65) return "65+";
        return "Unknown";
    }
}
