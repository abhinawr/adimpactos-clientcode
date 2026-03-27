using Xunit;
using FluentAssertions;
using AdImpactOs.Campaign.Models;

namespace AdImpactOs.Campaign.Tests;

public class CampaignModelTests
{
    [Fact]
    public void Campaign_DefaultValues_AreSetCorrectly()
    {
        var campaign = new Models.Campaign();

        campaign.Id.Should().Be(string.Empty);
        campaign.CampaignId.Should().Be(string.Empty);
        campaign.CampaignName.Should().Be(string.Empty);
        campaign.Status.Should().Be("Draft");
        campaign.Creatives.Should().BeEmpty();
        campaign.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        campaign.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Campaign_CanSetAllProperties()
    {
        var now = DateTime.UtcNow;
        var campaign = new Models.Campaign
        {
            Id = "c1",
            CampaignId = "campaign_123",
            CampaignName = "Test Campaign",
            Advertiser = "Acme Corp",
            Industry = "Technology",
            StartDate = now,
            EndDate = now.AddDays(30),
            Budget = 50000m,
            Status = "Active"
        };

        campaign.CampaignId.Should().Be("campaign_123");
        campaign.Advertiser.Should().Be("Acme Corp");
        campaign.Industry.Should().Be("Technology");
        campaign.Budget.Should().Be(50000m);
    }

    [Fact]
    public void TargetAudience_DefaultValues_AreEmptyLists()
    {
        var audience = new TargetAudience();

        audience.AgeRange.Should().BeEmpty();
        audience.Gender.Should().BeEmpty();
        audience.Interests.Should().BeEmpty();
        audience.Countries.Should().BeEmpty();
    }

    [Fact]
    public void TargetAudience_CanBePopulated()
    {
        var audience = new TargetAudience
        {
            AgeRange = new List<string> { "18-24", "25-34" },
            Gender = new List<string> { "M", "F" },
            Interests = new List<string> { "sports", "tech" },
            Countries = new List<string> { "US", "UK" }
        };

        audience.AgeRange.Should().HaveCount(2);
        audience.Countries.Should().Contain("US");
    }

    [Fact]
    public void Creative_HasAllProperties()
    {
        var creative = new Creative
        {
            CreativeId = "cr1",
            Format = "display",
            Size = "728x90",
            Message = "Buy now!",
            VariantName = "A"
        };

        creative.CreativeId.Should().Be("cr1");
        creative.Format.Should().Be("display");
        creative.Size.Should().Be("728x90");
    }

    [Fact]
    public void CampaignKpis_HasTargetMetrics()
    {
        var kpis = new CampaignKpis
        {
            TargetImpressions = 1000000,
            TargetReach = 500000,
            TargetLift = 10.0
        };

        kpis.TargetImpressions.Should().Be(1000000);
        kpis.TargetReach.Should().Be(500000);
        kpis.TargetLift.Should().Be(10.0);
    }

    [Fact]
    public void ActualMetrics_HasMeasuredValues()
    {
        var metrics = new ActualMetrics
        {
            Impressions = 750000,
            Reach = 300000,
            AverageLift = 8.5
        };

        metrics.Impressions.Should().Be(750000);
        metrics.Reach.Should().Be(300000);
        metrics.AverageLift.Should().Be(8.5);
    }

    [Fact]
    public void CreateCampaignRequest_DefaultValues()
    {
        var request = new CreateCampaignRequest();

        request.CampaignName.Should().Be(string.Empty);
        request.Advertiser.Should().Be(string.Empty);
        request.Creatives.Should().BeEmpty();
    }

    [Fact]
    public void UpdateCampaignRequest_AllFieldsOptional()
    {
        var request = new UpdateCampaignRequest();

        request.CampaignName.Should().BeNull();
        request.Budget.Should().BeNull();
        request.EndDate.Should().BeNull();
        request.Status.Should().BeNull();
    }

    [Fact]
    public void UpdateCampaignMetricsRequest_HasMetricFields()
    {
        var request = new UpdateCampaignMetricsRequest
        {
            Impressions = 5000,
            Reach = 2000,
            AverageLift = 12.5
        };

        request.Impressions.Should().Be(5000);
        request.Reach.Should().Be(2000);
        request.AverageLift.Should().Be(12.5);
    }

    [Theory]
    [InlineData(CampaignStatus.Draft)]
    [InlineData(CampaignStatus.Active)]
    [InlineData(CampaignStatus.Completed)]
    [InlineData(CampaignStatus.Scheduled)]
    [InlineData(CampaignStatus.Paused)]
    [InlineData(CampaignStatus.Archived)]
    public void CampaignStatus_HasExpectedValues(CampaignStatus status)
    {
        Enum.IsDefined(typeof(CampaignStatus), status).Should().BeTrue();
    }
}
