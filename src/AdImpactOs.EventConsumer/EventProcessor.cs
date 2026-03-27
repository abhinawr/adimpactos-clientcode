using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AdImpactOs.EventConsumer.Models;
using AdImpactOs.EventConsumer.Services;
using System.Text;

namespace AdImpactOs.EventConsumer;

public class EventProcessor
{
    private readonly ILogger<EventProcessor> _logger;
    private readonly IConfiguration _configuration;
    private readonly BotDetectionService _botDetection;
    private readonly GeoEnrichmentService _geoEnrichment;
    private readonly EventProcessorClient _processor;
    private readonly BlobContainerClient? _deadLetterContainer;
    private readonly HttpClient _campaignApiClient;
    private readonly int _maxRetries;

    public EventProcessor(
        ILogger<EventProcessor> logger,
        IConfiguration configuration,
        HttpClient? campaignApiClient = null)
    {
        _logger = logger;
        _configuration = configuration;
        _botDetection = new BotDetectionService();
        _geoEnrichment = new GeoEnrichmentService();
        _maxRetries = _configuration.GetValue<int>("EventHub:MaxRetries", 3);

        // Initialize Campaign API client for persisting impressions
        var campaignApiBaseUrl = _configuration["CampaignApi:BaseUrl"] ?? "http://localhost:5003";
        _campaignApiClient = campaignApiClient ?? new HttpClient { BaseAddress = new Uri(campaignApiBaseUrl) };

        var eventHubConnectionString = _configuration["EventHub:ConnectionString"]
            ?? throw new InvalidOperationException("EventHub connection string not configured");
        var eventHubName = _configuration["EventHub:Name"] ?? "ad-impressions";
        var blobConnectionString = _configuration["BlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Blob storage connection string not configured");
        var blobContainerName = _configuration["BlobStorage:ContainerName"] ?? "eventhub-checkpoints";
        var consumerGroup = _configuration["EventHub:ConsumerGroup"] ?? "$Default";

        // Initialize Dead Letter Container
        var dlqConnectionString = _configuration["DeadLetterQueue:ConnectionString"] ?? blobConnectionString;
        var dlqContainerName = _configuration["DeadLetterQueue:ContainerName"] ?? "dead-letter-queue";
        try
        {
            _deadLetterContainer = new BlobContainerClient(dlqConnectionString, dlqContainerName);
            _deadLetterContainer.CreateIfNotExists();
            _logger.LogInformation("Dead Letter Container initialized: {ContainerName}", dlqContainerName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize Dead Letter Container. DLQ will be disabled.");
            _deadLetterContainer = null;
        }

        var storageClient = new BlobContainerClient(blobConnectionString, blobContainerName);

        _processor = new EventProcessorClient(
            storageClient,
            consumerGroup,
            eventHubConnectionString,
            eventHubName);

        _processor.ProcessEventAsync += ProcessEventHandler;
        _processor.ProcessErrorAsync += ProcessErrorHandler;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting EventHub processor...");
        await _processor.StartProcessingAsync(cancellationToken);
        _logger.LogInformation("EventHub processor started successfully");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping EventHub processor...");
        await _processor.StopProcessingAsync(cancellationToken);
        _logger.LogInformation("EventHub processor stopped");
    }

    private async Task ProcessEventHandler(ProcessEventArgs args)
    {
        var retryCount = 0;
        var eventBody = string.Empty;

        try
        {
            eventBody = Encoding.UTF8.GetString(args.Data.Body.ToArray());
            var impressionEvent = JsonConvert.DeserializeObject<AdImpressionEvent>(eventBody);

            if (impressionEvent == null)
            {
                _logger.LogWarning("Failed to deserialize event, sending to DLQ");
                await SendToDeadLetterQueue(eventBody, "Deserialization failed", args.Data);
                await args.UpdateCheckpointAsync();
                return;
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(impressionEvent.EventId) ||
                string.IsNullOrWhiteSpace(impressionEvent.CampaignId))
            {
                _logger.LogWarning("Event missing required fields, sending to DLQ");
                await SendToDeadLetterQueue(eventBody, "Missing required fields", args.Data);
                await args.UpdateCheckpointAsync();
                return;
            }

            // Bot detection
            var (isBot, botReason) = _botDetection.DetectBot(
                impressionEvent.UserAgent,
                impressionEvent.IpAddress);

            // Geo enrichment
            var country = _geoEnrichment.GetCountryFromIp(impressionEvent.IpAddress);
            var deviceType = _geoEnrichment.GetDeviceType(impressionEvent.UserAgent);

            // Create normalized impression
            var normalized = new NormalizedImpression
            {
                ImpressionId = impressionEvent.EventId,
                TimestampUtc = impressionEvent.Timestamp.ToUniversalTime(),
                CampaignId = impressionEvent.CampaignId,
                CreativeId = impressionEvent.CreativeId,
                PanelistId = impressionEvent.PanelistToken,
                DeviceType = deviceType,
                Country = country,
                IsBot = isBot,
                IngestSource = impressionEvent.IsS2S ? "S2S" : "Pixel",
                BotReason = botReason
            };

            // Persist to Campaign API
            await PersistImpressionAsync(normalized);

            // Log processed event
            if (!isBot)
            {
                _logger.LogInformation(
                    "Processed impression {ImpressionId} for campaign {CampaignId}",
                    normalized.ImpressionId,
                    normalized.CampaignId);
            }
            else
            {
                _logger.LogInformation(
                    "Filtered bot impression {ImpressionId}: {Reason}",
                    normalized.ImpressionId,
                    botReason);
            }

            await args.UpdateCheckpointAsync();
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON parsing error, sending to DLQ");
            await SendToDeadLetterQueue(eventBody, $"JSON error: {jsonEx.Message}", args.Data);
            await args.UpdateCheckpointAsync();
        }
        catch (Exception ex)
        {
            retryCount = GetRetryCount(args.Data);
            
            if (retryCount >= _maxRetries)
            {
                _logger.LogError(ex, "Max retries exceeded, sending to DLQ. Retry count: {RetryCount}", retryCount);
                await SendToDeadLetterQueue(eventBody, $"Max retries exceeded: {ex.Message}", args.Data);
                await args.UpdateCheckpointAsync();
            }
            else
            {
                _logger.LogWarning(ex, "Error processing event, will retry. Retry count: {RetryCount}", retryCount);
                // Don't checkpoint - event will be retried
                throw;
            }
        }
    }

    private async Task PersistImpressionAsync(NormalizedImpression normalized)
    {
        try
        {
            var payload = new
            {
                impressionId = normalized.ImpressionId,
                campaignId = normalized.CampaignId,
                creativeId = normalized.CreativeId,
                panelistId = normalized.PanelistId,
                deviceType = normalized.DeviceType,
                country = normalized.Country,
                isBot = normalized.IsBot,
                botReason = normalized.BotReason,
                ingestSource = normalized.IngestSource,
                timestampUtc = normalized.TimestampUtc
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _campaignApiClient.PostAsync("/api/impressions", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Persisted impression {ImpressionId} to Campaign API", normalized.ImpressionId);
            }
            else
            {
                _logger.LogWarning("Failed to persist impression {ImpressionId}: {StatusCode}",
                    normalized.ImpressionId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist impression {ImpressionId} to Campaign API. Impression still processed.",
                normalized.ImpressionId);
        }
    }

    private async Task SendToDeadLetterQueue(string eventBody, string reason, EventData originalEvent)
    {
        if (_deadLetterContainer == null)
        {
            _logger.LogWarning("Dead Letter Container not available. Event will be lost: {Reason}", reason);
            return;
        }

        try
        {
            var dlqMessage = new
            {
                OriginalEvent = eventBody,
                ErrorReason = reason,
                Timestamp = DateTime.UtcNow,
                SequenceNumber = originalEvent.SequenceNumber,
                Offset = originalEvent.Offset,
                PartitionKey = originalEvent.PartitionKey,
                EnqueuedTime = originalEvent.EnqueuedTime
            };

            var messageJson = JsonConvert.SerializeObject(dlqMessage);
            var blobName = $"dlq-{DateTime.UtcNow:yyyy-MM-dd}/{Guid.NewGuid()}.json";
            var blobClient = _deadLetterContainer.GetBlobClient(blobName);
            
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(messageJson));
            await blobClient.UploadAsync(stream, overwrite: false);
            
            _logger.LogInformation("Event sent to Dead Letter Container: {Reason}, Blob: {BlobName}", reason, blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to Dead Letter Container");
        }
    }

    private int GetRetryCount(EventData eventData)
    {
        // Check if retry count is stored in properties
        if (eventData.Properties.TryGetValue("RetryCount", out var retryValue) && retryValue is int count)
        {
            return count;
        }
        return 0;
    }

    private Task ProcessErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Error in partition {PartitionId}: {Operation}",
            args.PartitionId,
            args.Operation);

        return Task.CompletedTask;
    }
}
