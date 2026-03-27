using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace AdImpactOs.Models;

/// <summary>
/// Represents the tracking metadata sent to the Event Hub for ad impressions.
/// This POCO is bound to EventHubOutput for high-throughput event ingestion.
/// </summary>
public class TrackingResponse
{
    /// <summary>
    /// Unique identifier for this event
    /// </summary>
    [JsonProperty("event_id")]
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp of when the impression was tracked (UTC).
    /// </summary>
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Campaign ID extracted from the 'cid' query parameter.
    /// </summary>
    [JsonProperty("campaign_id")]
    public string? CampaignId { get; set; }

    /// <summary>
    /// Creative ID extracted from the 'crid' query parameter.
    /// </summary>
    [JsonProperty("creative_id")]
    public string? CreativeId { get; set; }

    /// <summary>
    /// Panelist token extracted from the 'uid' query parameter.
    /// </summary>
    [JsonProperty("panelist_token")]
    public string? PanelistToken { get; set; }

    /// <summary>
    /// User agent string from the HTTP request header.
    /// </summary>
    [JsonProperty("user_agent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Device type parsed from User-Agent (Desktop/Mobile/Tablet)
    /// </summary>
    [JsonProperty("device_type")]
    public string? DeviceType { get; set; }

    /// <summary>
    /// Client's remote IP address.
    /// </summary>
    [JsonProperty("ip")]
    public string? Ip { get; set; }

    /// <summary>
    /// Referrer URL from HTTP header.
    /// </summary>
    [JsonProperty("referrer")]
    public string? Referrer { get; set; }

    /// <summary>
    /// Ad server identifier (optional, can be extracted from query params or headers).
    /// </summary>
    [JsonProperty("ad_server")]
    public string? AdServer { get; set; }

    /// <summary>
    /// Server-to-server flag indicating if request came from server vs browser.
    /// </summary>
    [JsonProperty("s2s_flag")]
    public bool S2SFlag { get; set; }

    /// <summary>
    /// Raw HTTP headers serialized as JSON for debugging/audit purposes.
    /// </summary>
    [JsonProperty("raw_headers")]
    public Dictionary<string, string>? RawHeaders { get; set; }

    /// <summary>
    /// Hash of the tracking event for deduplication.
    /// </summary>
    [JsonProperty("tracking_hash")]
    public string? TrackingHash { get; set; }
}
