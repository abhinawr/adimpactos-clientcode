using Xunit;
using FluentAssertions;
using AdImpactOs.Campaign.Models;

namespace AdImpactOs.Campaign.Tests;

public class ImpressionModelTests
{
    [Fact]
    public void Impression_DefaultValues_AreSetCorrectly()
    {
        var impression = new Impression();

        impression.Id.Should().Be(string.Empty);
        impression.ImpressionId.Should().Be(string.Empty);
        impression.CampaignId.Should().Be(string.Empty);
        impression.CreativeId.Should().Be(string.Empty);
        impression.PanelistId.Should().Be(string.Empty);
        impression.DeviceType.Should().Be("Unknown");
        impression.Country.Should().Be("Unknown");
        impression.IsBot.Should().BeFalse();
        impression.BotReason.Should().BeNull();
        impression.IngestSource.Should().Be(string.Empty);
        impression.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Impression_CanSetAllProperties()
    {
        var impression = new Impression
        {
            Id = "imp_001",
            ImpressionId = "imp_001",
            CampaignId = "campaign_summer_beverage_2024",
            CreativeId = "creative_banner_728x90_v1",
            PanelistId = "panelist_001",
            DeviceType = "Desktop",
            Country = "US",
            IsBot = false,
            IngestSource = "Pixel",
            TimestampUtc = DateTime.UtcNow
        };

        impression.CampaignId.Should().Be("campaign_summer_beverage_2024");
        impression.CreativeId.Should().Be("creative_banner_728x90_v1");
        impression.DeviceType.Should().Be("Desktop");
        impression.IngestSource.Should().Be("Pixel");
    }

    [Fact]
    public void Impression_BotImpressions_CanBeMarked()
    {
        var impression = new Impression
        {
            ImpressionId = "imp_bot_001",
            CampaignId = "campaign_test",
            IsBot = true,
            BotReason = "Bot pattern in user agent"
        };

        impression.IsBot.Should().BeTrue();
        impression.BotReason.Should().Be("Bot pattern in user agent");
    }

    [Fact]
    public void ImpressionSummary_DefaultValues_AreCorrect()
    {
        var summary = new ImpressionSummary();

        summary.CampaignId.Should().Be(string.Empty);
        summary.TotalImpressions.Should().Be(0);
        summary.ValidImpressions.Should().Be(0);
        summary.BotImpressions.Should().Be(0);
        summary.UniquePanelists.Should().Be(0);
        summary.ByCreative.Should().BeEmpty();
        summary.ByDevice.Should().BeEmpty();
        summary.ByCountry.Should().BeEmpty();
        summary.BySource.Should().BeEmpty();
    }

    [Fact]
    public void ImpressionSummary_CanPopulateBreakdowns()
    {
        var summary = new ImpressionSummary
        {
            CampaignId = "campaign_test",
            TotalImpressions = 100,
            ValidImpressions = 92,
            BotImpressions = 8,
            UniquePanelists = 45,
            ByCreative = new List<CreativeImpressionCount>
            {
                new() { CreativeId = "creative_001", Count = 60 },
                new() { CreativeId = "creative_002", Count = 32 }
            },
            ByDevice = new Dictionary<string, long>
            {
                { "Desktop", 50 },
                { "Mobile", 35 },
                { "Tablet", 7 }
            },
            ByCountry = new Dictionary<string, long>
            {
                { "US", 70 },
                { "CA", 22 }
            },
            BySource = new Dictionary<string, long>
            {
                { "Pixel", 75 },
                { "S2S", 25 }
            }
        };

        summary.TotalImpressions.Should().Be(100);
        summary.ValidImpressions.Should().Be(92);
        summary.BotImpressions.Should().Be(8);
        summary.ByCreative.Should().HaveCount(2);
        summary.ByDevice.Should().HaveCount(3);
        summary.ByDevice["Desktop"].Should().Be(50);
        summary.BySource["Pixel"].Should().Be(75);
        summary.BySource["S2S"].Should().Be(25);
    }

    [Fact]
    public void CreativeImpressionCount_HasCorrectValues()
    {
        var count = new CreativeImpressionCount
        {
            CreativeId = "creative_banner_728x90_v1",
            Count = 1500
        };

        count.CreativeId.Should().Be("creative_banner_728x90_v1");
        count.Count.Should().Be(1500);
    }

    [Theory]
    [InlineData("Pixel")]
    [InlineData("S2S")]
    public void Impression_AcceptsValidSources(string source)
    {
        var impression = new Impression { IngestSource = source };
        impression.IngestSource.Should().Be(source);
    }

    [Theory]
    [InlineData("Desktop")]
    [InlineData("Mobile")]
    [InlineData("Tablet")]
    [InlineData("Unknown")]
    public void Impression_AcceptsValidDeviceTypes(string deviceType)
    {
        var impression = new Impression { DeviceType = deviceType };
        impression.DeviceType.Should().Be(deviceType);
    }

    [Fact]
    public void ExposedPanelistResult_DefaultValues()
    {
        var result = new ExposedPanelistResult();

        result.PanelistId.Should().Be(string.Empty);
        result.ImpressionCount.Should().Be(0);
    }

    [Fact]
    public void ExposedPanelistResult_CanSetProperties()
    {
        var result = new ExposedPanelistResult
        {
            PanelistId = "panelist_001",
            ImpressionCount = 5
        };

        result.PanelistId.Should().Be("panelist_001");
        result.ImpressionCount.Should().Be(5);
    }

    [Fact]
    public void ExposedPanelistsResponse_DefaultValues()
    {
        var response = new ExposedPanelistsResponse();

        response.CampaignId.Should().Be(string.Empty);
        response.MinImpressions.Should().Be(0);
        response.Panelists.Should().BeEmpty();
        response.TotalExposedPanelists.Should().Be(0);
    }

    [Fact]
    public void ExposedPanelistsResponse_CanPopulate()
    {
        var response = new ExposedPanelistsResponse
        {
            CampaignId = "campaign_123",
            MinImpressions = 3,
            Panelists = new List<ExposedPanelistResult>
            {
                new() { PanelistId = "p1", ImpressionCount = 5 },
                new() { PanelistId = "p2", ImpressionCount = 3 }
            },
            TotalExposedPanelists = 2
        };

        response.CampaignId.Should().Be("campaign_123");
        response.MinImpressions.Should().Be(3);
        response.Panelists.Should().HaveCount(2);
        response.TotalExposedPanelists.Should().Be(2);
        response.Panelists[0].ImpressionCount.Should().Be(5);
    }
}
