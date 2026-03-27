using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AdImpactOs.Models;
using AdImpactOs.Services;
using System.Net;
using Newtonsoft.Json;

namespace AdImpactOs.Functions.S2S;

/// <summary>
/// Server-to-Server (S2S) Ingest API for ad impression tracking.
/// Accepts POST requests with JSON body and normalizes to the same event schema as pixel tracker.
/// </summary>
public class S2STracker
{
    private readonly ILogger _logger;
    private readonly LocalImpressionForwarder? _localForwarder;
    private static readonly HashSet<string> _processedIdempotencyKeys = new HashSet<string>();
    private static readonly object _idempotencyLock = new object();
    private const int MaxIdempotencyKeysCache = 10000;

    public S2STracker(ILoggerFactory loggerFactory, LocalImpressionForwarder? localForwarder = null)
    {
        _logger = loggerFactory.CreateLogger<S2STracker>();
        _localForwarder = localForwarder;
    }

    /// <summary>
    /// S2S Tracker HTTP-triggered function that records ad impressions from server-to-server calls.
    /// 
    /// POST Body (JSON):
    /// {
    ///   "campaign_id": "string (required)",
    ///   "creative_id": "string (required)",
    ///   "panelist_token": "string (required)",
    ///   "ad_server": "string (optional)",
    ///   "idempotency_key": "string (optional)",
    ///   "timestamp": "ISO8601 datetime (optional)"
    /// }
    /// 
    /// Headers:
    /// - User-Agent: Forwarded user agent (optional)
    /// - X-Forwarded-For: Client IP address (optional)
    /// 
    /// Returns: HTTP 200 with JSON response on success, HTTP 400 on validation error.
    /// Side Effect: Sends tracking metadata to 'ad-impressions' Event Hub.
    /// </summary>
    [Function("S2STracker")]
    public async Task<S2STrackerOutput> Track(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "s2s/track")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("S2STracker function invoked.");

            // Read and parse JSON body
            string requestBody;
            using (var reader = new StreamReader(req.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogWarning("Empty request body received.");
                return CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is empty");
            }

            S2STrackingRequest? trackingRequest;
            try
            {
                trackingRequest = JsonConvert.DeserializeObject<S2STrackingRequest>(requestBody);
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse request body: {ErrorMessage}", ex.Message);
                return CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid JSON format");
            }

            if (trackingRequest == null)
            {
                _logger.LogWarning("Failed to deserialize request body.");
                return CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format");
            }

            // Validate required fields
            var validationErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(trackingRequest.CampaignId))
                validationErrors.Add("campaign_id is required");
            if (string.IsNullOrWhiteSpace(trackingRequest.CreativeId))
                validationErrors.Add("creative_id is required");
            if (string.IsNullOrWhiteSpace(trackingRequest.PanelistToken))
                validationErrors.Add("panelist_token is required");

            if (validationErrors.Any())
            {
                _logger.LogWarning("Validation failed: {Errors}", string.Join(", ", validationErrors));
                return CreateErrorResponse(req, HttpStatusCode.BadRequest, 
                    $"Validation failed: {string.Join(", ", validationErrors)}");
            }

            // Check idempotency
            if (!string.IsNullOrWhiteSpace(trackingRequest.IdempotencyKey))
            {
                bool isDuplicate = false;
                lock (_idempotencyLock)
                {
                    if (_processedIdempotencyKeys.Contains(trackingRequest.IdempotencyKey))
                    {
                        isDuplicate = true;
                    }
                    else
                    {
                        _processedIdempotencyKeys.Add(trackingRequest.IdempotencyKey);
                        
                        // Limit cache size to prevent memory issues
                        if (_processedIdempotencyKeys.Count > MaxIdempotencyKeysCache)
                        {
                            _processedIdempotencyKeys.Clear();
                            _logger.LogInformation("Idempotency cache cleared after reaching {MaxSize} entries", MaxIdempotencyKeysCache);
                        }
                    }
                }

                if (isDuplicate)
                {
                    _logger.LogInformation("Duplicate request detected with idempotency_key: {IdempotencyKey}", 
                        trackingRequest.IdempotencyKey);
                    return CreateSuccessResponse(req, "Request already processed (idempotent)", processedBefore: true);
                }
            }

            _logger.LogInformation("Processing S2S tracking request: campaign_id={CampaignId}, creative_id={CreativeId}, panelist_token={PanelistToken}", 
                trackingRequest.CampaignId, trackingRequest.CreativeId, trackingRequest.PanelistToken);

            // Extract headers
            var userAgent = req.Headers.TryGetValues("User-Agent", out var userAgentValues)
                ? userAgentValues.FirstOrDefault()
                : "Unknown";

            var referrer = req.Headers.TryGetValues("Referer", out var referrerValues)
                ? referrerValues.FirstOrDefault()
                : null;

            var remoteIpAddress = ExtractRemoteIpAddress(req);

            // Capture raw headers for audit
            var rawHeaders = CaptureRawHeaders(req);

            // Create tracking response with normalized schema
            var timestamp = trackingRequest.Timestamp ?? DateTime.UtcNow;
            var trackingResponse = new TrackingResponse
            {
                CampaignId = trackingRequest.CampaignId,
                CreativeId = trackingRequest.CreativeId,
                PanelistToken = trackingRequest.PanelistToken,
                UserAgent = userAgent,
                DeviceType = UserAgentParser.ParseDeviceType(userAgent),
                Ip = remoteIpAddress,
                Referrer = referrer,
                AdServer = trackingRequest.AdServer,
                S2SFlag = true,
                RawHeaders = rawHeaders,
                Timestamp = timestamp
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

            // Return success response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            
            var responseBody = new
            {
                success = true,
                message = "Tracking event recorded successfully",
                event_id = trackingResponse.EventId,
                tracking_hash = trackingResponse.TrackingHash
            };

            await response.WriteStringAsync(JsonConvert.SerializeObject(responseBody));
            _logger.LogInformation("S2S tracking event successfully processed.");

            return new S2STrackerOutput
            {
                HttpResponse = response,
                EventData = trackingResponse
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in S2STracker: {ErrorMessage}", ex.Message);
            return CreateErrorResponse(req, HttpStatusCode.InternalServerError, 
                "An error occurred while processing the tracking request");
        }
    }

    /// <summary>
    /// Extracts the client's remote IP address from the HTTP request.
    /// Checks for X-Forwarded-For header (when behind a proxy) and falls back to RemoteEndPoint.
    /// </summary>
    private string ExtractRemoteIpAddress(HttpRequestData req)
    {
        if (req.Headers.TryGetValues("X-Forwarded-For", out var forwardedFor))
        {
            var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(ip))
                return ip;
        }

        if (req.Headers.TryGetValues("X-Real-IP", out var realIp))
        {
            var ip = realIp.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(ip))
                return ip;
        }

        return "Unknown";
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

    /// <summary>
    /// Creates an error response with specified status code and message.
    /// </summary>
    private S2STrackerOutput CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        
        var errorBody = new
        {
            success = false,
            error = message
        };

        response.WriteString(JsonConvert.SerializeObject(errorBody));

        return new S2STrackerOutput
        {
            HttpResponse = response,
            EventData = null
        };
    }

    /// <summary>
    /// Creates a success response.
    /// </summary>
    private S2STrackerOutput CreateSuccessResponse(HttpRequestData req, string message, bool processedBefore = false)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        
        var responseBody = new
        {
            success = true,
            message = message,
            processed_before = processedBefore
        };

        response.WriteString(JsonConvert.SerializeObject(responseBody));

        return new S2STrackerOutput
        {
            HttpResponse = response,
            EventData = null
        };
    }
}
