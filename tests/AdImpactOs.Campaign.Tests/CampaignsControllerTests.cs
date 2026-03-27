using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AdImpactOs.Campaign.Controllers;
using AdImpactOs.Campaign.Services;
using AdImpactOs.Campaign.Models;

namespace AdImpactOs.Campaign.Tests;

public class CampaignsControllerTests
{
    private readonly Mock<CampaignService> _mockService;
    private readonly Mock<ILogger<CampaignsController>> _mockLogger;
    private readonly CampaignsController _controller;

    public CampaignsControllerTests()
    {
        _mockService = new Mock<CampaignService>(
            Mock.Of<Microsoft.Azure.Cosmos.CosmosClient>(),
            Mock.Of<ILogger<CampaignService>>(),
            Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>());

        _mockLogger = new Mock<ILogger<CampaignsController>>();
        _controller = new CampaignsController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateCampaign_ReturnsBadRequest_WhenNameIsMissing()
    {
        var request = new CreateCampaignRequest { CampaignName = "", Advertiser = "Acme" };
        var result = await _controller.CreateCampaign(request);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateCampaign_ReturnsBadRequest_WhenAdvertiserIsMissing()
    {
        var request = new CreateCampaignRequest { CampaignName = "Test", Advertiser = "" };
        var result = await _controller.CreateCampaign(request);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateCampaign_ReturnsCreated_WhenValid()
    {
        var request = new CreateCampaignRequest
        {
            CampaignName = "Test Campaign",
            Advertiser = "Acme Corp",
            Industry = "Tech",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Budget = 100000
        };

        var created = new Models.Campaign
        {
            Id = "campaign_123",
            CampaignId = "campaign_123",
            CampaignName = request.CampaignName,
            Advertiser = request.Advertiser
        };

        _mockService.Setup(s => s.CreateCampaignAsync(request)).ReturnsAsync(created);

        var result = await _controller.CreateCampaign(request);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.Value.Should().Be(created);
    }

    [Fact]
    public async Task GetCampaign_ReturnsNotFound_WhenMissing()
    {
        _mockService.Setup(s => s.GetCampaignAsync("missing")).ReturnsAsync((Models.Campaign?)null);
        var result = await _controller.GetCampaign("missing");
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetCampaign_ReturnsOk_WhenExists()
    {
        var campaign = new Models.Campaign { Id = "c1", CampaignId = "c1", CampaignName = "Test" };
        _mockService.Setup(s => s.GetCampaignAsync("c1")).ReturnsAsync(campaign);

        var result = await _controller.GetCampaign("c1");

        result.Result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result.Result!).Value.Should().Be(campaign);
    }

    [Fact]
    public async Task GetAllCampaigns_ReturnsOk_WithList()
    {
        var campaigns = new List<Models.Campaign>
        {
            new() { Id = "c1", CampaignName = "Campaign 1" },
            new() { Id = "c2", CampaignName = "Campaign 2" }
        };

        _mockService.Setup(s => s.GetAllCampaignsAsync(null, null)).ReturnsAsync(campaigns);

        var result = await _controller.GetAllCampaigns();

        result.Result.Should().BeOfType<OkObjectResult>();
        var list = ((OkObjectResult)result.Result!).Value as List<Models.Campaign>;
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActiveCampaigns_ReturnsOk()
    {
        var campaigns = new List<Models.Campaign>
        {
            new() { Id = "c1", Status = "Active" }
        };

        _mockService.Setup(s => s.GetActiveCampaignsAsync()).ReturnsAsync(campaigns);

        var result = await _controller.GetActiveCampaigns();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateCampaign_ReturnsNotFound_WhenMissing()
    {
        var request = new UpdateCampaignRequest { CampaignName = "Updated" };
        _mockService.Setup(s => s.UpdateCampaignAsync("missing", request))
            .ThrowsAsync(new InvalidOperationException("not found"));

        var result = await _controller.UpdateCampaign("missing", request);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateCampaign_ReturnsOk_WhenSuccessful()
    {
        var request = new UpdateCampaignRequest { CampaignName = "Updated" };
        var updated = new Models.Campaign { Id = "c1", CampaignName = "Updated" };
        _mockService.Setup(s => s.UpdateCampaignAsync("c1", request)).ReturnsAsync(updated);

        var result = await _controller.UpdateCampaign("c1", request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateCampaignStatus_ReturnsNotFound_WhenMissing()
    {
        _mockService.Setup(s => s.UpdateCampaignStatusAsync("missing", "Active"))
            .ThrowsAsync(new InvalidOperationException("not found"));

        var result = await _controller.UpdateCampaignStatus("missing", "Active");

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateCampaignStatus_ReturnsOk_WhenSuccessful()
    {
        var campaign = new Models.Campaign { Id = "c1", Status = "Active" };
        _mockService.Setup(s => s.UpdateCampaignStatusAsync("c1", "Active")).ReturnsAsync(campaign);

        var result = await _controller.UpdateCampaignStatus("c1", "Active");

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateCampaignMetrics_ReturnsNotFound_WhenMissing()
    {
        var request = new UpdateCampaignMetricsRequest { Impressions = 1000 };
        _mockService.Setup(s => s.UpdateCampaignMetricsAsync("missing", request))
            .ThrowsAsync(new InvalidOperationException("not found"));

        var result = await _controller.UpdateCampaignMetrics("missing", request);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateCampaignMetrics_ReturnsOk_WhenSuccessful()
    {
        var request = new UpdateCampaignMetricsRequest { Impressions = 5000, Reach = 1000, AverageLift = 15.0 };
        var campaign = new Models.Campaign { Id = "c1" };
        _mockService.Setup(s => s.UpdateCampaignMetricsAsync("c1", request)).ReturnsAsync(campaign);

        var result = await _controller.UpdateCampaignMetrics("c1", request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteCampaign_ReturnsNotFound_WhenMissing()
    {
        _mockService.Setup(s => s.DeleteCampaignAsync("missing")).ReturnsAsync(false);

        var result = await _controller.DeleteCampaign("missing");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteCampaign_ReturnsNoContent_WhenSuccessful()
    {
        _mockService.Setup(s => s.DeleteCampaignAsync("c1")).ReturnsAsync(true);

        var result = await _controller.DeleteCampaign("c1");

        result.Should().BeOfType<NoContentResult>();
    }
}
