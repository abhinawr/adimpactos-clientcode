namespace AdImpactOs.EventConsumer.Services;

public class GeoEnrichmentService
{
    // Mock implementation - in production, use a real IP geolocation service
    private static readonly Dictionary<string, string> IpToCountry = new()
    {
        { "127.0.0.1", "US" },
        { "::1", "US" }
    };

    public string GetCountryFromIp(string ipAddress)
    {
        if (IpToCountry.TryGetValue(ipAddress, out var country))
        {
            return country;
        }

        // Mock logic based on IP range
        if (ipAddress.StartsWith("192.168.") || ipAddress.StartsWith("10."))
        {
            return "US";
        }

        // Default for unknown IPs
        return "Unknown";
    }

    public string GetDeviceType(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "Unknown";
        }

        var lowerUserAgent = userAgent.ToLower();

        if (lowerUserAgent.Contains("mobile") || lowerUserAgent.Contains("android") || lowerUserAgent.Contains("iphone"))
        {
            return "Mobile";
        }

        if (lowerUserAgent.Contains("tablet") || lowerUserAgent.Contains("ipad"))
        {
            return "Tablet";
        }

        return "Desktop";
    }
}
