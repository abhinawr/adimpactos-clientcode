using Newtonsoft.Json;

namespace AdImpactOs.Campaign.Models;

public class Campaign
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("campaignId")]
    public string CampaignId { get; set; } = string.Empty;

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

    [JsonProperty("status")]
    public string Status { get; set; } = "Draft";

    [JsonProperty("targetAudience")]
    public TargetAudience? TargetAudience { get; set; }

    [JsonProperty("creatives")]
    public List<Creative> Creatives { get; set; } = new();

    [JsonProperty("kpis")]
    public CampaignKpis? Kpis { get; set; }

    [JsonProperty("actualMetrics")]
    public ActualMetrics? ActualMetrics { get; set; }

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class TargetAudience
{
    [JsonProperty("ageRange")]
    public List<string> AgeRange { get; set; } = new();

    [JsonProperty("gender")]
    public List<string> Gender { get; set; } = new();

    [JsonProperty("interests")]
    public List<string> Interests { get; set; } = new();

    [JsonProperty("countries")]
    public List<string> Countries { get; set; } = new();
}

public class Creative
{
    [JsonProperty("creativeId")]
    public string CreativeId { get; set; } = string.Empty;

    [JsonProperty("format")]
    public string Format { get; set; } = string.Empty;

    [JsonProperty("size")]
    public string Size { get; set; } = string.Empty;

    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("variantName")]
    public string VariantName { get; set; } = string.Empty;
}

public class CampaignKpis
{
    [JsonProperty("targetImpressions")]
    public long TargetImpressions { get; set; }

    [JsonProperty("targetReach")]
    public long TargetReach { get; set; }

    [JsonProperty("targetLift")]
    public double TargetLift { get; set; }
}

public class ActualMetrics
{
    [JsonProperty("impressions")]
    public long Impressions { get; set; }

    [JsonProperty("reach")]
    public long Reach { get; set; }

    [JsonProperty("averageLift")]
    public double AverageLift { get; set; }
}

public enum CampaignStatus
{
    Draft,
    Scheduled,
    Active,
    Paused,
    Completed,
    Archived
}
