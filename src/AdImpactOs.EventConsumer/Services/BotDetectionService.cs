using System.Text.RegularExpressions;

namespace AdImpactOs.EventConsumer.Services;

public class BotDetectionService
{
    private static readonly string[] BotUserAgents = new[]
    {
        "bot", "crawl", "spider", "slurp", "scraper", "headless",
        "phantom", "selenium", "webdriver", "puppeteer"
    };

    private static readonly Regex BotRegex = new(
        string.Join("|", BotUserAgents),
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Dictionary<string, (int count, DateTime firstSeen)> _ipRateLimit = new();
    private readonly int _maxRequestsPerMinute;

    public BotDetectionService(int maxRequestsPerMinute = 100)
    {
        _maxRequestsPerMinute = maxRequestsPerMinute;
    }

    public (bool isBot, string? reason) DetectBot(string userAgent, string ipAddress)
    {
        // Check user agent
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return (true, "Empty user agent");
        }

        if (BotRegex.IsMatch(userAgent))
        {
            return (true, "Bot pattern in user agent");
        }

        // Check rate limiting
        if (IsRateLimitExceeded(ipAddress))
        {
            return (true, "Rate limit exceeded");
        }

        return (false, null);
    }

    private bool IsRateLimitExceeded(string ipAddress)
    {
        lock (_ipRateLimit)
        {
            var now = DateTime.UtcNow;

            if (_ipRateLimit.TryGetValue(ipAddress, out var record))
            {
                if ((now - record.firstSeen).TotalMinutes < 1)
                {
                    _ipRateLimit[ipAddress] = (record.count + 1, record.firstSeen);
                    return record.count + 1 > _maxRequestsPerMinute;
                }
                else
                {
                    _ipRateLimit[ipAddress] = (1, now);
                    return false;
                }
            }
            else
            {
                _ipRateLimit[ipAddress] = (1, now);
                return false;
            }
        }
    }

    public void CleanupOldRecords()
    {
        lock (_ipRateLimit)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-5);
            var keysToRemove = _ipRateLimit
                .Where(kvp => kvp.Value.firstSeen < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _ipRateLimit.Remove(key);
            }
        }
    }
}
