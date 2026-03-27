using Newtonsoft.Json;

namespace AdImpactOs.Campaign.Models;

public class CreateCampaignRequest
{
    [JsonProperty("campaignName")]
    public string CampaignName { get; set; } = string.Empty;

    [JsonProperty("advertiser")]
    public string Advertiser { get; set; } = string.Empty;

    [JsonProperty("industry")]
    public string Industry { get; set; } = string.Empty;

    [JsonProperty("startDate")]
    public DateTime StartDate { get; set; }

    [JsonProperty("endDate")]
    public DateTime EndDate { get; set; }

    [JsonProperty("budget")]
    public decimal Budget { get; set; }

    [JsonProperty("targetAudience")]
    public TargetAudience? TargetAudience { get; set; }

    [JsonProperty("creatives")]
    public List<Creative> Creatives { get; set; } = new();

    [JsonProperty("kpis")]
    public CampaignKpis? Kpis { get; set; }
}

public class UpdateCampaignRequest
{
    [JsonProperty("campaignName")]
    public string? CampaignName { get; set; }

    [JsonProperty("budget")]
    public decimal? Budget { get; set; }

    [JsonProperty("endDate")]
    public DateTime? EndDate { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("targetAudience")]
    public TargetAudience? TargetAudience { get; set; }

    [JsonProperty("creatives")]
    public List<Creative>? Creatives { get; set; }

    [JsonProperty("kpis")]
    public CampaignKpis? Kpis { get; set; }
}

public class UpdateCampaignMetricsRequest
{
    [JsonProperty("impressions")]
    public long Impressions { get; set; }

    [JsonProperty("reach")]
    public long Reach { get; set; }

    [JsonProperty("averageLift")]
    public double AverageLift { get; set; }
}
