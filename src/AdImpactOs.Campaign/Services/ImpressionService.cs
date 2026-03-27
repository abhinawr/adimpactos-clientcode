using Microsoft.Azure.Cosmos;
using AdImpactOs.Campaign.Models;
using System.Net;

namespace AdImpactOs.Campaign.Services;

public class ImpressionService
{
    private readonly Container _container;
    private readonly ILogger<ImpressionService> _logger;

    public ImpressionService(
        CosmosClient cosmosClient,
        ILogger<ImpressionService> logger,
        IConfiguration configuration)
    {
        var databaseName = configuration["CosmosDb:DatabaseName"] ?? "AdImpactOsDB";
        var containerName = configuration["CosmosDb:ImpressionContainerName"] ?? "Impressions";

        _container = cosmosClient.GetContainer(databaseName, containerName);
        _logger = logger;
    }

    public virtual async Task<Impression> RecordImpressionAsync(Impression impression)
    {
        impression.Id = impression.ImpressionId;
        impression.CreatedAt = DateTime.UtcNow;

        var response = await _container.CreateItemAsync(impression, new PartitionKey(impression.CampaignId));
        _logger.LogInformation("Recorded impression {ImpressionId} for campaign {CampaignId}",
            impression.ImpressionId, impression.CampaignId);

        return response.Resource;
    }

    public virtual async Task<List<Impression>> GetImpressionsByCampaignAsync(string campaignId, int limit = 100)
    {
        var query = new QueryDefinition(
            "SELECT TOP @limit * FROM c WHERE c.campaignId = @campaignId ORDER BY c.timestampUtc DESC")
            .WithParameter("@campaignId", campaignId)
            .WithParameter("@limit", limit);

        var iterator = _container.GetItemQueryIterator<Impression>(query);
        var results = new List<Impression>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public virtual async Task<List<Impression>> GetAllImpressionsAsync(int limit = 500)
    {
        var query = new QueryDefinition(
            "SELECT TOP @limit * FROM c ORDER BY c.timestampUtc DESC")
            .WithParameter("@limit", limit);

        var iterator = _container.GetItemQueryIterator<Impression>(query);
        var results = new List<Impression>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public virtual async Task<ImpressionSummary> GetCampaignImpressionSummaryAsync(string campaignId)
    {
        var impressions = await GetImpressionsByCampaignAsync(campaignId, limit: 10000);

        var valid = impressions.Where(i => !i.IsBot).ToList();
        var bots = impressions.Where(i => i.IsBot).ToList();

        return new ImpressionSummary
        {
            CampaignId = campaignId,
            TotalImpressions = impressions.Count,
            ValidImpressions = valid.Count,
            BotImpressions = bots.Count,
            UniquePanelists = valid.Select(i => i.PanelistId).Distinct().Count(),
            ByCreative = valid.GroupBy(i => i.CreativeId)
                .Select(g => new CreativeImpressionCount { CreativeId = g.Key, Count = g.Count() })
                .OrderByDescending(c => c.Count)
                .ToList(),
            ByDevice = valid.GroupBy(i => i.DeviceType)
                .ToDictionary(g => g.Key, g => (long)g.Count()),
            ByCountry = valid.GroupBy(i => i.Country)
                .ToDictionary(g => g.Key, g => (long)g.Count()),
            BySource = impressions.GroupBy(i => i.IngestSource)
                .ToDictionary(g => g.Key, g => (long)g.Count()),
            ByHour = valid.GroupBy(i => i.TimestampUtc.ToString("yyyy-MM-dd HH:00"))
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => (long)g.Count())
        };
    }

    public virtual async Task<ExposedPanelistsResponse> GetExposedPanelistIdsAsync(string campaignId, int minImpressions = 1, int limit = 10000)
    {
        var impressions = await GetImpressionsByCampaignAsync(campaignId, limit: limit);

        var validImpressions = impressions.Where(i => !i.IsBot).ToList();

        var panelistCounts = validImpressions
            .GroupBy(i => i.PanelistId)
            .Where(g => g.Count() >= minImpressions)
            .Select(g => new ExposedPanelistResult
            {
                PanelistId = g.Key,
                ImpressionCount = g.Count()
            })
            .OrderByDescending(p => p.ImpressionCount)
            .ToList();

        return new ExposedPanelistsResponse
        {
            CampaignId = campaignId,
            MinImpressions = minImpressions,
            Panelists = panelistCounts,
            TotalExposedPanelists = panelistCounts.Count
        };
    }

    public virtual async Task<Dictionary<string, ImpressionSummary>> GetAllCampaignSummariesAsync()
    {
        var allImpressions = await GetAllImpressionsAsync(limit: 50000);

        var result = new Dictionary<string, ImpressionSummary>();

        foreach (var group in allImpressions.GroupBy(i => i.CampaignId))
        {
            var campaignId = group.Key;
            var impressions = group.ToList();
            var valid = impressions.Where(i => !i.IsBot).ToList();

            result[campaignId] = new ImpressionSummary
            {
                CampaignId = campaignId,
                TotalImpressions = impressions.Count,
                ValidImpressions = valid.Count,
                BotImpressions = impressions.Count(i => i.IsBot),
                UniquePanelists = valid.Select(i => i.PanelistId).Distinct().Count(),
                ByCreative = valid.GroupBy(i => i.CreativeId)
                    .Select(g => new CreativeImpressionCount { CreativeId = g.Key, Count = g.Count() })
                    .OrderByDescending(c => c.Count)
                    .ToList(),
                ByDevice = valid.GroupBy(i => i.DeviceType)
                    .ToDictionary(g => g.Key, g => (long)g.Count()),
                ByCountry = valid.GroupBy(i => i.Country)
                    .ToDictionary(g => g.Key, g => (long)g.Count()),
                BySource = impressions.GroupBy(i => i.IngestSource)
                    .ToDictionary(g => g.Key, g => (long)g.Count())
            };
        }

        return result;
    }
}
