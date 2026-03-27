using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using AdImpactOs.PanelistAPI.Services;
using AdImpactOs.PanelistAPI.Models;

namespace AdImpactOs.PanelistAPI.Tests;

public class PanelistServiceHashingTests
{
    private readonly Mock<CosmosClient> _mockCosmosClient;
    private readonly Mock<Container> _mockContainer;
    private readonly Mock<ILogger<PanelistService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly PanelistService _service;

    public PanelistServiceHashingTests()
    {
        _mockCosmosClient = new Mock<CosmosClient>();
        _mockContainer = new Mock<Container>();
        _mockLogger = new Mock<ILogger<PanelistService>>();
        _mockConfiguration = new Mock<IConfiguration>();

        _mockConfiguration.Setup(x => x["CosmosDb:DatabaseName"]).Returns("TestDB");
        _mockConfiguration.Setup(x => x["CosmosDb:ContainerName"]).Returns("TestContainer");

        _mockCosmosClient
            .Setup(x => x.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(_mockContainer.Object);

        _service = new PanelistService(_mockCosmosClient.Object, _mockLogger.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task CreatePanelistAsync_HashesEmail()
    {
        // Arrange
        var request = new CreatePanelistRequest
        {
            Email = "Test@Example.COM",
            ConsentGdpr = true
        };

        Panelist? capturedPanelist = null;
        _mockContainer
            .Setup(x => x.CreateItemAsync(
                It.IsAny<Panelist>(),
                It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<Panelist, PartitionKey?, ItemRequestOptions?, CancellationToken>((p, pk, opts, ct) => capturedPanelist = p)
            .ReturnsAsync((Panelist p, PartitionKey? pk, ItemRequestOptions? opts, CancellationToken ct) =>
            {
                var mockResponse = new Mock<ItemResponse<Panelist>>();
                mockResponse.Setup(x => x.Resource).Returns(p);
                return mockResponse.Object;
            });

        // Act
        var result = await _service.CreatePanelistAsync(request);

        // Assert
        capturedPanelist.Should().NotBeNull();
        capturedPanelist!.HashedEmail.Should().NotBeNullOrEmpty();
        capturedPanelist.HashedEmail.Should().HaveLength(64); // SHA256 produces 64 hex chars
        
        // Verify normalization - should be same as lowercase
        var expectedHash = HashingService.HashEmail("test@example.com");
        capturedPanelist.HashedEmail.Should().Be(expectedHash);
    }

    [Fact]
    public async Task CreatePanelistAsync_HashesPhone()
    {
        // Arrange
        var request = new CreatePanelistRequest
        {
            Email = "test@example.com",
            Phone = "(123) 456-7890",
            ConsentGdpr = true
        };

        Panelist? capturedPanelist = null;
        _mockContainer
            .Setup(x => x.CreateItemAsync(
                It.IsAny<Panelist>(),
                It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<Panelist, PartitionKey?, ItemRequestOptions?, CancellationToken>((p, pk, opts, ct) => capturedPanelist = p)
            .ReturnsAsync((Panelist p, PartitionKey? pk, ItemRequestOptions? opts, CancellationToken ct) =>
            {
                var mockResponse = new Mock<ItemResponse<Panelist>>();
                mockResponse.Setup(x => x.Resource).Returns(p);
                return mockResponse.Object;
            });

        // Act
        var result = await _service.CreatePanelistAsync(request);

        // Assert
        capturedPanelist.Should().NotBeNull();
        capturedPanelist!.HashedPhone.Should().NotBeNullOrEmpty();
        capturedPanelist.HashedPhone.Should().HaveLength(64);
        
        // Verify phone normalization
        var expectedHash = HashingService.HashPhone("1234567890");
        capturedPanelist.HashedPhone.Should().Be(expectedHash);
    }

    [Fact]
    public async Task CreatePanelistAsync_CalculatesAgeRange()
    {
        // Arrange
        var request = new CreatePanelistRequest
        {
            Email = "test@example.com",
            Age = 30,
            ConsentGdpr = true
        };

        Panelist? capturedPanelist = null;
        _mockContainer
            .Setup(x => x.CreateItemAsync(
                It.IsAny<Panelist>(),
                It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<Panelist, PartitionKey?, ItemRequestOptions?, CancellationToken>((p, pk, opts, ct) => capturedPanelist = p)
            .ReturnsAsync((Panelist p, PartitionKey? pk, ItemRequestOptions? opts, CancellationToken ct) =>
            {
                var mockResponse = new Mock<ItemResponse<Panelist>>();
                mockResponse.Setup(x => x.Resource).Returns(p);
                return mockResponse.Object;
            });

        // Act
        var result = await _service.CreatePanelistAsync(request);

        // Assert
        capturedPanelist.Should().NotBeNull();
        capturedPanelist!.AgeRange.Should().Be("25-34");
        capturedPanelist.Age.Should().Be(30);
    }

    [Fact]
    public async Task CreatePanelistAsync_SetsLastActive()
    {
        // Arrange
        var request = new CreatePanelistRequest
        {
            Email = "test@example.com",
            ConsentGdpr = true
        };

        Panelist? capturedPanelist = null;
        _mockContainer
            .Setup(x => x.CreateItemAsync(
                It.IsAny<Panelist>(),
                It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<Panelist, PartitionKey?, ItemRequestOptions?, CancellationToken>((p, pk, opts, ct) => capturedPanelist = p)
            .ReturnsAsync((Panelist p, PartitionKey? pk, ItemRequestOptions? opts, CancellationToken ct) =>
            {
                var mockResponse = new Mock<ItemResponse<Panelist>>();
                mockResponse.Setup(x => x.Resource).Returns(p);
                return mockResponse.Object;
            });

        // Act
        var result = await _service.CreatePanelistAsync(request);

        // Assert
        capturedPanelist.Should().NotBeNull();
        capturedPanelist!.LastActive.Should().NotBeNull();
        capturedPanelist.LastActive.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreatePanelistAsync_SetsConsentGdpr()
    {
        // Arrange
        var request = new CreatePanelistRequest
        {
            Email = "test@example.com",
            ConsentGdpr = true,
            ConsentCcpa = false
        };

        Panelist? capturedPanelist = null;
        _mockContainer
            .Setup(x => x.CreateItemAsync(
                It.IsAny<Panelist>(),
                It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<Panelist, PartitionKey?, ItemRequestOptions?, CancellationToken>((p, pk, opts, ct) => capturedPanelist = p)
            .ReturnsAsync((Panelist p, PartitionKey? pk, ItemRequestOptions? opts, CancellationToken ct) =>
            {
                var mockResponse = new Mock<ItemResponse<Panelist>>();
                mockResponse.Setup(x => x.Resource).Returns(p);
                return mockResponse.Object;
            });

        // Act
        var result = await _service.CreatePanelistAsync(request);

        // Assert
        capturedPanelist.Should().NotBeNull();
        capturedPanelist!.ConsentGdpr.Should().BeTrue();
        capturedPanelist.ConsentCcpa.Should().BeFalse();
        capturedPanelist.ConsentGiven.Should().BeTrue(); // Should be true if any consent is given
    }

    [Fact]
    public async Task CreatePanelistAsync_InitializesPointsBalance()
    {
        // Arrange
        var request = new CreatePanelistRequest
        {
            Email = "test@example.com",
            ConsentGdpr = true
        };

        Panelist? capturedPanelist = null;
        _mockContainer
            .Setup(x => x.CreateItemAsync(
                It.IsAny<Panelist>(),
                It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<Panelist, PartitionKey?, ItemRequestOptions?, CancellationToken>((p, pk, opts, ct) => capturedPanelist = p)
            .ReturnsAsync((Panelist p, PartitionKey? pk, ItemRequestOptions? opts, CancellationToken ct) =>
            {
                var mockResponse = new Mock<ItemResponse<Panelist>>();
                mockResponse.Setup(x => x.Resource).Returns(p);
                return mockResponse.Object;
            });

        // Act
        var result = await _service.CreatePanelistAsync(request);

        // Assert
        capturedPanelist.Should().NotBeNull();
        capturedPanelist!.PointsBalance.Should().Be(0);
    }
}
