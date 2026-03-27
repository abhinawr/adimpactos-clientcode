using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using AdImpactOs.PanelistAPI.Migration;
using System.Collections.ObjectModel;

namespace AdImpactOs.PanelistAPI.Tests;

public class PanelistDbMigrationTests
{
    private readonly Mock<CosmosClient> _mockCosmosClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<PanelistDbMigration>> _mockLogger;

    public PanelistDbMigrationTests()
    {
        _mockCosmosClient = new Mock<CosmosClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<PanelistDbMigration>>();

        _mockConfiguration.Setup(x => x["CosmosDb:DatabaseName"]).Returns("TestDB");
        _mockConfiguration.Setup(x => x["CosmosDb:ContainerName"]).Returns("TestContainer");
    }

    [Fact]
    public void PanelistDbMigration_CanBeInstantiated()
    {
        // Act
        var migration = new PanelistDbMigration(
            _mockCosmosClient.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Assert
        migration.Should().NotBeNull();
    }

    [Fact]
    public async Task RunMigrationAsync_CreatesDatabase()
    {
        // Arrange
        var mockDatabase = new Mock<Database>();
        var mockDatabaseResponse = new Mock<DatabaseResponse>();
        mockDatabaseResponse.Setup(x => x.StatusCode).Returns(System.Net.HttpStatusCode.Created);
        mockDatabaseResponse.Setup(x => x.Database).Returns(mockDatabase.Object);

        _mockCosmosClient
            .Setup(x => x.CreateDatabaseIfNotExistsAsync(
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDatabaseResponse.Object);

        _mockCosmosClient
            .Setup(x => x.GetDatabase(It.IsAny<string>()))
            .Returns(mockDatabase.Object);

        var mockContainerResponse = new Mock<ContainerResponse>();
        mockContainerResponse.Setup(x => x.StatusCode).Returns(System.Net.HttpStatusCode.Created);
        
        mockDatabase
            .Setup(x => x.CreateContainerIfNotExistsAsync(
                It.IsAny<ContainerProperties>(),
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContainerResponse.Object);

        var migration = new PanelistDbMigration(
            _mockCosmosClient.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        await migration.RunMigrationAsync();

        // Assert
        _mockCosmosClient.Verify(
            x => x.CreateDatabaseIfNotExistsAsync(
                "TestDB",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunMigrationAsync_CreatesContainer()
    {
        // Arrange
        var mockDatabase = new Mock<Database>();
        var mockDatabaseResponse = new Mock<DatabaseResponse>();
        mockDatabaseResponse.Setup(x => x.StatusCode).Returns(System.Net.HttpStatusCode.OK);
        mockDatabaseResponse.Setup(x => x.Database).Returns(mockDatabase.Object);

        _mockCosmosClient
            .Setup(x => x.CreateDatabaseIfNotExistsAsync(
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDatabaseResponse.Object);

        _mockCosmosClient
            .Setup(x => x.GetDatabase(It.IsAny<string>()))
            .Returns(mockDatabase.Object);

        var mockContainerResponse = new Mock<ContainerResponse>();
        mockContainerResponse.Setup(x => x.StatusCode).Returns(System.Net.HttpStatusCode.Created);
        
        ContainerProperties? capturedProperties = null;
        mockDatabase
            .Setup(x => x.CreateContainerIfNotExistsAsync(
                It.IsAny<ContainerProperties>(),
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<ContainerProperties, int?, RequestOptions, CancellationToken>(
                (props, throughput, opts, ct) => capturedProperties = props)
            .ReturnsAsync(mockContainerResponse.Object);

        var migration = new PanelistDbMigration(
            _mockCosmosClient.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        await migration.RunMigrationAsync();

        // Assert
        capturedProperties.Should().NotBeNull();
        capturedProperties!.Id.Should().Be("TestContainer");
        capturedProperties.PartitionKeyPath.Should().Be("/id");
    }

    [Fact]
    public async Task RunMigrationAsync_CreatesCompositeIndexes()
    {
        // Arrange
        var mockDatabase = new Mock<Database>();
        var mockDatabaseResponse = new Mock<DatabaseResponse>();
        mockDatabaseResponse.Setup(x => x.StatusCode).Returns(System.Net.HttpStatusCode.OK);

        _mockCosmosClient
            .Setup(x => x.CreateDatabaseIfNotExistsAsync(
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockDatabaseResponse.Object);

        _mockCosmosClient
            .Setup(x => x.GetDatabase(It.IsAny<string>()))
            .Returns(mockDatabase.Object);

        var mockContainerResponse = new Mock<ContainerResponse>();
        mockContainerResponse.Setup(x => x.StatusCode).Returns(System.Net.HttpStatusCode.Created);
        
        ContainerProperties? capturedProperties = null;
        mockDatabase
            .Setup(x => x.CreateContainerIfNotExistsAsync(
                It.IsAny<ContainerProperties>(),
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<ContainerProperties, int?, RequestOptions, CancellationToken>(
                (props, throughput, opts, ct) => capturedProperties = props)
            .ReturnsAsync(mockContainerResponse.Object);

        var migration = new PanelistDbMigration(
            _mockCosmosClient.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        await migration.RunMigrationAsync();

        // Assert
        capturedProperties.Should().NotBeNull();
        capturedProperties!.IndexingPolicy.Should().NotBeNull();
        capturedProperties.IndexingPolicy.CompositeIndexes.Should().HaveCount(2);
        
        // Verify consent + isActive composite index
        var consentIndex = capturedProperties.IndexingPolicy.CompositeIndexes[0];
        consentIndex.Should().HaveCount(2);
        consentIndex[0].Path.Should().Be("/consentGiven");
        consentIndex[1].Path.Should().Be("/isActive");
        
        // Verify country + createdAt composite index
        var countryIndex = capturedProperties.IndexingPolicy.CompositeIndexes[1];
        countryIndex.Should().HaveCount(2);
        countryIndex[0].Path.Should().Be("/country");
        countryIndex[1].Path.Should().Be("/createdAt");
    }
}
