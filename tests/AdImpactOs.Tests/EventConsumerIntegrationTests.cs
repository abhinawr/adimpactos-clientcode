using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using AdImpactOs.EventConsumer.Models;
using AdImpactOs.EventConsumer.Services;
using Newtonsoft.Json;

namespace AdImpactOs.Tests;

public class EventConsumerIntegrationTests
{
    [Fact]
    public void AdImpressionEvent_DeserializesFromTrackingResponse_Correctly()
    {
        // This JSON simulates what AdTracker/S2STracker publishes to EventHub
        var trackingResponseJson = @"{
            ""event_id"": ""abc-123"",
            ""timestamp"": ""2024-07-15T10:30:00Z"",
            ""campaign_id"": ""campaign_summer_beverage_2024"",
            ""creative_id"": ""creative_banner_728x90_v1"",
            ""panelist_token"": ""panelist_001"",
            ""user_agent"": ""Mozilla/5.0 (Windows NT 10.0; Win64; x64)"",
            ""device_type"": ""Desktop"",
            ""ip"": ""203.0.113.1"",
            ""referrer"": ""https://example.com"",
            ""ad_server"": ""doubleclick"",
            ""s2s_flag"": false,
            ""raw_headers"": { ""User-Agent"": ""Mozilla/5.0"" },
            ""tracking_hash"": ""abc123hash""
        }";

        var evt = JsonConvert.DeserializeObject<AdImpressionEvent>(trackingResponseJson);

        evt.Should().NotBeNull();
        evt!.EventId.Should().Be("abc-123");
        evt.CampaignId.Should().Be("campaign_summer_beverage_2024");
        evt.CreativeId.Should().Be("creative_banner_728x90_v1");
        evt.PanelistToken.Should().Be("panelist_001");
        evt.UserAgent.Should().Contain("Mozilla");
        evt.IpAddress.Should().Be("203.0.113.1");
        evt.Referrer.Should().Be("https://example.com");
        evt.AdServer.Should().Be("doubleclick");
        evt.IsS2S.Should().BeFalse();
        evt.RawHeaders.Should().ContainKey("User-Agent");
        evt.TrackingHash.Should().Be("abc123hash");
    }

    [Fact]
    public void AdImpressionEvent_DeserializesS2SEvent_Correctly()
    {
        var s2sJson = @"{
            ""event_id"": ""s2s-456"",
            ""timestamp"": ""2024-07-15T10:30:00Z"",
            ""campaign_id"": ""campaign_food_delivery_q3_2024"",
            ""creative_id"": ""creative_banner_300x250_food_v1"",
            ""panelist_token"": ""panelist_005"",
            ""user_agent"": ""ServerBot/1.0"",
            ""ip"": ""10.0.0.1"",
            ""s2s_flag"": true
        }";

        var evt = JsonConvert.DeserializeObject<AdImpressionEvent>(s2sJson);

        evt.Should().NotBeNull();
        evt!.IsS2S.Should().BeTrue();
        evt.CampaignId.Should().Be("campaign_food_delivery_q3_2024");
    }

    [Fact]
    public void NormalizedImpression_HasCorrectDefaults()
    {
        var normalized = new NormalizedImpression();

        normalized.ImpressionId.Should().Be(string.Empty);
        normalized.CampaignId.Should().Be(string.Empty);
        normalized.DeviceType.Should().Be("Unknown");
        normalized.Country.Should().Be("Unknown");
        normalized.IsBot.Should().BeFalse();
        normalized.IngestSource.Should().Be(string.Empty);
    }

    [Fact]
    public void NormalizedImpression_CanBePopulated()
    {
        var normalized = new NormalizedImpression
        {
            ImpressionId = "evt-123",
            TimestampUtc = DateTime.UtcNow,
            CampaignId = "campaign_summer_beverage_2024",
            CreativeId = "creative_banner_728x90_v1",
            PanelistId = "panelist_001",
            DeviceType = "Desktop",
            Country = "US",
            IsBot = false,
            IngestSource = "Pixel"
        };

        normalized.CampaignId.Should().Be("campaign_summer_beverage_2024");
        normalized.IngestSource.Should().Be("Pixel");
        normalized.IsBot.Should().BeFalse();
    }

    [Fact]
    public void BotDetectionService_DetectsBot_FromUserAgent()
    {
        var service = new BotDetectionService();

        var result = service.DetectBot("Googlebot/2.1 (+http://www.google.com/bot.html)", "203.0.113.1");

        result.isBot.Should().BeTrue();
        result.reason.Should().Contain("Bot pattern");
    }

    [Fact]
    public void BotDetectionService_AllowsValidUserAgent()
    {
        var service = new BotDetectionService();

        var result = service.DetectBot("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36", "203.0.113.1");

        result.isBot.Should().BeFalse();
        result.reason.Should().BeNull();
    }

    [Fact]
    public void BotDetectionService_DetectsEmptyUserAgent()
    {
        var service = new BotDetectionService();

        var result = service.DetectBot("", "203.0.113.1");

        result.isBot.Should().BeTrue();
        result.reason.Should().Contain("Empty user agent");
    }

    [Fact]
    public void GeoEnrichmentService_ReturnsCountry()
    {
        var service = new GeoEnrichmentService();

        var country = service.GetCountryFromIp("192.168.1.1");
        country.Should().Be("US");

        var unknown = service.GetCountryFromIp("8.8.8.8");
        unknown.Should().Be("Unknown");
    }

    [Fact]
    public void GeoEnrichmentService_ReturnsDeviceType()
    {
        var service = new GeoEnrichmentService();

        service.GetDeviceType("Mozilla/5.0 (Windows NT 10.0; Win64; x64)").Should().Be("Desktop");
        service.GetDeviceType("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0)").Should().Be("Mobile");
        service.GetDeviceType("Mozilla/5.0 (iPad; CPU OS 17_0)").Should().Be("Tablet");
        service.GetDeviceType("").Should().Be("Unknown");
    }

    [Fact]
    public void TrackingResponse_And_AdImpressionEvent_FieldsAlign()
    {
        // Verify that TrackingResponse JSON property names match AdImpressionEvent JSON property names
        var trackingResponseProps = typeof(AdImpactOs.Models.TrackingResponse)
            .GetProperties()
            .Select(p =>
            {
                var attr = p.GetCustomAttributes(typeof(Newtonsoft.Json.JsonPropertyAttribute), false)
                    .FirstOrDefault() as Newtonsoft.Json.JsonPropertyAttribute;
                return attr?.PropertyName ?? p.Name;
            })
            .ToHashSet();

        var adImpressionProps = typeof(AdImpressionEvent)
            .GetProperties()
            .Select(p =>
            {
                var attr = p.GetCustomAttributes(typeof(Newtonsoft.Json.JsonPropertyAttribute), false)
                    .FirstOrDefault() as Newtonsoft.Json.JsonPropertyAttribute;
                return attr?.PropertyName ?? p.Name;
            })
            .ToHashSet();

        // Key fields that must exist in both
        var requiredFields = new[] { "event_id", "campaign_id", "creative_id", "panelist_token", "s2s_flag", "ip", "timestamp" };
        foreach (var field in requiredFields)
        {
            trackingResponseProps.Should().Contain(field,
                because: $"TrackingResponse should have JSON property '{field}'");
            adImpressionProps.Should().Contain(field,
                because: $"AdImpressionEvent should have JSON property '{field}'");
        }
    }
}
