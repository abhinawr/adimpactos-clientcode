using System;
using AdImpactOs.Services;
using FluentAssertions;
using Xunit;

namespace AdImpactOs.Tests;

public class PixelServiceTests
{
    [Fact]
    public void GetTransparentGif_ReturnsValidGifBytes()
    {
        // Act
        var gifBytes = PixelService.GetTransparentGif();

        // Assert
        gifBytes.Should().NotBeNull();
        gifBytes.Length.Should().Be(43);
        gifBytes[0].Should().Be(0x47); // 'G'
        gifBytes[1].Should().Be(0x49); // 'I'
        gifBytes[2].Should().Be(0x46); // 'F'
    }

    [Fact]
    public void GenerateTrackingHash_ReturnsConsistentHash()
    {
        // Arrange
        var campaignId = "camp-001";
        var creativeId = "cre-002";
        var userId = "user-003";
        var timestamp = new DateTime(2026, 02, 02, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var hash1 = PixelService.GenerateTrackingHash(campaignId, creativeId, userId, timestamp);
        var hash2 = PixelService.GenerateTrackingHash(campaignId, creativeId, userId, timestamp);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Length.Should().Be(16);
    }

    [Fact]
    public void GenerateTrackingHash_ReturnsDifferentHashForDifferentInputs()
    {
        // Arrange
        var timestamp = new DateTime(2026, 02, 02, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var hash1 = PixelService.GenerateTrackingHash("camp-001", "cre-002", "user-003", timestamp);
        var hash2 = PixelService.GenerateTrackingHash("camp-999", "cre-002", "user-003", timestamp);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateTrackingHash_HandlesNullOrWhitespaceProperly(string input)
    {
        // Arrange
        var timestamp = new DateTime(2026, 02, 02, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var hash = PixelService.GenerateTrackingHash(input, input, input, timestamp);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Length.Should().Be(16);
    }
}
