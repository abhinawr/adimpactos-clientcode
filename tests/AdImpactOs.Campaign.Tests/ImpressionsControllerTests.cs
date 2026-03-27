using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AdImpactOs.Campaign.Controllers;
using AdImpactOs.Campaign.Services;
using AdImpactOs.Campaign.Models;

namespace AdImpactOs.Campaign.Tests;

public class ImpressionsControllerTests
{
    private readonly Mock<ImpressionService> _mockService;
    private readonly Mock<ILogger<ImpressionsController>> _mockLogger;
    private readonly ImpressionsController _controller;

    public ImpressionsControllerTests()
    {
        _mockService = new Mock<ImpressionService>(
            Mock.Of<Microsoft.Azure.Cosmos.CosmosClient>(),
            Mock.Of<ILogger<ImpressionService>>(),
            Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>());

        _mockLogger = new Mock<ILogger<ImpressionsController>>();
        _controller = new ImpressionsController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task RecordImpression_ReturnsBadRequest_WhenCampaignIdMissing()
    {
        var impression = new Impression { ImpressionId = "imp_001", CampaignId = "" };

        var result = await _controller.RecordImpression(impression);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RecordImpression_ReturnsBadRequest_WhenImpressionIdMissing()
    {
        var impression = new Impression { ImpressionId = "", CampaignId = "campaign_test" };

        var result = await _controller.RecordImpression(impression);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RecordImpression_ReturnsCreated_WhenValid()
    {
        var impression = new Impression
        {
            ImpressionId = "imp_001",
            CampaignId = "campaign_summer_beverage_2024",
            CreativeId = "creative_banner_728x90_v1",
            PanelistId = "panelist_001",
            DeviceType = "Desktop",
            Country = "US",
            IsBot = false,
            IngestSource = "Pixel"
        };

        _mockService.Setup(s => s.RecordImpressionAsync(impression)).ReturnsAsync(impression);

        var result = await _controller.RecordImpression(impression);

        result.Result.Should().BeOfType<CreatedResult>();
    }

    [Fact]
    public async Task GetAllImpressions_ReturnsOk_WithList()
    {
        var impressions = new List<Impression>
        {
            new() { ImpressionId = "imp_001", CampaignId = "c1" },
            new() { ImpressionId = "imp_002", CampaignId = "c2" }
        };

        _mockService.Setup(s => s.GetAllImpressionsAsync(500)).ReturnsAsync(impressions);

        var result = await _controller.GetAllImpressions();

        result.Result.Should().BeOfType<OkObjectResult>();
        var list = ((OkObjectResult)result.Result!).Value as List<Impression>;
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCampaignImpressions_ReturnsOk()
    {
        var impressions = new List<Impression>
        {
            new() { ImpressionId = "imp_001", CampaignId = "campaign_test" }
        };

        _mockService.Setup(s => s.GetImpressionsByCampaignAsync("campaign_test", 100))
            .ReturnsAsync(impressions);

        var result = await _controller.GetCampaignImpressions("campaign_test");

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCampaignSummary_ReturnsOk_WithSummary()
    {
        var summary = new ImpressionSummary
        {
            CampaignId = "campaign_test",
            TotalImpressions = 100,
            ValidImpressions = 92,
            BotImpressions = 8,
            UniquePanelists = 45
        };

        _mockService.Setup(s => s.GetCampaignImpressionSummaryAsync("campaign_test"))
            .ReturnsAsync(summary);

        var result = await _controller.GetCampaignSummary("campaign_test");

        result.Result.Should().BeOfType<OkObjectResult>();
        var returned = ((OkObjectResult)result.Result!).Value as ImpressionSummary;
        returned!.TotalImpressions.Should().Be(100);
        returned.ValidImpressions.Should().Be(92);
    }

    [Fact]
    public async Task GetAllSummaries_ReturnsOk()
    {
        var summaries = new Dictionary<string, ImpressionSummary>
        {
            ["campaign_1"] = new ImpressionSummary { CampaignId = "campaign_1", TotalImpressions = 50 },
            ["campaign_2"] = new ImpressionSummary { CampaignId = "campaign_2", TotalImpressions = 75 }
        };

        _mockService.Setup(s => s.GetAllCampaignSummariesAsync()).ReturnsAsync(summaries);

        var result = await _controller.GetAllSummaries();

        result.Result.Should().BeOfType<OkObjectResult>();
        var returned = ((OkObjectResult)result.Result!).Value as Dictionary<string, ImpressionSummary>;
        returned.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetExposedPanelists_ReturnsOk_WithPanelists()
    {
        var response = new ExposedPanelistsResponse
        {
            CampaignId = "campaign_test",
            MinImpressions = 1,
            Panelists = new List<ExposedPanelistResult>
            {
                new() { PanelistId = "p1", ImpressionCount = 5 },
                new() { PanelistId = "p2", ImpressionCount = 3 }
            },
            TotalExposedPanelists = 2
        };

        _mockService.Setup(s => s.GetExposedPanelistIdsAsync("campaign_test", 1, It.IsAny<int>()))
            .ReturnsAsync(response);

        var result = await _controller.GetExposedPanelists("campaign_test");

        result.Result.Should().BeOfType<OkObjectResult>();
        var returned = ((OkObjectResult)result.Result!).Value as ExposedPanelistsResponse;
        returned!.TotalExposedPanelists.Should().Be(2);
        returned.Panelists.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetExposedPanelists_WithMinImpressions_PassesParameter()
    {
        var response = new ExposedPanelistsResponse
        {
            CampaignId = "campaign_test",
            MinImpressions = 3,
            Panelists = new List<ExposedPanelistResult>
            {
                new() { PanelistId = "p1", ImpressionCount = 5 }
            },
            TotalExposedPanelists = 1
        };

        _mockService.Setup(s => s.GetExposedPanelistIdsAsync("campaign_test", 3, It.IsAny<int>()))
            .ReturnsAsync(response);

        var result = await _controller.GetExposedPanelists("campaign_test", 3);

        result.Result.Should().BeOfType<OkObjectResult>();
        var returned = ((OkObjectResult)result.Result!).Value as ExposedPanelistsResponse;
        returned!.MinImpressions.Should().Be(3);
        returned.TotalExposedPanelists.Should().Be(1);
    }

    [Fact]
    public async Task GetExposedPanelists_ReturnsBadRequest_WhenMinImpressionsInvalid()
    {
        var result = await _controller.GetExposedPanelists("campaign_test", 0);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
