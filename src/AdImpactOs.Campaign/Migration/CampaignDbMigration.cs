using Microsoft.Azure.Cosmos;
using System.Collections.ObjectModel;

namespace AdImpactOs.Campaign.Migration;

public class CampaignDbMigration
{
    private readonly CosmosClient _cosmosClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CampaignDbMigration> _logger;

    public CampaignDbMigration(CosmosClient cosmosClient, IConfiguration configuration, ILogger<CampaignDbMigration> logger)
    {
        _cosmosClient = cosmosClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RunMigrationAsync()
    {
        var databaseName = _configuration["CosmosDb:DatabaseName"] ?? "AdImpactOsDB";
        var containerName = _configuration["CosmosDb:ContainerName"] ?? "Campaigns";
        var impressionContainerName = _configuration["CosmosDb:ImpressionContainerName"] ?? "Impressions";

        try
        {
            var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
            if (databaseResponse.StatusCode == System.Net.HttpStatusCode.Created)
            {
                _logger.LogInformation("Database created: {DatabaseName}", databaseName);
            }
            else
            {
                _logger.LogInformation("Database already exists: {DatabaseName}", databaseName);
            }

            var database = _cosmosClient.GetDatabase(databaseName);
            await CreateCampaignsContainerAsync(database, containerName);
            await CreateImpressionsContainerAsync(database, impressionContainerName);
            _logger.LogInformation("Campaign migration completed successfully");
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Campaign migration failed: {Message}", ex.Message);
            throw;
        }
    }

    public async Task SeedSampleDataAsync()
    {
        var databaseName = _configuration["CosmosDb:DatabaseName"] ?? "AdImpactOsDB";
        var containerName = _configuration["CosmosDb:ContainerName"] ?? "Campaigns";
        var container = _cosmosClient.GetDatabase(databaseName).GetContainer(containerName);

        var sampleCampaigns = new List<object>
        {
            new
            {
                id = "campaign_summer_beverage_2024",
                campaignId = "campaign_summer_beverage_2024",
                campaignName = "Summer Refresh - Energy Drink Launch",
                advertiser = "FreshBurst Beverages Inc.",
                industry = "CPG",
                startDate = new DateTime(2024, 6, 1),
                endDate = new DateTime(2024, 8, 31, 23, 59, 59),
                budget = 500000,
                status = "Active",
                targetAudience = new
                {
                    ageRange = new List<string> { "18-24", "25-34" },
                    gender = new List<string> { "M", "F" },
                    interests = new List<string> { "sports", "fitness", "gaming" },
                    countries = new List<string> { "US", "CA" }
                },
                creatives = new List<object>
                {
                    new { creativeId = "creative_banner_728x90_v1", format = "banner", size = "728x90", message = "Fuel Your Summer Adventures", variantName = "Lifestyle" },
                    new { creativeId = "creative_banner_728x90_v2", format = "banner", size = "728x90", message = "Unleash Your Energy", variantName = "Action" },
                    new { creativeId = "creative_banner_300x250_v1", format = "banner", size = "300x250", message = "Summer Energy Boost", variantName = "Compact" }
                },
                kpis = new { targetImpressions = 5000000L, targetReach = 1000000L, targetLift = 15.0 },
                createdAt = new DateTime(2024, 5, 15, 10, 0, 0),
                updatedAt = new DateTime(2024, 6, 1, 8, 0, 0)
            },
            new
            {
                id = "campaign_retail_back_to_school_2024",
                campaignId = "campaign_retail_back_to_school_2024",
                campaignName = "Back to School Blowout",
                advertiser = "SmartShop Retail",
                industry = "Retail",
                startDate = new DateTime(2024, 7, 15),
                endDate = new DateTime(2024, 9, 15, 23, 59, 59),
                budget = 350000,
                status = "Active",
                targetAudience = new
                {
                    ageRange = new List<string> { "25-34", "35-44" },
                    gender = new List<string> { "F" },
                    interests = new List<string> { "shopping", "family", "education" },
                    countries = new List<string> { "US" }
                },
                creatives = new List<object>
                {
                    new { creativeId = "creative_banner_300x600_v1", format = "banner", size = "300x600", message = "Save Big on Back to School", variantName = "Value" },
                    new { creativeId = "creative_banner_728x90_bts_v1", format = "banner", size = "728x90", message = "Everything Your Kids Need", variantName = "Comprehensive" }
                },
                kpis = new { targetImpressions = 3000000L, targetReach = 750000L, targetLift = 18.0 },
                createdAt = new DateTime(2024, 6, 20, 9, 0, 0),
                updatedAt = new DateTime(2024, 7, 15, 7, 0, 0)
            },
            new
            {
                id = "campaign_pharma_allergy_spring_2024",
                campaignId = "campaign_pharma_allergy_spring_2024",
                campaignName = "Spring Relief - Allergy Medication",
                advertiser = "HealthFirst Pharmaceuticals",
                industry = "Pharmaceutical",
                startDate = new DateTime(2024, 3, 1),
                endDate = new DateTime(2024, 5, 31, 23, 59, 59),
                budget = 800000,
                status = "Completed",
                targetAudience = new
                {
                    ageRange = new List<string> { "25-34", "35-44", "45-54", "55-64" },
                    gender = new List<string> { "M", "F" },
                    interests = new List<string> { "health", "outdoors" },
                    countries = new List<string> { "US", "UK" }
                },
                creatives = new List<object>
                {
                    new { creativeId = "creative_banner_728x90_relief_v1", format = "banner", size = "728x90", message = "Breathe Easy This Spring", variantName = "Symptom Relief" },
                    new { creativeId = "creative_video_15sec_v1", format = "video", size = "1280x720", message = "24-Hour Allergy Relief", variantName = "Duration Focus" }
                },
                kpis = new { targetImpressions = 6000000L, targetReach = 1500000L, targetLift = 22.0 },
                actualMetrics = new { impressions = 6234567L, reach = 1587234L, averageLift = 24.5 },
                createdAt = new DateTime(2024, 1, 15, 10, 0, 0),
                updatedAt = new DateTime(2024, 5, 31, 23, 59, 59)
            },
            new
            {
                id = "campaign_finance_credit_card_q2_2024",
                campaignId = "campaign_finance_credit_card_q2_2024",
                campaignName = "Rewards Plus - Premium Credit Card",
                advertiser = "GlobalBank Financial",
                industry = "Financial Services",
                startDate = new DateTime(2024, 4, 1),
                endDate = new DateTime(2024, 6, 30, 23, 59, 59),
                budget = 600000,
                status = "Completed",
                targetAudience = new
                {
                    ageRange = new List<string> { "25-34", "35-44", "45-54" },
                    gender = new List<string> { "M", "F" },
                    interests = new List<string> { "finance", "travel", "shopping" },
                    countries = new List<string> { "US", "CA" }
                },
                creatives = new List<object>
                {
                    new { creativeId = "creative_banner_300x250_rewards_v1", format = "banner", size = "300x250", message = "5% Cash Back on Everything", variantName = "Rewards" },
                    new { creativeId = "creative_banner_728x90_travel_v1", format = "banner", size = "728x90", message = "Travel Rewards Reimagined", variantName = "Travel" }
                },
                kpis = new { targetImpressions = 4000000L, targetReach = 1000000L, targetLift = 16.0 },
                actualMetrics = new { impressions = 4156789L, reach = 1034567L, averageLift = 17.8 },
                createdAt = new DateTime(2024, 2, 20, 10, 0, 0),
                updatedAt = new DateTime(2024, 6, 30, 23, 59, 59)
            },
            new
            {
                id = "campaign_travel_cruise_summer_2024",
                campaignId = "campaign_travel_cruise_summer_2024",
                campaignName = "Mediterranean Dreams - Luxury Cruise",
                advertiser = "OceanVoyage Cruises",
                industry = "Travel & Hospitality",
                startDate = new DateTime(2024, 5, 1),
                endDate = new DateTime(2024, 7, 31, 23, 59, 59),
                budget = 450000,
                status = "Active",
                targetAudience = new
                {
                    ageRange = new List<string> { "35-44", "45-54", "55-64", "65+" },
                    gender = new List<string> { "M", "F" },
                    interests = new List<string> { "travel", "luxury", "culture" },
                    countries = new List<string> { "US", "UK", "AU" }
                },
                creatives = new List<object>
                {
                    new { creativeId = "creative_video_30sec_cruise_v1", format = "video", size = "1920x1080", message = "Discover the Mediterranean in Style", variantName = "Destination" },
                    new { creativeId = "creative_banner_728x90_luxury_v1", format = "banner", size = "728x90", message = "All-Inclusive Luxury Awaits", variantName = "Luxury" }
                },
                kpis = new { targetImpressions = 2500000L, targetReach = 600000L, targetLift = 20.0 },
                createdAt = new DateTime(2024, 3, 15, 10, 0, 0),
                updatedAt = new DateTime(2024, 5, 1, 8, 0, 0)
            },
            new
            {
                id = "campaign_food_delivery_q3_2024",
                campaignId = "campaign_food_delivery_q3_2024",
                campaignName = "Quick Eats - Food Delivery App",
                advertiser = "QuickBite Technologies",
                industry = "Food & Beverage",
                startDate = new DateTime(2024, 7, 1),
                endDate = new DateTime(2024, 9, 30, 23, 59, 59),
                budget = 400000,
                status = "Active",
                targetAudience = new
                {
                    ageRange = new List<string> { "18-24", "25-34", "35-44" },
                    gender = new List<string> { "M", "F" },
                    interests = new List<string> { "food", "convenience", "technology" },
                    countries = new List<string> { "US", "CA" }
                },
                creatives = new List<object>
                {
                    new { creativeId = "creative_banner_300x250_food_v1", format = "banner", size = "300x250", message = "Food Delivered in 30 Minutes", variantName = "Speed" },
                    new { creativeId = "creative_banner_320x50_mobile_v1", format = "banner", size = "320x50", message = "Get $15 Off Your First Order", variantName = "Promotion" }
                },
                kpis = new { targetImpressions = 3500000L, targetReach = 900000L, targetLift = 19.0 },
                createdAt = new DateTime(2024, 6, 1, 10, 0, 0),
                updatedAt = new DateTime(2024, 7, 1, 7, 0, 0)
            },
            new
            {
                id = "campaign_auto_suv_fall_2024",
                campaignId = "campaign_auto_suv_fall_2024",
                campaignName = "Adventure Awaits - SUV Launch",
                advertiser = "DriveMax Automotive",
                industry = "Automotive",
                startDate = new DateTime(2024, 9, 1),
                endDate = new DateTime(2024, 11, 30, 23, 59, 59),
                budget = 1200000,
                status = "Scheduled",
                targetAudience = new
                {
                    ageRange = new List<string> { "25-34", "35-44", "45-54" },
                    gender = new List<string> { "M", "F" },
                    interests = new List<string> { "automotive", "outdoor", "family" },
                    countries = new List<string> { "US", "CA", "UK" }
                },
                creatives = new List<object>
                {
                    new { creativeId = "creative_video_pre_roll_v1", format = "video", size = "1920x1080", message = "Your Family's Next Adventure", variantName = "Family Focus" },
                    new { creativeId = "creative_banner_970x250_v1", format = "banner", size = "970x250", message = "Power Meets Luxury", variantName = "Premium" }
                },
                kpis = new { targetImpressions = 8000000L, targetReach = 2000000L, targetLift = 20.0 },
                createdAt = new DateTime(2024, 8, 1, 10, 0, 0),
                updatedAt = new DateTime(2024, 8, 15, 14, 30, 0)
            },
            new
            {
                id = "campaign_tech_smartphone_holiday_2024",
                campaignId = "campaign_tech_smartphone_holiday_2024",
                campaignName = "Holiday Tech - Flagship Smartphone",
                advertiser = "TechNova Electronics",
                industry = "Technology",
                startDate = new DateTime(2024, 11, 1),
                endDate = new DateTime(2024, 12, 31, 23, 59, 59),
                budget = 1500000,
                status = "Scheduled",
                targetAudience = new
                {
                    ageRange = new List<string> { "18-24", "25-34", "35-44" },
                    gender = new List<string> { "M", "F" },
                    interests = new List<string> { "technology", "gaming", "photography" },
                    countries = new List<string> { "US", "CA", "UK", "AU" }
                },
                creatives = new List<object>
                {
                    new { creativeId = "creative_video_30sec_tech_v1", format = "video", size = "1920x1080", message = "Photography Perfected", variantName = "Camera Focus" },
                    new { creativeId = "creative_banner_970x90_tech_v1", format = "banner", size = "970x90", message = "Power in Your Pocket", variantName = "Performance" }
                },
                kpis = new { targetImpressions = 10000000L, targetReach = 3000000L, targetLift = 25.0 },
                createdAt = new DateTime(2024, 9, 15, 10, 0, 0),
                updatedAt = new DateTime(2024, 10, 1, 12, 0, 0)
            },
            new
            {
                id = "campaign_insurance_home_q4_2024",
                campaignId = "campaign_insurance_home_q4_2024",
                campaignName = "Protect Your Home - Insurance Bundle",
                advertiser = "SecureLife Insurance",
                industry = "Insurance",
                startDate = new DateTime(2024, 10, 1),
                endDate = new DateTime(2024, 12, 31, 23, 59, 59),
                budget = 550000,
                status = "Scheduled",
                targetAudience = new
                {
                    ageRange = new List<string> { "25-34", "35-44", "45-54" },
                    gender = new List<string> { "M", "F" },
                    interests = new List<string> { "homeownership", "family", "finance" },
                    countries = new List<string> { "US" }
                },
                creatives = new List<object>
                {
                    new { creativeId = "creative_banner_728x90_home_v1", format = "banner", size = "728x90", message = "Bundle and Save 20%", variantName = "Savings" },
                    new { creativeId = "creative_banner_300x600_family_v1", format = "banner", size = "300x600", message = "Protection for What Matters Most", variantName = "Family" }
                },
                kpis = new { targetImpressions = 3000000L, targetReach = 800000L, targetLift = 17.0 },
                createdAt = new DateTime(2024, 8, 20, 10, 0, 0),
                updatedAt = new DateTime(2024, 9, 15, 14, 0, 0)
            },
            new
            {
                id = "campaign_fitness_gym_new_year_2025",
                campaignId = "campaign_fitness_gym_new_year_2025",
                campaignName = "New Year, New You - Gym Membership",
                advertiser = "FitLife Gyms",
                industry = "Health & Fitness",
                startDate = new DateTime(2024, 12, 15),
                endDate = new DateTime(2025, 2, 15, 23, 59, 59),
                budget = 300000,
                status = "Scheduled",
                targetAudience = new
                {
                    ageRange = new List<string> { "18-24", "25-34", "35-44" },
                    gender = new List<string> { "M", "F" },
                    interests = new List<string> { "fitness", "health", "wellness" },
                    countries = new List<string> { "US", "CA" }
                },
                creatives = new List<object>
                {
                    new { creativeId = "creative_video_15sec_fitness_v1", format = "video", size = "1280x720", message = "Transform Your Life in 2025", variantName = "Motivation" },
                    new { creativeId = "creative_banner_300x250_promo_v1", format = "banner", size = "300x250", message = "50% Off First 3 Months", variantName = "Offer" }
                },
                kpis = new { targetImpressions = 2000000L, targetReach = 500000L, targetLift = 21.0 },
                createdAt = new DateTime(2024, 11, 1, 10, 0, 0),
                updatedAt = new DateTime(2024, 11, 15, 12, 0, 0)
            }
        };

        foreach (var campaign in sampleCampaigns)
        {
            try
            {
                dynamic c = campaign;
                string campaignId = c.campaignId;
                await container.CreateItemAsync(campaign, new PartitionKey(campaignId));
                _logger.LogInformation("Seeded campaign: {CampaignId}", campaignId);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                dynamic c = campaign;
                string campaignId = c.campaignId;
                _logger.LogInformation("Campaign already exists: {CampaignId}", campaignId);
            }
        }

        _logger.LogInformation("Sample campaign data seeding completed - 10 campaigns seeded");
    }

    public async Task SeedImpressionDataAsync()
    {
        var databaseName = _configuration["CosmosDb:DatabaseName"] ?? "AdImpactOsDB";
        var impressionContainerName = _configuration["CosmosDb:ImpressionContainerName"] ?? "Impressions";
        var container = _cosmosClient.GetDatabase(databaseName).GetContainer(impressionContainerName);

        var random = new Random(42);
        var devices = new[] { "Desktop", "Mobile", "Tablet" };
        var countries = new[] { "US", "CA", "UK", "AU", "DE", "FR" };
        var sources = new[] { "Pixel", "S2S" };

        var campaignCreatives = new Dictionary<string, string[]>
        {
            ["campaign_summer_beverage_2024"] = new[] { "creative_banner_728x90_v1", "creative_banner_728x90_v2", "creative_banner_300x250_v1" },
            ["campaign_retail_back_to_school_2024"] = new[] { "creative_banner_300x600_v1", "creative_banner_728x90_bts_v1" },
            ["campaign_pharma_allergy_spring_2024"] = new[] { "creative_banner_728x90_relief_v1", "creative_video_15sec_v1" },
            ["campaign_food_delivery_q3_2024"] = new[] { "creative_banner_300x250_food_v1", "creative_banner_320x50_mobile_v1" },
            ["campaign_travel_cruise_summer_2024"] = new[] { "creative_video_30sec_cruise_v1", "creative_banner_728x90_luxury_v1" },
            ["campaign_finance_credit_card_q2_2024"] = new[] { "creative_banner_300x250_rewards_v1", "creative_banner_728x90_travel_v1" },
            ["campaign_auto_suv_fall_2024"] = new[] { "creative_video_pre_roll_v1", "creative_banner_970x250_v1" },
            ["campaign_tech_smartphone_holiday_2024"] = new[] { "creative_video_30sec_tech_v1", "creative_banner_970x90_tech_v1" },
            ["campaign_insurance_home_q4_2024"] = new[] { "creative_banner_728x90_home_v1", "creative_banner_300x600_family_v1" },
            ["campaign_fitness_gym_new_year_2025"] = new[] { "creative_video_15sec_fitness_v1", "creative_banner_300x250_promo_v1" }
        };

        // Expanded panelist pool: panelist-001 through panelist-100
        var panelistIds = Enumerable.Range(1, 100).Select(i => $"panelist-{i:D3}").ToArray();

        var seededCount = 0;

        foreach (var (campaignId, creativeIds) in campaignCreatives)
        {
            var impressionCount = random.Next(40, 80);

            for (var i = 0; i < impressionCount; i++)
            {
                var impressionId = $"imp_{campaignId}_{i:D4}";
                var creativeId = creativeIds[random.Next(creativeIds.Length)];
                var panelistId = panelistIds[random.Next(panelistIds.Length)];
                var isBot = random.NextDouble() < 0.08;
                var hoursAgo = random.Next(1, 168);

                var impression = new
                {
                    id = impressionId,
                    impressionId = impressionId,
                    campaignId = campaignId,
                    creativeId = creativeId,
                    panelistId = panelistId,
                    deviceType = devices[random.Next(devices.Length)],
                    country = countries[random.Next(countries.Length)],
                    isBot = isBot,
                    botReason = isBot ? "Bot pattern in user agent" : (string?)null,
                    ingestSource = sources[random.Next(sources.Length)],
                    timestampUtc = DateTime.UtcNow.AddHours(-hoursAgo),
                    createdAt = DateTime.UtcNow.AddHours(-hoursAgo)
                };

                try
                {
                    await container.CreateItemAsync(impression, new PartitionKey(campaignId));
                    seededCount++;
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    // Already exists
                }
            }
        }

        _logger.LogInformation("Sample impression data seeding completed - {Count} impressions seeded across 10 campaigns", seededCount);
    }

    private async Task CreateCampaignsContainerAsync(Database database, string containerName)
    {
        var containerProperties = new ContainerProperties
        {
            Id = containerName,
            PartitionKeyPath = "/campaignId",
            IndexingPolicy = new IndexingPolicy
            {
                Automatic = true,
                IndexingMode = IndexingMode.Consistent,
                IncludedPaths = { new IncludedPath { Path = "/*" } },
                ExcludedPaths = { new ExcludedPath { Path = "/\"_etag\"/?" } },
                CompositeIndexes =
                {
                    new Collection<CompositePath>
                    {
                        new CompositePath { Path = "/status", Order = CompositePathSortOrder.Ascending },
                        new CompositePath { Path = "/startDate", Order = CompositePathSortOrder.Descending }
                    },
                    new Collection<CompositePath>
                    {
                        new CompositePath { Path = "/industry", Order = CompositePathSortOrder.Ascending },
                        new CompositePath { Path = "/createdAt", Order = CompositePathSortOrder.Descending }
                    },
                    new Collection<CompositePath>
                    {
                        new CompositePath { Path = "/advertiser", Order = CompositePathSortOrder.Ascending },
                        new CompositePath { Path = "/budget", Order = CompositePathSortOrder.Descending }
                    }
                }
            }
        };

        _logger.LogInformation("Creating container: {ContainerName}", containerName);
        var containerResponse = await database.CreateContainerIfNotExistsAsync(containerProperties);

        if (containerResponse.StatusCode == System.Net.HttpStatusCode.Created)
        {
            _logger.LogInformation("Container created: {ContainerName}", containerName);
        }
        else
        {
            _logger.LogInformation("Container already exists: {ContainerName}", containerName);
        }
    }

    private async Task CreateImpressionsContainerAsync(Database database, string containerName)
    {
        var containerProperties = new ContainerProperties
        {
            Id = containerName,
            PartitionKeyPath = "/campaignId",
            IndexingPolicy = new IndexingPolicy
            {
                Automatic = true,
                IndexingMode = IndexingMode.Consistent,
                IncludedPaths = { new IncludedPath { Path = "/*" } },
                ExcludedPaths = { new ExcludedPath { Path = "/\"_etag\"/?" } }
            }
        };

        _logger.LogInformation("Creating container: {ContainerName}", containerName);
        var containerResponse = await database.CreateContainerIfNotExistsAsync(containerProperties);

        if (containerResponse.StatusCode == System.Net.HttpStatusCode.Created)
        {
            _logger.LogInformation("Container created: {ContainerName}", containerName);
        }
        else
        {
            _logger.LogInformation("Container already exists: {ContainerName}", containerName);
        }
    }
}
