using Xunit;
using FluentAssertions;
using AdImpactOs.PanelistAPI.Models;

namespace AdImpactOs.PanelistAPI.Tests;

public class PanelistModelTests
{
    [Fact]
    public void Panelist_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var panelist = new Panelist();

        // Assert
        panelist.Id.Should().Be(string.Empty);
        panelist.ConsentGiven.Should().BeFalse();
        panelist.ConsentGdpr.Should().BeFalse();
        panelist.ConsentCcpa.Should().BeFalse();
        panelist.IsActive.Should().BeTrue();
        panelist.PointsBalance.Should().Be(0);
        panelist.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        panelist.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Panelist_PartitionKey_ReturnsId()
    {
        // Arrange
        var panelist = new Panelist { Id = "test-id-123" };

        // Act
        var partitionKey = panelist.PartitionKey;

        // Assert
        partitionKey.Should().Be("test-id-123");
    }

    [Fact]
    public void CreatePanelistRequest_HasRequiredProperties()
    {
        // Arrange & Act
        var request = new CreatePanelistRequest
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            Age = 30,
            Gender = "M",
            Country = "US",
            ConsentGiven = true
        };

        // Assert
        request.Email.Should().Be("test@example.com");
        request.FirstName.Should().Be("John");
        request.LastName.Should().Be("Doe");
        request.Age.Should().Be(30);
        request.Gender.Should().Be("M");
        request.Country.Should().Be("US");
        request.ConsentGiven.Should().BeTrue();
    }

    [Fact]
    public void UpdatePanelistRequest_AllFieldsAreOptional()
    {
        // Arrange & Act
        var request = new UpdatePanelistRequest();

        // Assert
        request.Email.Should().BeNull();
        request.FirstName.Should().BeNull();
        request.LastName.Should().BeNull();
        request.Age.Should().BeNull();
        request.Gender.Should().BeNull();
        request.Country.Should().BeNull();
        request.ConsentGiven.Should().BeNull();
        request.IsActive.Should().BeNull();
    }

    [Fact]
    public void Panelist_ConsentTimestamp_IsSetWhenConsentGiven()
    {
        // Arrange & Act
        var panelist = new Panelist
        {
            ConsentGiven = true,
            ConsentTimestamp = DateTime.UtcNow
        };

        // Assert
        panelist.ConsentTimestamp.Should().NotBeNull();
        panelist.ConsentTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Panelist_HasHashedEmailAndPhone()
    {
        // Arrange & Act
        var panelist = new Panelist
        {
            HashedEmail = "hashed_email_value",
            HashedPhone = "hashed_phone_value"
        };

        // Assert
        panelist.HashedEmail.Should().Be("hashed_email_value");
        panelist.HashedPhone.Should().Be("hashed_phone_value");
    }

    [Fact]
    public void Panelist_HasAgeRangeField()
    {
        // Arrange & Act
        var panelist = new Panelist
        {
            Age = 30,
            AgeRange = "25-34"
        };

        // Assert
        panelist.AgeRange.Should().Be("25-34");
        panelist.Age.Should().Be(30);
    }

    [Fact]
    public void Panelist_HasHhIncomeBucket()
    {
        // Arrange & Act
        var panelist = new Panelist
        {
            HhIncomeBucket = "50K-75K"
        };

        // Assert
        panelist.HhIncomeBucket.Should().Be("50K-75K");
    }

    [Fact]
    public void Panelist_HasInterests()
    {
        // Arrange & Act
        var panelist = new Panelist
        {
            Interests = "sports,technology,music"
        };

        // Assert
        panelist.Interests.Should().Be("sports,technology,music");
    }

    [Fact]
    public void Panelist_HasGdprAndCcpaConsent()
    {
        // Arrange & Act
        var panelist = new Panelist
        {
            ConsentGdpr = true,
            ConsentCcpa = false
        };

        // Assert
        panelist.ConsentGdpr.Should().BeTrue();
        panelist.ConsentCcpa.Should().BeFalse();
    }

    [Fact]
    public void Panelist_HasLastActive()
    {
        // Arrange & Act
        var lastActive = DateTime.UtcNow.AddDays(-1);
        var panelist = new Panelist
        {
            LastActive = lastActive
        };

        // Assert
        panelist.LastActive.Should().BeCloseTo(lastActive, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Panelist_HasPointsBalance()
    {
        // Arrange & Act
        var panelist = new Panelist
        {
            PointsBalance = 500
        };

        // Assert
        panelist.PointsBalance.Should().Be(500);
    }

    [Theory]
    [InlineData("M")]
    [InlineData("F")]
    [InlineData("Other")]
    public void Panelist_Gender_AcceptsValidValues(string gender)
    {
        // Arrange & Act
        var panelist = new Panelist { Gender = gender };

        // Assert
        panelist.Gender.Should().Be(gender);
    }

    [Theory]
    [InlineData("Desktop")]
    [InlineData("Mobile")]
    [InlineData("Tablet")]
    public void Panelist_DeviceType_AcceptsValidValues(string deviceType)
    {
        // Arrange & Act
        var panelist = new Panelist { DeviceType = deviceType };

        // Assert
        panelist.DeviceType.Should().Be(deviceType);
    }

    [Theory]
    [InlineData("Chrome")]
    [InlineData("Firefox")]
    [InlineData("Safari")]
    [InlineData("Edge")]
    public void Panelist_Browser_AcceptsValidValues(string browser)
    {
        // Arrange & Act
        var panelist = new Panelist { Browser = browser };

        // Assert
        panelist.Browser.Should().Be(browser);
    }
}
