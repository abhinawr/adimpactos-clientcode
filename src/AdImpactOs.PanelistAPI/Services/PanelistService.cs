using Microsoft.Azure.Cosmos;
using AdImpactOs.PanelistAPI.Models;

namespace AdImpactOs.PanelistAPI.Services;

/// <summary>
/// Service for managing panelist profiles in Cosmos DB
/// </summary>
public class PanelistService
{
    private readonly Container _container;
    private readonly ILogger<PanelistService> _logger;

    public PanelistService(CosmosClient cosmosClient, ILogger<PanelistService> logger, IConfiguration configuration)
    {
        _logger = logger;
        var databaseName = configuration["CosmosDb:DatabaseName"] ?? "AdImpactOsDB";
        var containerName = configuration["CosmosDb:ContainerName"] ?? "Panelists";
        
        _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    /// <summary>
    /// Create a new panelist profile
    /// </summary>
    public virtual async Task<Panelist> CreatePanelistAsync(CreatePanelistRequest request)
    {
        var panelist = new Panelist
        {
            Id = Guid.NewGuid().ToString(),
            PanelistId = Guid.NewGuid().ToString(),
            Email = request.Email,
            HashedEmail = HashingService.HashEmail(request.Email),
            HashedPhone = !string.IsNullOrWhiteSpace(request.Phone) ? HashingService.HashPhone(request.Phone) : null,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Age = request.Age,
            AgeRange = request.AgeRange ?? (request.Age.HasValue ? HashingService.GetAgeRange(request.Age.Value) : null),
            Gender = request.Gender,
            HhIncomeBucket = request.HhIncomeBucket,
            Interests = request.Interests,
            Country = request.Country,
            PostalCode = request.PostalCode,
            DeviceType = request.DeviceType,
            Browser = request.Browser,
            ConsentGdpr = request.ConsentGdpr,
            ConsentCcpa = request.ConsentCcpa,
            ConsentGiven = request.ConsentGiven || request.ConsentGdpr || request.ConsentCcpa,
            ConsentTimestamp = (request.ConsentGiven || request.ConsentGdpr || request.ConsentCcpa) ? DateTime.UtcNow : null,
            LastActive = DateTime.UtcNow,
            PointsBalance = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            var response = await _container.CreateItemAsync(panelist, new PartitionKey(panelist.Id));
            _logger.LogInformation("Created panelist with ID: {PanelistId}", panelist.Id);
            return response.Resource;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Failed to create panelist");
            throw;
        }
    }

    /// <summary>
    /// Get panelist by pseudonymized ID
    /// </summary>
    public virtual async Task<Panelist?> GetPanelistByIdAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<Panelist>(id, new PartitionKey(id));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Panelist not found: {PanelistId}", id);
            return null;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Failed to get panelist: {PanelistId}", id);
            throw;
        }
    }

    /// <summary>
    /// Update panelist profile - enforces consent check
    /// </summary>
    public virtual async Task<Panelist?> UpdatePanelistAsync(string id, UpdatePanelistRequest request)
    {
        try
        {
            // Get existing panelist
            var existingPanelist = await GetPanelistByIdAsync(id);
            if (existingPanelist == null)
            {
                return null;
            }

            // Update email and hash if changed
            if (request.Email != null && request.Email != existingPanelist.Email)
            {
                existingPanelist.Email = request.Email;
                existingPanelist.HashedEmail = HashingService.HashEmail(request.Email);
            }

            // Update phone and hash if changed
            if (request.Phone != null)
            {
                existingPanelist.HashedPhone = HashingService.HashPhone(request.Phone);
            }

            // Enforce consent flags
            if (request.ConsentGdpr.HasValue)
            {
                existingPanelist.ConsentGdpr = request.ConsentGdpr.Value;
                if (request.ConsentGdpr.Value)
                {
                    existingPanelist.ConsentTimestamp = DateTime.UtcNow;
                }
            }

            if (request.ConsentCcpa.HasValue)
            {
                existingPanelist.ConsentCcpa = request.ConsentCcpa.Value;
                if (request.ConsentCcpa.Value)
                {
                    existingPanelist.ConsentTimestamp = DateTime.UtcNow;
                }
            }

            if (request.ConsentGiven.HasValue)
            {
                existingPanelist.ConsentGiven = request.ConsentGiven.Value;
                if (request.ConsentGiven.Value)
                {
                    existingPanelist.ConsentTimestamp = DateTime.UtcNow;
                }
            }

            // Update other fields
            if (request.FirstName != null) existingPanelist.FirstName = request.FirstName;
            if (request.LastName != null) existingPanelist.LastName = request.LastName;
            
            if (request.Age.HasValue)
            {
                existingPanelist.Age = request.Age;
                existingPanelist.AgeRange = HashingService.GetAgeRange(request.Age.Value);
            }
            
            if (request.AgeRange != null) existingPanelist.AgeRange = request.AgeRange;
            if (request.Gender != null) existingPanelist.Gender = request.Gender;
            if (request.HhIncomeBucket != null) existingPanelist.HhIncomeBucket = request.HhIncomeBucket;
            if (request.Interests != null) existingPanelist.Interests = request.Interests;
            if (request.Country != null) existingPanelist.Country = request.Country;
            if (request.PostalCode != null) existingPanelist.PostalCode = request.PostalCode;
            if (request.DeviceType != null) existingPanelist.DeviceType = request.DeviceType;
            if (request.Browser != null) existingPanelist.Browser = request.Browser;
            if (request.IsActive.HasValue) existingPanelist.IsActive = request.IsActive.Value;
            if (request.PointsBalance.HasValue) existingPanelist.PointsBalance = request.PointsBalance.Value;

            // Update last active and updated timestamps
            existingPanelist.LastActive = DateTime.UtcNow;
            existingPanelist.UpdatedAt = DateTime.UtcNow;

            var response = await _container.ReplaceItemAsync(existingPanelist, id, new PartitionKey(id));
            _logger.LogInformation("Updated panelist: {PanelistId}", id);
            return response.Resource;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Failed to update panelist: {PanelistId}", id);
            throw;
        }
    }

    /// <summary>
    /// Check if panelist has given consent
    /// </summary>
    public virtual async Task<bool> CheckConsentAsync(string id)
    {
        var panelist = await GetPanelistByIdAsync(id);
        return panelist?.ConsentGiven ?? false;
    }

    /// <summary>
    /// Delete panelist (soft delete by setting IsActive = false)
    /// </summary>
    public virtual async Task<bool> DeletePanelistAsync(string id)
    {
        try
        {
            var panelist = await GetPanelistByIdAsync(id);
            if (panelist == null)
            {
                return false;
            }

            panelist.IsActive = false;
            panelist.UpdatedAt = DateTime.UtcNow;

            await _container.ReplaceItemAsync(panelist, id, new PartitionKey(id));
            _logger.LogInformation("Soft deleted panelist: {PanelistId}", id);
            return true;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Failed to delete panelist: {PanelistId}", id);
            throw;
        }
    }

    /// <summary>
    /// Get all panelists (with pagination)
    /// </summary>
    public virtual async Task<List<Panelist>> GetAllPanelistsAsync(int pageSize = 100, string? continuationToken = null)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.isActive = true");
        
        var iterator = _container.GetItemQueryIterator<Panelist>(
            query,
            continuationToken,
            new QueryRequestOptions { MaxItemCount = pageSize }
        );

        var results = new List<Panelist>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
            break; // Only get first page for now
        }

        return results;
    }
}
