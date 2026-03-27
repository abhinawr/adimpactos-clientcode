using Newtonsoft.Json;

namespace AdImpactOs.Models;

/// <summary>
/// Represents the JSON body for server-to-server tracking requests.
/// </summary>
public class S2STrackingRequest
{
    /// <summary>
    /// Campaign ID (required)
    /// </summary>
    [JsonProperty("campaign_id")]
    public string? CampaignId { get; set; }

    /// <summary>
    /// Creative ID (required)
    /// </summary>
    [JsonProperty("creative_id")]
    public string? CreativeId { get; set; }

    /// <summary>
    /// User ID/Panelist Token (required)
    /// </summary>
    [JsonProperty("panelist_token")]
    public string? PanelistToken { get; set; }

    /// <summary>
    /// Ad server identifier (optional)
    /// </summary>
    [JsonProperty("ad_server")]
    public string? AdServer { get; set; }

    /// <summary>
    /// User agent string from the original request (optional)
    /// </summary>
    [JsonProperty("user_agent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// IP address of the end user (optional)
    /// </summary>
    [JsonProperty("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Referrer URL (optional)
    /// </summary>
    [JsonProperty("referrer")]
    public string? Referrer { get; set; }

    /// <summary>
    /// Custom data dictionary (optional)
    /// </summary>
    [JsonProperty("custom_data")]
    public Dictionary<string, object>? CustomData { get; set; }

    /// <summary>
    /// Optional idempotency key for request deduplication
    /// </summary>
    [JsonProperty("idempotency_key")]
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Optional timestamp override (defaults to server time if not provided)
    /// </summary>
    [JsonProperty("timestamp")]
    public DateTime? Timestamp { get; set; }
}
