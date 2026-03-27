using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AdImpactOs.Models;

namespace AdImpactOs.Services;

/// <summary>
/// Forwards tracking events directly to the Campaign API when Event Hub is not configured.
/// This enables full end-to-end local development without Azure Event Hubs.
/// </summary>
public class LocalImpressionForwarder
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocalImpressionForwarder> _logger;

    public LocalImpressionForwarder(HttpClient httpClient, ILogger<LocalImpressionForwarder> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task ForwardAsync(TrackingResponse trackingResponse)
    {
        try
        {
            var impression = new
            {
                impressionId = trackingResponse.EventId,
                campaignId = trackingResponse.CampaignId,
                creativeId = trackingResponse.CreativeId,
                panelistId = trackingResponse.PanelistToken,
                deviceType = trackingResponse.DeviceType ?? "Unknown",
                country = "Unknown",
                isBot = false,
                botReason = (string?)null,
                ingestSource = trackingResponse.S2SFlag ? "S2S" : "Pixel",
                timestampUtc = trackingResponse.Timestamp.ToUniversalTime()
            };

            var json = JsonConvert.SerializeObject(impression);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/impressions", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Forwarded impression {ImpressionId} to Campaign API (local mode)",
                    trackingResponse.EventId);
            }
            else
            {
                _logger.LogWarning(
                    "Campaign API returned {StatusCode} for impression {ImpressionId}",
                    response.StatusCode, trackingResponse.EventId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to forward impression {ImpressionId} to Campaign API. Is CampaignAPI running on port 5003?",
                trackingResponse.EventId);
        }
    }
}
