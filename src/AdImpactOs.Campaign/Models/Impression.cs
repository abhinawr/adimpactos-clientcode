using Newtonsoft.Json;

namespace AdImpactOs.Campaign.Models;

public class Impression
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("impressionId")]
    public string ImpressionId { get; set; } = string.Empty;

    [JsonProperty("campaignId")]
    public string CampaignId { get; set; } = string.Empty;

    [JsonProperty("creativeId")]
    public string CreativeId { get; set; } = string.Empty;

    [JsonProperty("panelistId")]
    public string PanelistId { get; set; } = string.Empty;

    [JsonProperty("deviceType")]
    public string DeviceType { get; set; } = "Unknown";

    [JsonProperty("country")]
    public string Country { get; set; } = "Unknown";

    [JsonProperty("isBot")]
    public bool IsBot { get; set; }

    [JsonProperty("botReason")]
    public string? BotReason { get; set; }

    [JsonProperty("ingestSource")]
    public string IngestSource { get; set; } = string.Empty;

    [JsonProperty("timestampUtc")]
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ImpressionSummary
{
    [JsonProperty("campaignId")]
    public string CampaignId { get; set; } = string.Empty;

    [JsonProperty("totalImpressions")]
    public long TotalImpressions { get; set; }

    [JsonProperty("validImpressions")]
    public long ValidImpressions { get; set; }

    [JsonProperty("botImpressions")]
    public long BotImpressions { get; set; }

    [JsonProperty("uniquePanelists")]
    public int UniquePanelists { get; set; }

    [JsonProperty("byCreative")]
    public List<CreativeImpressionCount> ByCreative { get; set; } = new();

    [JsonProperty("byDevice")]
    public Dictionary<string, long> ByDevice { get; set; } = new();

    [JsonProperty("byCountry")]
    public Dictionary<string, long> ByCountry { get; set; } = new();

    [JsonProperty("bySource")]
    public Dictionary<string, long> BySource { get; set; } = new();

    [JsonProperty("byHour")]
    public Dictionary<string, long> ByHour { get; set; } = new();
}

public class CreativeImpressionCount
{
    [JsonProperty("creativeId")]
    public string CreativeId { get; set; } = string.Empty;

    [JsonProperty("count")]
    public long Count { get; set; }
}

public class ExposedPanelistResult
{
    [JsonProperty("panelistId")]
    public string PanelistId { get; set; } = string.Empty;

    [JsonProperty("impressionCount")]
    public int ImpressionCount { get; set; }
}

public class ExposedPanelistsResponse
{
    [JsonProperty("campaignId")]
    public string CampaignId { get; set; } = string.Empty;

    [JsonProperty("minImpressions")]
    public int MinImpressions { get; set; }

    [JsonProperty("panelists")]
    public List<ExposedPanelistResult> Panelists { get; set; } = new();

    [JsonProperty("totalExposedPanelists")]
    public int TotalExposedPanelists { get; set; }
}
