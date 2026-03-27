using Microsoft.Azure.Cosmos;
using AdImpactOs.Campaign.Models;
using System.Net;

namespace AdImpactOs.Campaign.Services;

public class CampaignService
{
    private readonly Container _container;
    private readonly ILogger<CampaignService> _logger;

    public CampaignService(
        CosmosClient cosmosClient,
        ILogger<CampaignService> logger,
        IConfiguration configuration)
    {
        var databaseName = configuration["CosmosDb:DatabaseName"] ?? "AdTrackingDB";
        var containerName = configuration["CosmosDb:ContainerName"] ?? "Campaigns";

        _container = cosmosClient.GetContainer(databaseName, containerName);
        _logger = logger;
    }

    public virtual async Task<Models.Campaign> CreateCampaignAsync(CreateCampaignRequest request)
    {
        var campaign = new Models.Campaign
        {
            CampaignId = $"campaign_{Guid.NewGuid():N}",
            CampaignName = request.CampaignName,
            Advertiser = request.Advertiser,
            Industry = request.Industry,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Budget = request.Budget,
            TargetAudience = request.TargetAudience,
            Creatives = request.Creatives,
            Kpis = request.Kpis,
            Status = DetermineStatus(request.StartDate, request.EndDate),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        campaign.Id = campaign.CampaignId;

        var response = await _container.CreateItemAsync(campaign, new PartitionKey(campaign.CampaignId));
        _logger.LogInformation("Created campaign {CampaignId}: {CampaignName}", campaign.CampaignId, campaign.CampaignName);

        return response.Resource;
    }

    public virtual async Task<Models.Campaign?> GetCampaignAsync(string campaignId)
    {
        try
        {
            var response = await _container.ReadItemAsync<Models.Campaign>(campaignId, new PartitionKey(campaignId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<Models.Campaign>> GetAllCampaignsAsync(string? status = null, string? industry = null)
    {
        var queryText = "SELECT * FROM c";
        var conditions = new List<string>();

        if (!string.IsNullOrEmpty(status))
        {
            conditions.Add("c.status = @status");
        }

        if (!string.IsNullOrEmpty(industry))
        {
            conditions.Add("c.industry = @industry");
        }

        if (conditions.Any())
        {
            queryText += " WHERE " + string.Join(" AND ", conditions);
        }

        queryText += " ORDER BY c.createdAt DESC";

        var query = new QueryDefinition(queryText);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.WithParameter("@status", status);
        }

        if (!string.IsNullOrEmpty(industry))
        {
            query = query.WithParameter("@industry", industry);
        }

        var iterator = _container.GetItemQueryIterator<Models.Campaign>(query);
        var results = new List<Models.Campaign>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public virtual async Task<List<Models.Campaign>> GetActiveCampaignsAsync()
    {
        return await GetAllCampaignsAsync(status: "Active");
    }

    public virtual async Task<Models.Campaign> UpdateCampaignAsync(string campaignId, UpdateCampaignRequest request)
    {
        var campaign = await GetCampaignAsync(campaignId);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        if (!string.IsNullOrEmpty(request.CampaignName))
            campaign.CampaignName = request.CampaignName;

        if (request.Budget.HasValue)
            campaign.Budget = request.Budget.Value;

        if (request.EndDate.HasValue)
            campaign.EndDate = request.EndDate.Value;

        if (!string.IsNullOrEmpty(request.Status))
            campaign.Status = request.Status;

        if (request.TargetAudience != null)
            campaign.TargetAudience = request.TargetAudience;

        if (request.Creatives != null && request.Creatives.Any())
            campaign.Creatives = request.Creatives;

        if (request.Kpis != null)
            campaign.Kpis = request.Kpis;

        campaign.UpdatedAt = DateTime.UtcNow;

        var response = await _container.ReplaceItemAsync(campaign, campaign.Id, new PartitionKey(campaign.CampaignId));
        _logger.LogInformation("Updated campaign {CampaignId}", campaignId);

        return response.Resource;
    }

    public virtual async Task<Models.Campaign> UpdateCampaignStatusAsync(string campaignId, string status)
    {
        var campaign = await GetCampaignAsync(campaignId);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        campaign.Status = status;
        campaign.UpdatedAt = DateTime.UtcNow;

        var response = await _container.ReplaceItemAsync(campaign, campaign.Id, new PartitionKey(campaign.CampaignId));
        _logger.LogInformation("Updated campaign {CampaignId} status to {Status}", campaignId, status);

        return response.Resource;
    }

    public virtual async Task<Models.Campaign> UpdateCampaignMetricsAsync(string campaignId, UpdateCampaignMetricsRequest request)
    {
        var campaign = await GetCampaignAsync(campaignId);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        campaign.ActualMetrics = new ActualMetrics
        {
            Impressions = request.Impressions,
            Reach = request.Reach,
            AverageLift = request.AverageLift
        };

        campaign.UpdatedAt = DateTime.UtcNow;

        var response = await _container.ReplaceItemAsync(campaign, campaign.Id, new PartitionKey(campaign.CampaignId));
        _logger.LogInformation("Updated campaign {CampaignId} metrics", campaignId);

        return response.Resource;
    }

    public virtual async Task<bool> DeleteCampaignAsync(string campaignId)
    {
        try
        {
            await _container.DeleteItemAsync<Models.Campaign>(campaignId, new PartitionKey(campaignId));
            _logger.LogInformation("Deleted campaign {CampaignId}", campaignId);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private string DetermineStatus(DateTime startDate, DateTime endDate)
    {
        var now = DateTime.UtcNow;

        if (now < startDate)
            return "Scheduled";
        else if (now >= startDate && now <= endDate)
            return "Active";
        else
            return "Completed";
    }
}
