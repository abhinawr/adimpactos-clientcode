using Xunit;
using FluentAssertions;
using AdImpactOs.PanelistAPI.Services;

namespace AdImpactOs.PanelistAPI.Tests;

public class HashingServiceTests
{
    [Fact]
    public void HashEmail_ProducesConsistentHash()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var hash1 = HashingService.HashEmail(email);
        var hash2 = HashingService.HashEmail(email);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().NotBeNullOrEmpty();
        hash1.Length.Should().Be(64); // SHA256 produces 64 hex characters
    }

    [Fact]
    public void HashEmail_NormalizesToLowercase()
    {
        // Arrange
        var email1 = "Test@Example.COM";
        var email2 = "test@example.com";

        // Act
        var hash1 = HashingService.HashEmail(email1);
        var hash2 = HashingService.HashEmail(email2);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HashEmail_HandlesNullOrWhitespace(string email)
    {
        // Act
        var hash = HashingService.HashEmail(email);

        // Assert
        hash.Should().Be(string.Empty);
    }

    [Fact]
    public void HashPhone_ProducesConsistentHash()
    {
        // Arrange
        var phone = "1234567890";

        // Act
        var hash1 = HashingService.HashPhone(phone);
        var hash2 = HashingService.HashPhone(phone);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().NotBeNullOrEmpty();
        hash1.Length.Should().Be(64);
    }

    [Fact]
    public void HashPhone_NormalizesPhone()
    {
        // Arrange
        var phone1 = "(123) 456-7890";
        var phone2 = "123-456-7890";
        var phone3 = "1234567890";

        // Act
        var hash1 = HashingService.HashPhone(phone1);
        var hash2 = HashingService.HashPhone(phone2);
        var hash3 = HashingService.HashPhone(phone3);

        // Assert
        hash1.Should().Be(hash2);
        hash2.Should().Be(hash3);
    }

    [Theory]
    [InlineData(17, "<18")]
    [InlineData(18, "18-24")]
    [InlineData(24, "18-24")]
    [InlineData(25, "25-34")]
    [InlineData(34, "25-34")]
    [InlineData(35, "35-44")]
    [InlineData(44, "35-44")]
    [InlineData(45, "45-54")]
    [InlineData(54, "45-54")]
    [InlineData(55, "55-64")]
    [InlineData(64, "55-64")]
    [InlineData(65, "65+")]
    [InlineData(100, "65+")]
    public void GetAgeRange_ReturnsCorrectRange(int age, string expectedRange)
    {
        // Act
        var ageRange = HashingService.GetAgeRange(age);

        // Assert
        ageRange.Should().Be(expectedRange);
    }
}
