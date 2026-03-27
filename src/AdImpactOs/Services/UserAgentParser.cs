namespace AdImpactOs.Services;

/// <summary>
/// Service for parsing device type and browser from User-Agent string
/// </summary>
public static class UserAgentParser
{
    /// <summary>
    /// Parse device type from User-Agent string
    /// </summary>
    public static string ParseDeviceType(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown";

        var ua = userAgent.ToLowerInvariant();

        // Tablets - check before mobile since some tablets contain "mobile"
        if (ua.Contains("tablet") || ua.Contains("ipad"))
        {
            return "Tablet";
        }

        // Mobile devices
        if (ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone") || 
            ua.Contains("ipod") || ua.Contains("blackberry") || ua.Contains("windows phone"))
        {
            return "Mobile";
        }

        // Smart TV
        if (ua.Contains("smart-tv") || ua.Contains("smarttv") || ua.Contains("googletv"))
        {
            return "SmartTV";
        }

        // Desktop (default)
        return "Desktop";
    }

    /// <summary>
    /// Parse browser from User-Agent string
    /// </summary>
    public static string ParseBrowser(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown";

        var ua = userAgent.ToLowerInvariant();

        // Order matters - check more specific browsers first
        if (ua.Contains("edg/") || ua.Contains("edge"))
            return "Edge";
        
        if (ua.Contains("opr/") || ua.Contains("opera"))
            return "Opera";
        
        if (ua.Contains("chrome") && !ua.Contains("edg"))
            return "Chrome";
        
        if (ua.Contains("safari") && !ua.Contains("chrome"))
            return "Safari";
        
        if (ua.Contains("firefox"))
            return "Firefox";
        
        if (ua.Contains("msie") || ua.Contains("trident"))
            return "IE";

        return "Other";
    }

    /// <summary>
    /// Parse operating system from User-Agent string
    /// </summary>
    public static string ParseOS(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown";

        var ua = userAgent.ToLowerInvariant();

        // Check iOS before MacOS because iOS UA strings contain "mac os"
        if (ua.Contains("iphone") || ua.Contains("ipad") || ua.Contains("ipod"))
            return "iOS";
        
        if (ua.Contains("android"))
            return "Android";
        
        if (ua.Contains("windows nt"))
            return "Windows";
        
        if (ua.Contains("mac os") || ua.Contains("macos"))
            return "MacOS";
        
        if (ua.Contains("linux"))
            return "Linux";

        return "Other";
    }
}
