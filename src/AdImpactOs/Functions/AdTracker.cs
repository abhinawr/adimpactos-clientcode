using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AdImpactOs.Models;
using AdImpactOs.Services;
using System.Net;

namespace AdImpactOs.Functions.Pixel;

/// <summary>
/// High-concurrency Pixel Tracker for Kantar-style ad measurement.
/// Tracks campaign impressions with user, campaign, and creative metadata.
/// </summary>
public class AdTracker
{
    private readonly ILogger _logger;
    private readonly LocalImpressionForwarder? _localForwarder;

    public AdTracker(ILoggerFactory loggerFactory, LocalImpressionForwarder? localForwarder = null)
    {
        _logger = loggerFactory.CreateLogger<AdTracker>();
        _localForwarder = localForwarder;
    }

    /// <summary>
    /// PixelTracker HTTP-triggered function that records ad impressions.
    /// 
    /// Query Parameters:
    /// - cid: Campaign ID (required)
    /// - crid: Creative ID (required)
    /// - uid: User ID/Panelist Token (required)
    /// - adserver: Ad server identifier (optional)
    /// 
    /// Returns: 1x1 transparent GIF to terminate browser request quickly.
    /// Side Effect: Sends tracking metadata to 'ad-impressions' Event Hub.
    /// 
    /// Route: GET /api/pixel
    /// </summary>
    [Function("PixelTracker")]
    public async Task<PixelTrackerOutput> PixelTracker(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "pixel")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("PixelTracker function invoked.");

            // Extract and validate query parameters
            var cid = req.Query["cid"];
            var crid = req.Query["crid"];
            var uid = req.Query["uid"];
            var adServer = req.Query["adserver"];

            // Validation: Check required parameters
            var validationErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(cid))
                validationErrors.Add("Missing required parameter: cid (Campaign ID)");
            if (string.IsNullOrWhiteSpace(crid))
                validationErrors.Add("Missing required parameter: crid (Creative ID)");
            if (string.IsNullOrWhiteSpace(uid))
                validationErrors.Add("Missing required parameter: uid (User ID/Panelist Token)");

            if (validationErrors.Any())
            {
                _logger.LogWarning("Validation failed: {Errors}", string.Join(", ", validationErrors));
                // Still return GIF but don't publish event
                var errorResponse = req.CreateResponse(HttpStatusCode.OK);
                errorResponse.Headers.Add("Content-Type", PixelService.GifContentType);
                errorResponse.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                errorResponse.Headers.Add("Pragma", "no-cache");
                errorResponse.Headers.Add("Expires", "0");
                await errorResponse.WriteBytesAsync(PixelService.GetTransparentGif());
                
                return new PixelTrackerOutput
                {
                    HttpResponse = errorResponse,
                    EventData = null
                };
            }

            _logger.LogInformation("Received tracking request: cid={CampaignId}, crid={CreativeId}, uid={UserId}", cid, crid, uid);

            // Extract headers
            var userAgent = req.Headers.TryGetValues("User-Agent", out var userAgentValues)
                ? userAgentValues.FirstOrDefault()
                : "Unknown";

            var referrer = req.Headers.TryGetValues("Referer", out var referrerValues)
                ? referrerValues.FirstOrDefault()
                : null;

            var remoteIpAddress = ExtractRemoteIpAddress(req);

            // Determine if request is server-to-server
            var s2sFlag = DetermineS2SFlag(req);

            // Capture raw headers for audit
            var rawHeaders = CaptureRawHeaders(req);

            // Create tracking response
            var trackingResponse = new TrackingResponse
            {
                CampaignId = cid,
                CreativeId = crid,
                PanelistToken = uid,
                UserAgent = userAgent,
                DeviceType = UserAgentParser.ParseDeviceType(userAgent),
                Ip = remoteIpAddress,
                Referrer = referrer,
                AdServer = adServer,
                S2SFlag = s2sFlag,
                RawHeaders = rawHeaders,
                Timestamp = DateTime.UtcNow
            };

            // Generate tracking hash for deduplication
            trackingResponse.TrackingHash = PixelService.GenerateTrackingHash(
                trackingResponse.CampaignId,
                trackingResponse.CreativeId,
                trackingResponse.PanelistToken,
                trackingResponse.Timestamp);

            _logger.LogInformation("Generated tracking event with event_id={EventId}, tracking_hash={TrackingHash}", 
                trackingResponse.EventId, trackingResponse.TrackingHash);

            // Forward directly to Campaign API when running locally (no Event Hub)
            if (_localForwarder != null)
            {
                await _localForwarder.ForwardAsync(trackingResponse);
            }

            // EventHub output will be returned as function output tuple
            _logger.LogInformation("Tracking event prepared for Event Hub output.");

            // Return 1x1 transparent GIF
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", PixelService.GifContentType);
            response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            response.Headers.Add("Pragma", "no-cache");
            response.Headers.Add("Expires", "0");

            await response.WriteBytesAsync(PixelService.GetTransparentGif());
            _logger.LogInformation("GIF response returned to client.");

            return new PixelTrackerOutput
            {
                HttpResponse = response,
                EventData = trackingResponse
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PixelTracker: {ErrorMessage}", ex.Message);
            // Return GIF even on error to maintain tracking continuity
            var errorResponse = req.CreateResponse(HttpStatusCode.OK);
            errorResponse.Headers.Add("Content-Type", PixelService.GifContentType);
            await errorResponse.WriteBytesAsync(PixelService.GetTransparentGif());
            return new PixelTrackerOutput
            {
                HttpResponse = errorResponse,
                EventData = null
            };
        }
    }

    /// <summary>
    /// Extracts the client's remote IP address from the HTTP request.
    /// Checks for X-Forwarded-For header (when behind a proxy) and falls back to RemoteEndPoint.
    /// </summary>
    private string ExtractRemoteIpAddress(HttpRequestData req)
    {
        // Check for X-Forwarded-For header (common when behind Azure API Management or similar)
        if (req.Headers.TryGetValues("X-Forwarded-For", out var forwardedFor))
        {
            var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(ip))
                return ip;
        }

        // Check for X-Real-IP header
        if (req.Headers.TryGetValues("X-Real-IP", out var realIp))
        {
            var ip = realIp.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(ip))
                return ip;
        }

        // Return unknown if no proxy headers found
        return "Unknown";
    }

    /// <summary>
    /// Determines if the request is server-to-server based on headers.
    /// S2S requests typically lack browser-specific headers or have specific markers.
    /// </summary>
    private bool DetermineS2SFlag(HttpRequestData req)
    {
        // Check for common S2S indicators
        if (req.Headers.TryGetValues("X-S2S", out var s2sHeader))
        {
            return s2sHeader.FirstOrDefault()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        // If User-Agent is missing or contains "server", "bot", or similar, it might be S2S
        if (req.Headers.TryGetValues("User-Agent", out var userAgentValues))
        {
            var userAgent = userAgentValues.FirstOrDefault()?.ToLowerInvariant() ?? "";
            if (userAgent.Contains("server") || userAgent.Contains("bot") || userAgent.Contains("curl") || 
                userAgent.Contains("wget") || string.IsNullOrWhiteSpace(userAgent))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Captures raw HTTP headers for debugging and audit purposes.
    /// </summary>
    private Dictionary<string, string> CaptureRawHeaders(HttpRequestData req)
    {
        var headers = new Dictionary<string, string>();
        
        foreach (var header in req.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        return headers;
    }
}
