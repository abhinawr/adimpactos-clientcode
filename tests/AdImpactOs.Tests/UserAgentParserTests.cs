using Xunit;
using FluentAssertions;
using AdImpactOs.Services;

namespace AdImpactOs.Tests;

public class UserAgentParserTests
{
    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36", "Desktop")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X)", "Mobile")]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 14_0 like Mac OS X)", "Tablet")]
    [InlineData("Mozilla/5.0 (Android 11; Mobile; rv:68.0)", "Mobile")]
    [InlineData("Mozilla/5.0 (Linux; Android 11; Tablet)", "Tablet")]
    [InlineData(null, "Unknown")]
    [InlineData("", "Unknown")]
    public void ParseDeviceType_ReturnsCorrectType(string userAgent, string expectedDeviceType)
    {
        // Act
        var deviceType = UserAgentParser.ParseDeviceType(userAgent);

        // Assert
        deviceType.Should().Be(expectedDeviceType);
    }

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/91.0", "Chrome")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/91.0", "Edge")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 Safari/605.1.15", "Safari")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0", "Firefox")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko", "IE")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 OPR/77.0", "Opera")]
    [InlineData(null, "Unknown")]
    [InlineData("", "Unknown")]
    public void ParseBrowser_ReturnsCorrectBrowser(string userAgent, string expectedBrowser)
    {
        // Act
        var browser = UserAgentParser.ParseBrowser(userAgent);

        // Assert
        browser.Should().Be(expectedBrowser);
    }

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64)", "Windows")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)", "MacOS")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X)", "iOS")]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 14_0 like Mac OS X)", "iOS")]
    [InlineData("Mozilla/5.0 (Linux; Android 11)", "Android")]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64)", "Linux")]
    [InlineData(null, "Unknown")]
    [InlineData("", "Unknown")]
    public void ParseOS_ReturnsCorrectOS(string userAgent, string expectedOS)
    {
        // Act
        var os = UserAgentParser.ParseOS(userAgent);

        // Assert
        os.Should().Be(expectedOS);
    }

    [Fact]
    public void ParseDeviceType_IsCaseInsensitive()
    {
        // Arrange
        var ua1 = "Mozilla/5.0 (IPHONE; CPU iPhone OS 14_0)";
        var ua2 = "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0)";

        // Act
        var device1 = UserAgentParser.ParseDeviceType(ua1);
        var device2 = UserAgentParser.ParseDeviceType(ua2);

        // Assert
        device1.Should().Be("Mobile");
        device2.Should().Be("Mobile");
    }
}
