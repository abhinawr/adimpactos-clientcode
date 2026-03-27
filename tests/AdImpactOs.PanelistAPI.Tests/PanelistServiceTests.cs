using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using AdImpactOs.PanelistAPI.Services;
using AdImpactOs.PanelistAPI.Models;

namespace AdImpactOs.PanelistAPI.Tests;

public class PanelistServiceTests
{
    private readonly Mock<CosmosClient> _mockCosmosClient;
    private readonly Mock<Container> _mockContainer;
    private readonly Mock<ILogger<PanelistService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public PanelistServiceTests()
    {
        _mockCosmosClient = new Mock<CosmosClient>();
        _mockContainer = new Mock<Container>();
        _mockLogger = new Mock<ILogger<PanelistService>>();
        _mockConfiguration = new Mock<IConfiguration>();

        _mockConfiguration.Setup(c => c["CosmosDb:DatabaseName"]).Returns("AdImpactOsDB");
        _mockConfiguration.Setup(c => c["CosmosDb:ContainerName"]).Returns("Panelists");

        _mockCosmosClient
            .Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(_mockContainer.Object);
    }

    [Fact]
    public void PanelistService_Constructor_InitializesCorrectly()
    {
        // Act
        var service = new PanelistService(_mockCosmosClient.Object, _mockLogger.Object, _mockConfiguration.Object);

        // Assert
        service.Should().NotBeNull();
        _mockCosmosClient.Verify(c => c.GetContainer("AdImpactOsDB", "Panelists"), Times.Once);
    }

    [Fact]
    public async Task CreatePanelistAsync_CreatesNewPanelist_WithGeneratedId()
    {
        // Arrange
        var request = new CreatePanelistRequest
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            ConsentGiven = true
        };

        var createdPanelist = new Panelist
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            ConsentGiven = true
        };

        var mockResponse = new Mock<ItemResponse<Panelist>>();
        mockResponse.Setup(r => r.Resource).Returns(createdPanelist);

        _mockContainer
            .Setup(c => c.CreateItemAsync(
                It.IsAny<Panelist>(),
                It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        var service = new PanelistService(_mockCosmosClient.Object, _mockLogger.Object, _mockConfiguration.Object);

        // Act
        var result = await service.CreatePanelistAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(request.Email);
        result.ConsentGiven.Should().BeTrue();
    }

    [Fact]
    public async Task GetPanelistByIdAsync_ReturnsNull_WhenPanelistNotFound()
    {
        // Arrange
        var panelistId = "non-existent-id";

        _mockContainer
            .Setup(c => c.ReadItemAsync<Panelist>(
                panelistId,
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 0, "", 0));

        var service = new PanelistService(_mockCosmosClient.Object, _mockLogger.Object, _mockConfiguration.Object);

        // Act
        var result = await service.GetPanelistByIdAsync(panelistId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckConsentAsync_ReturnsFalse_WhenPanelistNotFound()
    {
        // Arrange
        var panelistId = "non-existent-id";

        _mockContainer
            .Setup(c => c.ReadItemAsync<Panelist>(
                panelistId,
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 0, "", 0));

        var service = new PanelistService(_mockCosmosClient.Object, _mockLogger.Object, _mockConfiguration.Object);

        // Act
        var result = await service.CheckConsentAsync(panelistId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePanelistAsync_ReturnsNull_WhenPanelistNotFound()
    {
        // Arrange
        var panelistId = "non-existent-id";
        var updateRequest = new UpdatePanelistRequest { Email = "new@example.com" };

        _mockContainer
            .Setup(c => c.ReadItemAsync<Panelist>(
                panelistId,
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 0, "", 0));

        var service = new PanelistService(_mockCosmosClient.Object, _mockLogger.Object, _mockConfiguration.Object);

        // Act
        var result = await service.UpdatePanelistAsync(panelistId, updateRequest);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeletePanelistAsync_ReturnsFalse_WhenPanelistNotFound()
    {
        // Arrange
        var panelistId = "non-existent-id";

        _mockContainer
            .Setup(c => c.ReadItemAsync<Panelist>(
                panelistId,
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 0, "", 0));

        var service = new PanelistService(_mockCosmosClient.Object, _mockLogger.Object, _mockConfiguration.Object);

        // Act
        var result = await service.DeletePanelistAsync(panelistId);

        // Assert
        result.Should().BeFalse();
    }
}
