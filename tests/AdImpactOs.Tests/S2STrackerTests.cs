using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using AdImpactOs.Functions.S2S;
using AdImpactOs.Functions.Pixel;
using AdImpactOs.Models;
using FluentAssertions;
using Newtonsoft.Json;

namespace AdImpactOs.Tests;

/// <summary>
/// Unit tests for the S2STracker server-to-server tracking function.
/// </summary>
public class S2STrackerTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly S2STracker _tracker;

    public S2STrackerTests()
    {
        _mockLogger = new Mock<ILogger>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        
        _tracker = new S2STracker(_mockLoggerFactory.Object);
    }

    [Fact]
    public void S2STrackingRequest_HasRequiredFields()
    {
        // Arrange & Act
        var request = new S2STrackingRequest
        {
            CampaignId = "camp123",
            CreativeId = "cre456",
            PanelistToken = "user789"
        };

        // Assert
        request.CampaignId.Should().Be("camp123");
        request.CreativeId.Should().Be("cre456");
        request.PanelistToken.Should().Be("user789");
    }

    [Fact]
    public void S2STrackingRequest_HasOptionalFields()
    {
        // Arrange & Act
        var request = new S2STrackingRequest
        {
            CampaignId = "camp123",
            CreativeId = "cre456",
            PanelistToken = "user789",
            AdServer = "doubleclick",
            IdempotencyKey = "unique-key-123",
            Timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        // Assert
        request.AdServer.Should().Be("doubleclick");
        request.IdempotencyKey.Should().Be("unique-key-123");
        request.Timestamp.Should().Be(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void S2STrackingRequest_Serialization_ProducesValidJson()
    {
        // Arrange
        var request = new S2STrackingRequest
        {
            CampaignId = "summer2024",
            CreativeId = "banner300x250",
            PanelistToken = "user12345",
            AdServer = "doubleclick",
            IdempotencyKey = "tx-12345",
            Timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var json = JsonConvert.SerializeObject(request);
        var deserialized = JsonConvert.DeserializeObject<S2STrackingRequest>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.CampaignId.Should().Be(request.CampaignId);
        deserialized.CreativeId.Should().Be(request.CreativeId);
        deserialized.PanelistToken.Should().Be(request.PanelistToken);
        deserialized.AdServer.Should().Be(request.AdServer);
        deserialized.IdempotencyKey.Should().Be(request.IdempotencyKey);
        deserialized.Timestamp.Should().Be(request.Timestamp);
    }

    [Fact]
    public void S2STrackingRequest_JsonProperties_AreCorrectlyNamed()
    {
        // Arrange
        var request = new S2STrackingRequest
        {
            CampaignId = "test",
            CreativeId = "test",
            PanelistToken = "test"
        };

        // Act
        var json = JsonConvert.SerializeObject(request);

        // Assert - Verify JSON property names match specification
        json.Should().Contain("\"campaign_id\":");
        json.Should().Contain("\"creative_id\":");
        json.Should().Contain("\"panelist_token\":");
    }

    [Fact]
    public void S2STrackerOutput_HasEventHubBinding()
    {
        // Arrange & Act
        var output = new S2STrackerOutput();

        // Assert - Verify the output type has the required properties
        output.Should().NotBeNull();
        typeof(S2STrackerOutput).GetProperty("HttpResponse").Should().NotBeNull();
        typeof(S2STrackerOutput).GetProperty("EventData").Should().NotBeNull();
        
        // Verify EventHub attribute is present
        var eventDataProperty = typeof(S2STrackerOutput).GetProperty("EventData");
        var eventHubAttribute = eventDataProperty.GetCustomAttributes(typeof(EventHubOutputAttribute), false).FirstOrDefault();
        eventHubAttribute.Should().NotBeNull();
        
        var attr = eventHubAttribute as EventHubOutputAttribute;
        attr.EventHubName.Should().Be("ad-impressions");
    }

    [Fact]
    public void S2STrackerOutput_UsesSameEventSchema_AsPixelTracker()
    {
        // Arrange
        var s2sOutput = new S2STrackerOutput();
        var pixelOutput = new PixelTrackerOutput();

        // Assert - Both outputs should use the same TrackingResponse model
        s2sOutput.Should().NotBeNull();
        pixelOutput.Should().NotBeNull();
        
        var s2sEventDataType = typeof(S2STrackerOutput).GetProperty("EventData").PropertyType;
        var pixelEventDataType = typeof(PixelTrackerOutput).GetProperty("EventData").PropertyType;
        
        s2sEventDataType.Should().Be(pixelEventDataType);
        s2sEventDataType.Should().Be(typeof(TrackingResponse));
    }

    [Fact]
    public void S2STrackingRequest_Deserialization_HandlesNullValues()
    {
        // Arrange
        var json = @"{
            ""campaign_id"": ""camp123"",
            ""creative_id"": ""cre456"",
            ""panelist_token"": ""user789"",
            ""ad_server"": null,
            ""idempotency_key"": null,
            ""timestamp"": null
        }";

        // Act
        var request = JsonConvert.DeserializeObject<S2STrackingRequest>(json);

        // Assert
        request.Should().NotBeNull();
        request.CampaignId.Should().Be("camp123");
        request.CreativeId.Should().Be("cre456");
        request.PanelistToken.Should().Be("user789");
        request.AdServer.Should().BeNull();
        request.IdempotencyKey.Should().BeNull();
        request.Timestamp.Should().BeNull();
    }

    [Fact]
    public void S2STrackingRequest_Deserialization_HandlesIso8601Timestamp()
    {
        // Arrange
        var json = @"{
            ""campaign_id"": ""camp123"",
            ""creative_id"": ""cre456"",
            ""panelist_token"": ""user789"",
            ""timestamp"": ""2024-01-15T10:30:00Z""
        }";

        // Act
        var request = JsonConvert.DeserializeObject<S2STrackingRequest>(json);

        // Assert
        request.Should().NotBeNull();
        request.Timestamp.Should().NotBeNull();
        request.Timestamp.Value.Year.Should().Be(2024);
        request.Timestamp.Value.Month.Should().Be(1);
        request.Timestamp.Value.Day.Should().Be(15);
        request.Timestamp.Value.Hour.Should().Be(10);
        request.Timestamp.Value.Minute.Should().Be(30);
    }

    [Fact]
    public void TrackingResponse_S2SFlag_IsSetCorrectly()
    {
        // Arrange
        var pixelResponse = new TrackingResponse { S2SFlag = false };
        var s2sResponse = new TrackingResponse { S2SFlag = true };

        // Assert
        pixelResponse.S2SFlag.Should().BeFalse();
        s2sResponse.S2SFlag.Should().BeTrue();
    }

    [Fact]
    public void S2STrackingRequest_MinimalValidRequest()
    {
        // Arrange - Minimal valid request with only required fields
        var json = @"{
            ""campaign_id"": ""camp123"",
            ""creative_id"": ""cre456"",
            ""panelist_token"": ""user789""
        }";

        // Act
        var request = JsonConvert.DeserializeObject<S2STrackingRequest>(json);

        // Assert
        request.Should().NotBeNull();
        request.CampaignId.Should().Be("camp123");
        request.CreativeId.Should().Be("cre456");
        request.PanelistToken.Should().Be("user789");
        request.AdServer.Should().BeNull();
        request.IdempotencyKey.Should().BeNull();
        request.Timestamp.Should().BeNull();
    }

    [Theory]
    [InlineData("camp123", "cre456", "user789")]
    [InlineData("summer-2024", "banner-300x250", "panelist-abc-123")]
    [InlineData("123", "456", "789")]
    public void S2STrackingRequest_AcceptsVariousIdFormats(string campaignId, string creativeId, string panelistToken)
    {
        // Arrange & Act
        var request = new S2STrackingRequest
        {
            CampaignId = campaignId,
            CreativeId = creativeId,
            PanelistToken = panelistToken
        };

        // Assert
        request.CampaignId.Should().Be(campaignId);
        request.CreativeId.Should().Be(creativeId);
        request.PanelistToken.Should().Be(panelistToken);
    }

    [Fact]
    public void S2STrackingRequest_WithAllFields_SerializesCorrectly()
    {
        // Arrange
        var request = new S2STrackingRequest
        {
            CampaignId = "camp123",
            CreativeId = "cre456",
            PanelistToken = "user789",
            AdServer = "doubleclick",
            IdempotencyKey = "unique-tx-12345",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var json = JsonConvert.SerializeObject(request);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("campaign_id");
        json.Should().Contain("creative_id");
        json.Should().Contain("panelist_token");
        json.Should().Contain("ad_server");
        json.Should().Contain("idempotency_key");
        json.Should().Contain("timestamp");
    }
}
