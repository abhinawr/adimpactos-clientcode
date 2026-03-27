using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AdImpactOs.PanelistAPI.Controllers;
using AdImpactOs.PanelistAPI.Services;
using AdImpactOs.PanelistAPI.Models;

namespace AdImpactOs.PanelistAPI.Tests;

public class PanelistsControllerTests
{
    private readonly Mock<PanelistService> _mockService;
    private readonly Mock<ILogger<PanelistsController>> _mockLogger;
    private readonly PanelistsController _controller;

    public PanelistsControllerTests()
    {
        _mockService = new Mock<PanelistService>(
            Mock.Of<Microsoft.Azure.Cosmos.CosmosClient>(),
            Mock.Of<ILogger<PanelistService>>(),
            Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>());
        
        _mockLogger = new Mock<ILogger<PanelistsController>>();
        _controller = new PanelistsController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreatePanelist_ReturnsBadRequest_WhenEmailIsMissing()
    {
        // Arrange
        var request = new CreatePanelistRequest { Email = "" };

        // Act
        var result = await _controller.CreatePanelist(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreatePanelist_ReturnsCreatedResult_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreatePanelistRequest
        {
            Email = "test@example.com",
            ConsentGiven = true
        };

        var createdPanelist = new Panelist
        {
            Id = "panelist-123",
            Email = request.Email,
            ConsentGiven = true
        };

        _mockService
            .Setup(s => s.CreatePanelistAsync(request))
            .ReturnsAsync(createdPanelist);

        // Act
        var result = await _controller.CreatePanelist(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Value.Should().Be(createdPanelist);
    }

    [Fact]
    public async Task GetPanelistById_ReturnsNotFound_WhenPanelistDoesNotExist()
    {
        // Arrange
        var panelistId = "non-existent-id";
        _mockService
            .Setup(s => s.GetPanelistByIdAsync(panelistId))
            .ReturnsAsync((Panelist?)null);

        // Act
        var result = await _controller.GetPanelistById(panelistId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPanelistById_ReturnsOk_WhenPanelistExists()
    {
        // Arrange
        var panelistId = "panelist-123";
        var panelist = new Panelist
        {
            Id = panelistId,
            Email = "test@example.com"
        };

        _mockService
            .Setup(s => s.GetPanelistByIdAsync(panelistId))
            .ReturnsAsync(panelist);

        // Act
        var result = await _controller.GetPanelistById(panelistId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().Be(panelist);
    }

    [Fact]
    public async Task UpdatePanelist_ReturnsNotFound_WhenPanelistDoesNotExist()
    {
        // Arrange
        var panelistId = "non-existent-id";
        var updateRequest = new UpdatePanelistRequest { Email = "new@example.com" };

        _mockService
            .Setup(s => s.UpdatePanelistAsync(panelistId, updateRequest))
            .ReturnsAsync((Panelist?)null);

        // Act
        var result = await _controller.UpdatePanelist(panelistId, updateRequest);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdatePanelist_ReturnsOk_WhenUpdateIsSuccessful()
    {
        // Arrange
        var panelistId = "panelist-123";
        var updateRequest = new UpdatePanelistRequest { Email = "new@example.com" };
        var updatedPanelist = new Panelist
        {
            Id = panelistId,
            Email = "new@example.com"
        };

        _mockService
            .Setup(s => s.UpdatePanelistAsync(panelistId, updateRequest))
            .ReturnsAsync(updatedPanelist);

        // Act
        var result = await _controller.UpdatePanelist(panelistId, updateRequest);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().Be(updatedPanelist);
    }

    [Fact]
    public async Task CheckConsent_ReturnsNotFound_WhenPanelistDoesNotExist()
    {
        // Arrange
        var panelistId = "non-existent-id";
        _mockService
            .Setup(s => s.CheckConsentAsync(panelistId))
            .ReturnsAsync(false);
        _mockService
            .Setup(s => s.GetPanelistByIdAsync(panelistId))
            .ReturnsAsync((Panelist?)null);

        // Act
        var result = await _controller.CheckConsent(panelistId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CheckConsent_ReturnsOk_WithConsentStatus()
    {
        // Arrange
        var panelistId = "panelist-123";
        var panelist = new Panelist
        {
            Id = panelistId,
            ConsentGiven = true,
            ConsentTimestamp = DateTime.UtcNow
        };

        _mockService
            .Setup(s => s.CheckConsentAsync(panelistId))
            .ReturnsAsync(true);
        _mockService
            .Setup(s => s.GetPanelistByIdAsync(panelistId))
            .ReturnsAsync(panelist);

        // Act
        var result = await _controller.CheckConsent(panelistId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeletePanelist_ReturnsNotFound_WhenPanelistDoesNotExist()
    {
        // Arrange
        var panelistId = "non-existent-id";
        _mockService
            .Setup(s => s.DeletePanelistAsync(panelistId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeletePanelist(panelistId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeletePanelist_ReturnsNoContent_WhenDeletionIsSuccessful()
    {
        // Arrange
        var panelistId = "panelist-123";
        _mockService
            .Setup(s => s.DeletePanelistAsync(panelistId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeletePanelist(panelistId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetAllPanelists_ReturnsOk_WithPanelistList()
    {
        // Arrange
        var panelists = new List<Panelist>
        {
            new Panelist { Id = "panelist-1", Email = "test1@example.com" },
            new Panelist { Id = "panelist-2", Email = "test2@example.com" }
        };

        _mockService
            .Setup(s => s.GetAllPanelistsAsync(It.IsAny<int>(), It.IsAny<string?>()))
            .ReturnsAsync(panelists);

        // Act
        var result = await _controller.GetAllPanelists();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var returnedPanelists = okResult.Value as List<Panelist>;
        returnedPanelists.Should().HaveCount(2);
    }
}
