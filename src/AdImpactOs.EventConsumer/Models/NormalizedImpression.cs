namespace AdImpactOs.EventConsumer.Models;

public class NormalizedImpression
{
    public string ImpressionId { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public string CampaignId { get; set; } = string.Empty;
    public string CreativeId { get; set; } = string.Empty;
    public string PanelistId { get; set; } = string.Empty;
    public string DeviceType { get; set; } = "Unknown";
    public string Country { get; set; } = "Unknown";
    public bool IsBot { get; set; }
    public string IngestSource { get; set; } = string.Empty;
    public string? BotReason { get; set; }
}
