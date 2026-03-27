using Xunit;
using FluentAssertions;
using System.IO;
using System.Linq;

namespace AdImpactOs.Tests;

/// <summary>
/// Tests to verify Docker configuration and setup files
/// </summary>
public class DockerConfigurationTests
{
    [Fact]
    public void DockerCompose_Dev_FileExists()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "..", "docker-compose.dev.yml");

        // Act & Assert
        File.Exists(filePath).Should().BeTrue("docker-compose.dev.yml should exist for development");
    }

    [Fact]
    public void DockerCompose_Full_FileExists()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "..", "docker-compose.yml");

        // Act & Assert
        File.Exists(filePath).Should().BeTrue("docker-compose.yml should exist for full stack");
    }

    [Fact]
    public void Dockerfile_Functions_Exists()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "..", "src", "AdImpactOs", "Dockerfile");

        // Act & Assert
        File.Exists(filePath).Should().BeTrue("Dockerfile for Azure Functions should exist");
    }

    [Fact]
    public void Dockerfile_PanelistAPI_Exists()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "..", "src", "AdImpactOs.PanelistAPI", "Dockerfile");

        // Act & Assert
        File.Exists(filePath).Should().BeTrue("Dockerfile for Panelist API should exist");
    }

    [Fact]
    public void DockerIgnore_FileExists()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "..", ".dockerignore");

        // Act & Assert
        File.Exists(filePath).Should().BeTrue(".dockerignore should exist");
    }

    [Fact]
    public void StartupScript_PowerShell_Exists()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "..", "start-docker.ps1");

        // Act & Assert
        File.Exists(filePath).Should().BeTrue("PowerShell startup script should exist");
    }

    [Fact]
    public void StartupScript_Bash_Exists()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "..", "start-docker.sh");

        // Act & Assert
        File.Exists(filePath).Should().BeTrue("Bash startup script should exist");
    }

    [Fact]
    public void DockerCompose_Dev_ContainsPanelistAPI()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "..", "docker-compose.dev.yml");

        // Act
        var content = File.ReadAllText(filePath);

        // Assert
        content.Should().Contain("panelist-api", "Dev compose should include Panelist API");
        content.Should().Contain("5001:80", "Panelist API should expose port 5001");
    }

    [Fact]
    public void DockerCompose_Dev_ContainsCosmosDB()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "..", "docker-compose.dev.yml");

        // Act
        var content = File.ReadAllText(filePath);

        // Assert
        content.Should().Contain("cosmosdb", "Dev compose should include Cosmos DB emulator");
        content.Should().Contain("8081:8081", "Cosmos DB should expose port 8081");
    }

    [Fact]
    public void DockerCompose_Full_ContainsAllServices()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "..", "docker-compose.yml");

        // Act
        var content = File.ReadAllText(filePath);

        // Assert
        content.Should().Contain("panelist-api", "Full compose should include Panelist API");
        content.Should().Contain("survey-api", "Full compose should include Survey API");
        content.Should().Contain("adimpactos-functions", "Full compose should include Azure Functions");
        content.Should().Contain("event-consumer", "Full compose should include Event Consumer");
    }

    [Fact]
    public void Dockerfile_Functions_UsesCorrectBaseImage()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "..", "src", "AdImpactOs", "Dockerfile");

        // Act
        var content = File.ReadAllText(filePath);

        // Assert
        content.Should().Contain("mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0",
            "Functions Dockerfile should use Azure Functions base image for .NET 10");
    }

    [Fact]
    public void Dockerfile_PanelistAPI_UsesCorrectBaseImage()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "..", "src", "AdImpactOs.PanelistAPI", "Dockerfile");

        // Act
        var content = File.ReadAllText(filePath);

        // Assert
        content.Should().Contain("mcr.microsoft.com/dotnet/aspnet:10.0",
            "Panelist API Dockerfile should use ASP.NET Core 10.0 runtime");
    }

    [Fact]
    public void DockerIgnore_ExcludesBuildArtifacts()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "..", ".dockerignore");

        // Act
        var content = File.ReadAllText(filePath);

        // Assert
        content.Should().Contain("**/bin", "Should exclude bin folders");
        content.Should().Contain("**/obj", "Should exclude obj folders");
        content.Should().Contain("**/.vs", "Should exclude Visual Studio folders");
    }
}
