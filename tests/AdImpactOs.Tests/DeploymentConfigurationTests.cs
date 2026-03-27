using Xunit;
using FluentAssertions;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AdImpactOs.Tests;

/// <summary>
/// Tests to verify CI/CD workflow files, provisioning scripts, and deployment scripts.
/// </summary>
public class DeploymentConfigurationTests
{
    private static readonly string RepoRoot = Path.Combine("..", "..", "..", "..", "..");

    // ───────────────────────────────────────────────────
    // CI Workflow Tests
    // ───────────────────────────────────────────────────

    [Fact]
    public void CiWorkflow_FileExists()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "ci.yml");
        File.Exists(filePath).Should().BeTrue("ci.yml workflow should exist");
    }

    [Fact]
    public void CiWorkflow_HasSingleJobsKey()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "ci.yml");
        var content = File.ReadAllText(filePath);

        // A valid YAML file should have exactly one top-level 'jobs:' key
        var matches = Regex.Matches(content, @"^jobs:", RegexOptions.Multiline);
        matches.Count.Should().Be(1, "ci.yml should have exactly one top-level 'jobs:' key to avoid YAML silently discarding jobs");
    }

    [Fact]
    public void CiWorkflow_HasPushTrigger()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "ci.yml");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("push:", "CI workflow should trigger on push events");
    }

    [Fact]
    public void CiWorkflow_HasPullRequestTrigger()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "ci.yml");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("pull_request:", "CI workflow should trigger on pull_request events");
    }

    [Fact]
    public void CiWorkflow_HasBuildAndTestJob()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "ci.yml");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("build-and-test:", "CI workflow should have a build-and-test job");
        content.Should().Contain("dotnet build", "CI workflow should build the solution");
        content.Should().Contain("dotnet test", "CI workflow should run tests");
    }

    [Fact]
    public void CiWorkflow_HasPreventDirectMergeJob()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "ci.yml");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("prevent-direct-merge:", "CI workflow should enforce branch naming");
    }

    [Fact]
    public void CiWorkflow_UsesNet10()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "ci.yml");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("10.0", "CI workflow should use .NET 10");
    }

    // ───────────────────────────────────────────────────
    // CD Workflow Tests
    // ───────────────────────────────────────────────────

    [Fact]
    public void CdWorkflow_FileExists()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "cd.yml");
        File.Exists(filePath).Should().BeTrue("cd.yml workflow should exist");
    }

    [Fact]
    public void CdWorkflow_HasSingleJobsKey()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "cd.yml");
        var content = File.ReadAllText(filePath);

        var matches = Regex.Matches(content, @"^jobs:", RegexOptions.Multiline);
        matches.Count.Should().Be(1, "cd.yml should have exactly one top-level 'jobs:' key");
    }

    [Fact]
    public void CdWorkflow_UsesRepoRootAsDockerContext()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "cd.yml");
        var content = File.ReadAllText(filePath);

        // Docker context should be repo root, not service subdirectories
        content.Should().Contain("context: .", "Docker build context should be repo root");

        // Each Dockerfile should be specified via the 'file:' key
        content.Should().Contain("file: src/AdImpactOs.PanelistAPI/Dockerfile",
            "Panelist API Dockerfile should be referenced with explicit file path");
    }

    [Fact]
    public void CdWorkflow_HasWorkflowDispatch()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "cd.yml");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("workflow_dispatch:", "CD workflow should support manual dispatch");
    }

    [Fact]
    public void CdWorkflow_DefaultEnvironmentIsDev()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "cd.yml");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("default: 'dev'", "CD workflow default environment should be dev");
    }

    [Fact]
    public void CdWorkflow_HasBuildAndPushJob()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "cd.yml");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("build-and-push:", "CD workflow should have a build-and-push job");
    }

    [Fact]
    public void CdWorkflow_HasDeployJobs()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "cd.yml");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("deploy-functions:", "CD workflow should have a deploy-functions job");
        content.Should().Contain("deploy-services:", "CD workflow should have a deploy-services job");
    }

    // ───────────────────────────────────────────────────
    // Infrastructure Workflow Tests
    // ───────────────────────────────────────────────────

    [Fact]
    public void InfraWorkflow_FileExists()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "infra.yml");
        File.Exists(filePath).Should().BeTrue("infra.yml workflow should exist for infrastructure provisioning");
    }

    [Fact]
    public void InfraWorkflow_HasSingleJobsKey()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "infra.yml");
        var content = File.ReadAllText(filePath);

        var matches = Regex.Matches(content, @"^jobs:", RegexOptions.Multiline);
        matches.Count.Should().Be(1, "infra.yml should have exactly one top-level 'jobs:' key");
    }

    [Fact]
    public void InfraWorkflow_IsWorkflowDispatchOnly()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "infra.yml");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("workflow_dispatch:", "Infra workflow should be manually triggered");
        // Should NOT auto-trigger on push or PR
        content.Should().NotContain("\n  push:", "Infra workflow should NOT trigger on push");
        content.Should().NotContain("\n  pull_request:", "Infra workflow should NOT trigger on pull_request");
    }

    [Fact]
    public void InfraWorkflow_UsesServerlessCosmos()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "infra.yml");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("EnableServerless", "Infra workflow should provision Cosmos DB in serverless mode");
        content.Should().NotContain("--throughput", "Serverless Cosmos DB should not use --throughput");
    }

    [Fact]
    public void InfraWorkflow_CreatesAllRequiredResources()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "infra.yml");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("az group create", "Should create resource group");
        content.Should().Contain("az acr create", "Should create container registry");
        content.Should().Contain("az cosmosdb create", "Should create Cosmos DB account");
        content.Should().Contain("az eventhubs namespace create", "Should create Event Hubs");
        content.Should().Contain("az storage account create", "Should create storage account");
        content.Should().Contain("az keyvault create", "Should create Key Vault");
        content.Should().Contain("az appservice plan create", "Should create App Service plan");
        content.Should().Contain("az functionapp create", "Should create Function App");
        content.Should().Contain("az webapp create", "Should create web apps");
    }

    [Fact]
    public void InfraWorkflow_HasInputParameters()
    {
        var filePath = Path.Combine(RepoRoot, ".github", "workflows", "infra.yml");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("prefix:", "Should accept prefix input");
        content.Should().Contain("environment:", "Should accept environment input");
        content.Should().Contain("location:", "Should accept location input");
    }

    // ───────────────────────────────────────────────────
    // Provisioning Script Tests (Bash)
    // ───────────────────────────────────────────────────

    [Fact]
    public void ProvisionBash_FileExists()
    {
        var filePath = Path.Combine(RepoRoot, "scripts", "provision-azure.sh");
        File.Exists(filePath).Should().BeTrue("provision-azure.sh should exist");
    }

    [Fact]
    public void ProvisionBash_UsesServerlessCosmos()
    {
        var filePath = Path.Combine(RepoRoot, "scripts", "provision-azure.sh");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("EnableServerless", "Bash script should provision Cosmos DB in serverless mode");
        content.Should().NotContain("--throughput", "Serverless Cosmos DB should not use --throughput");
    }

    [Fact]
    public void ProvisionBash_CreatesAllRequiredResources()
    {
        var filePath = Path.Combine(RepoRoot, "scripts", "provision-azure.sh");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("az group create", "Should create resource group");
        content.Should().Contain("az acr create", "Should create container registry");
        content.Should().Contain("az cosmosdb create", "Should create Cosmos DB");
        content.Should().Contain("az eventhubs namespace create", "Should create Event Hubs");
        content.Should().Contain("az storage account create", "Should create storage account");
        content.Should().Contain("az keyvault create", "Should create Key Vault");
        content.Should().Contain("az appservice plan create", "Should create App Service plan");
        content.Should().Contain("az functionapp create", "Should create Function App");
        content.Should().Contain("az webapp create", "Should create web apps");
    }

    [Fact]
    public void ProvisionBash_StoresSecretsInKeyVault()
    {
        var filePath = Path.Combine(RepoRoot, "scripts", "provision-azure.sh");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("az keyvault secret set", "Should store secrets in Key Vault");
        content.Should().Contain("CosmosDbEndpoint", "Should store Cosmos DB endpoint");
        content.Should().Contain("CosmosDbKey", "Should store Cosmos DB key");
        content.Should().Contain("EventHubConnectionString", "Should store Event Hub connection string");
    }

    [Fact]
    public void ProvisionBash_SupportsCommandLineArgs()
    {
        var filePath = Path.Combine(RepoRoot, "scripts", "provision-azure.sh");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("--prefix", "Should accept --prefix argument");
        content.Should().Contain("--env", "Should accept --env argument");
        content.Should().Contain("--location", "Should accept --location argument");
    }

    [Fact]
    public void ProvisionBash_HasDefaultValues()
    {
        var filePath = Path.Combine(RepoRoot, "scripts", "provision-azure.sh");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("adimpact", "Should have adimpact as default prefix");
        content.Should().Contain("dev", "Should have dev as default environment");
        content.Should().Contain("eastus", "Should have eastus as default location");
    }

    // ───────────────────────────────────────────────────
    // Provisioning Script Tests (PowerShell)
    // ───────────────────────────────────────────────────

    [Fact]
    public void ProvisionPowerShell_FileExists()
    {
        var filePath = Path.Combine(RepoRoot, "scripts", "provision-azure.ps1");
        File.Exists(filePath).Should().BeTrue("provision-azure.ps1 should exist");
    }

    [Fact]
    public void ProvisionPowerShell_UsesServerlessCosmos()
    {
        var filePath = Path.Combine(RepoRoot, "scripts", "provision-azure.ps1");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("EnableServerless", "PowerShell script should provision Cosmos DB in serverless mode");
    }

    [Fact]
    public void ProvisionPowerShell_CreatesAllRequiredResources()
    {
        var filePath = Path.Combine(RepoRoot, "scripts", "provision-azure.ps1");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("az group create", "Should create resource group");
        content.Should().Contain("az acr create", "Should create container registry");
        content.Should().Contain("az cosmosdb create", "Should create Cosmos DB");
        content.Should().Contain("az eventhubs namespace create", "Should create Event Hubs");
        content.Should().Contain("az keyvault create", "Should create Key Vault");
        content.Should().Contain("az functionapp create", "Should create Function App");
        content.Should().Contain("az webapp create", "Should create web apps");
    }

    [Fact]
    public void ProvisionPowerShell_HasParamBlock()
    {
        var filePath = Path.Combine(RepoRoot, "scripts", "provision-azure.ps1");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("param(", "PowerShell script should have a param block");
        content.Should().Contain("$Prefix", "Should accept Prefix parameter");
        content.Should().Contain("$Env", "Should accept Env parameter");
        content.Should().Contain("$Location", "Should accept Location parameter");
    }

    // ───────────────────────────────────────────────────
    // Deploy Script Tests
    // ───────────────────────────────────────────────────

    [Fact]
    public void DeployBash_FileExists()
    {
        var filePath = Path.Combine(RepoRoot, "scripts", "deploy.sh");
        File.Exists(filePath).Should().BeTrue("deploy.sh should exist for manual deployments");
    }

    [Fact]
    public void DeployBash_BuildsAllDockerImages()
    {
        var filePath = Path.Combine(RepoRoot, "scripts", "deploy.sh");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("docker build", "Deploy script should build Docker images");
        content.Should().Contain("docker push", "Deploy script should push Docker images");
        content.Should().Contain("panelistapi", "Should build Panelist API image");
        content.Should().Contain("campaignapi", "Should build Campaign API image");
        content.Should().Contain("surveyapi", "Should build Survey API image");
        content.Should().Contain("dashboard", "Should build Dashboard image");
        content.Should().Contain("eventconsumer", "Should build Event Consumer image");
    }

    [Fact]
    public void DeployBash_UpdatesAppServiceContainers()
    {
        var filePath = Path.Combine(RepoRoot, "scripts", "deploy.sh");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("az webapp config container set", "Should update App Service container images");
    }

    [Fact]
    public void DeployBash_DeploysAzureFunctions()
    {
        var filePath = Path.Combine(RepoRoot, "scripts", "deploy.sh");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("func azure functionapp publish", "Should deploy Azure Functions");
    }

    [Fact]
    public void DeployBash_PerformsHealthChecks()
    {
        var filePath = Path.Combine(RepoRoot, "scripts", "deploy.sh");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("health", "Deploy script should perform health checks");
        content.Should().Contain("curl", "Should use curl for health checks");
    }

    [Fact]
    public void DeployBash_SupportsSkipBuild()
    {
        var filePath = Path.Combine(RepoRoot, "scripts", "deploy.sh");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("--skip-build", "Should support --skip-build flag");
    }

    // ───────────────────────────────────────────────────
    // Deployment Guide Tests
    // ───────────────────────────────────────────────────

    [Fact]
    public void DeploymentGuide_FileExists()
    {
        var filePath = Path.Combine(RepoRoot, "AZURE-DEPLOYMENT-GUIDE-STEP-BY-STEP.md");
        File.Exists(filePath).Should().BeTrue("Deployment guide should exist");
    }

    [Fact]
    public void DeploymentGuide_UsesServerlessCosmos()
    {
        var filePath = Path.Combine(RepoRoot, "AZURE-DEPLOYMENT-GUIDE-STEP-BY-STEP.md");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("EnableServerless", "Deployment guide should reference serverless Cosmos DB");
        content.Should().NotContain("--throughput 400", "Deployment guide should not reference provisioned throughput for serverless");
    }

    [Fact]
    public void DeploymentGuide_ReferencesAdImpactOs()
    {
        var filePath = Path.Combine(RepoRoot, "AZURE-DEPLOYMENT-GUIDE-STEP-BY-STEP.md");
        var content = File.ReadAllText(filePath);

        content.Should().Contain("AdImpactOs", "Deployment guide should reference the correct product name");
    }

    // ───────────────────────────────────────────────────
    // Cross-file Consistency Tests
    // ───────────────────────────────────────────────────

    [Fact]
    public void AllWorkflows_UseValidYamlStructure()
    {
        var workflowDir = Path.Combine(RepoRoot, ".github", "workflows");
        var yamlFiles = Directory.GetFiles(workflowDir, "*.yml");

        yamlFiles.Should().NotBeEmpty("There should be workflow YAML files");

        foreach (var file in yamlFiles)
        {
            var content = File.ReadAllText(file);
            var jobsMatches = Regex.Matches(content, @"^jobs:", RegexOptions.Multiline);
            jobsMatches.Count.Should().Be(1,
                $"{Path.GetFileName(file)} should have exactly one top-level 'jobs:' key");
        }
    }

    [Fact]
    public void ProvisionScripts_AreConsistentOnCosmosContainers()
    {
        var bashPath = Path.Combine(RepoRoot, "scripts", "provision-azure.sh");
        var psPath = Path.Combine(RepoRoot, "scripts", "provision-azure.ps1");

        var bashContent = File.ReadAllText(bashPath);
        var psContent = File.ReadAllText(psPath);

        var containers = new[] { "Panelists", "Campaigns", "Impressions", "Surveys", "SurveyResponses" };

        foreach (var container in containers)
        {
            bashContent.Should().Contain(container, $"Bash script should create {container} container");
            psContent.Should().Contain(container, $"PowerShell script should create {container} container");
        }
    }

    [Fact]
    public void ProvisionScripts_UseConsistentDefaults()
    {
        var bashPath = Path.Combine(RepoRoot, "scripts", "provision-azure.sh");
        var psPath = Path.Combine(RepoRoot, "scripts", "provision-azure.ps1");

        var bashContent = File.ReadAllText(bashPath);
        var psContent = File.ReadAllText(psPath);

        // Both scripts should use the same default prefix
        bashContent.Should().Contain("adimpact", "Bash script should use adimpact prefix");
        psContent.Should().Contain("adimpact", "PowerShell script should use adimpact prefix");

        // Both should target the same email
        bashContent.Should().Contain("tech@theeditorialinstitute.com");
        psContent.Should().Contain("tech@theeditorialinstitute.com");
    }
}
