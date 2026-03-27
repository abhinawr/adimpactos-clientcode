using System.Collections.Concurrent;

namespace AdImpactOs.Services;

/// <summary>
/// In-memory rate limiter for Azure Functions
/// Uses sliding window algorithm
/// </summary>
public class RateLimiterService
{
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _requestLog = new();
    private readonly int _maxRequestsPerWindow;
    private readonly TimeSpan _windowSize;

    public RateLimiterService(int maxRequestsPerWindow = 1000, TimeSpan? windowSize = null)
    {
        _maxRequestsPerWindow = maxRequestsPerWindow;
        _windowSize = windowSize ?? TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Checks if a request is allowed based on rate limiting rules
    /// </summary>
    /// <param name="clientKey">Unique identifier for the client (IP, user ID, etc.)</param>
    /// <returns>True if request is allowed, false if rate limit exceeded</returns>
    public bool IsAllowed(string clientKey)
    {
        var now = DateTime.UtcNow;
        var cutoff = now - _windowSize;

        var requests = _requestLog.GetOrAdd(clientKey, _ => new Queue<DateTime>());

        lock (requests)
        {
            // Remove old requests outside the window
            while (requests.Count > 0 && requests.Peek() < cutoff)
            {
                requests.Dequeue();
            }

            // Check if limit exceeded
            if (requests.Count >= _maxRequestsPerWindow)
            {
                return false;
            }

            // Add current request
            requests.Enqueue(now);
            return true;
        }
    }

    /// <summary>
    /// Gets the remaining requests for a client
    /// </summary>
    public int GetRemainingRequests(string clientKey)
    {
        var now = DateTime.UtcNow;
        var cutoff = now - _windowSize;

        if (!_requestLog.TryGetValue(clientKey, out var requests))
        {
            return _maxRequestsPerWindow;
        }

        lock (requests)
        {
            // Remove old requests
            while (requests.Count > 0 && requests.Peek() < cutoff)
            {
                requests.Dequeue();
            }

            return Math.Max(0, _maxRequestsPerWindow - requests.Count);
        }
    }

    /// <summary>
    /// Gets the time until the rate limit resets
    /// </summary>
    public TimeSpan GetResetTime(string clientKey)
    {
        var now = DateTime.UtcNow;

        if (!_requestLog.TryGetValue(clientKey, out var requests))
        {
            return TimeSpan.Zero;
        }

        lock (requests)
        {
            if (requests.Count == 0)
            {
                return TimeSpan.Zero;
            }

            var oldestRequest = requests.Peek();
            var resetTime = oldestRequest + _windowSize;
            return resetTime > now ? resetTime - now : TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Clears old entries to prevent memory leaks
    /// Should be called periodically
    /// </summary>
    public void CleanupOldEntries()
    {
        var now = DateTime.UtcNow;
        var cutoff = now - (_windowSize * 2); // Keep some buffer

        var keysToRemove = new List<string>();

        foreach (var kvp in _requestLog)
        {
            lock (kvp.Value)
            {
                if (kvp.Value.Count == 0 || kvp.Value.All(t => t < cutoff))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
        }

        foreach (var key in keysToRemove)
        {
            _requestLog.TryRemove(key, out _);
        }
    }
}
