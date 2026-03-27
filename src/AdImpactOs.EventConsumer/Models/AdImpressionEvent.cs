using Newtonsoft.Json;

namespace AdImpactOs.EventConsumer.Models;

public class AdImpressionEvent
{
    [JsonProperty("event_id")]
    public string EventId { get; set; } = string.Empty;

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonProperty("campaign_id")]
    public string CampaignId { get; set; } = string.Empty;

    [JsonProperty("creative_id")]
    public string CreativeId { get; set; } = string.Empty;

    [JsonProperty("panelist_token")]
    public string PanelistToken { get; set; } = string.Empty;

    [JsonProperty("user_agent")]
    public string UserAgent { get; set; } = string.Empty;

    [JsonProperty("ip")]
    public string IpAddress { get; set; } = string.Empty;

    [JsonProperty("referrer")]
    public string? Referrer { get; set; }

    [JsonProperty("ad_server")]
    public string? AdServer { get; set; }

    [JsonProperty("s2s_flag")]
    public bool IsS2S { get; set; }

    [JsonProperty("raw_headers")]
    public Dictionary<string, string>? RawHeaders { get; set; }

    [JsonProperty("tracking_hash")]
    public string? TrackingHash { get; set; }
}
