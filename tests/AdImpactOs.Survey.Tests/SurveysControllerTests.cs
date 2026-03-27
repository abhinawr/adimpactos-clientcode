using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AdImpactOs.Survey.Controllers;
using AdImpactOs.Survey.Services;
using AdImpactOs.Survey.Models;

namespace AdImpactOs.Survey.Tests;

public class SurveysControllerTests
{
    private readonly Mock<SurveyService> _mockService;
    private readonly Mock<ILogger<SurveysController>> _mockLogger;
    private readonly SurveysController _controller;

    public SurveysControllerTests()
    {
        var mockTokenService = new SurveyTokenService(
            Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>(),
            Mock.Of<ILogger<SurveyTokenService>>());

        _mockService = new Mock<SurveyService>(
            Mock.Of<Microsoft.Azure.Cosmos.CosmosClient>(),
            Mock.Of<ILogger<SurveyService>>(),
            Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>(),
            mockTokenService);

        _mockLogger = new Mock<ILogger<SurveysController>>();
        _controller = new SurveysController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateSurvey_ReturnsBadRequest_WhenCampaignIdMissing()
    {
        var request = new CreateSurveyRequest { CampaignId = "", SurveyName = "Test" };
        var result = await _controller.CreateSurvey(request);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateSurvey_ReturnsBadRequest_WhenSurveyNameMissing()
    {
        var request = new CreateSurveyRequest { CampaignId = "c1", SurveyName = "" };
        var result = await _controller.CreateSurvey(request);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateSurvey_ReturnsCreated_WhenValid()
    {
        var request = new CreateSurveyRequest
        {
            CampaignId = "campaign_123",
            SurveyName = "Brand Lift Study",
            SurveyType = "BrandLift"
        };

        var created = new Models.Survey
        {
            SurveyId = "survey_123",
            CampaignId = "campaign_123",
            SurveyName = "Brand Lift Study"
        };

        _mockService.Setup(s => s.CreateSurveyAsync(request)).ReturnsAsync(created);

        var result = await _controller.CreateSurvey(request);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.Value.Should().Be(created);
    }

    [Fact]
    public async Task GetSurvey_ReturnsNotFound_WhenMissing()
    {
        _mockService.Setup(s => s.GetSurveyAsync("missing")).ReturnsAsync((Models.Survey?)null);
        var result = await _controller.GetSurvey("missing");
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetSurvey_ReturnsOk_WhenExists()
    {
        var survey = new Models.Survey { SurveyId = "s1", SurveyName = "Test" };
        _mockService.Setup(s => s.GetSurveyAsync("s1")).ReturnsAsync(survey);

        var result = await _controller.GetSurvey("s1");

        result.Result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result.Result!).Value.Should().Be(survey);
    }

    [Fact]
    public async Task GetAllSurveys_ReturnsOk_WithList()
    {
        var surveys = new List<Models.Survey>
        {
            new() { SurveyId = "s1", SurveyName = "Survey 1" },
            new() { SurveyId = "s2", SurveyName = "Survey 2" }
        };

        _mockService.Setup(s => s.GetAllSurveysAsync(null)).ReturnsAsync(surveys);

        var result = await _controller.GetAllSurveys();

        result.Result.Should().BeOfType<OkObjectResult>();
        var list = ((OkObjectResult)result.Result!).Value as List<Models.Survey>;
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSurveysByCampaign_ReturnsOk()
    {
        var surveys = new List<Models.Survey>
        {
            new() { SurveyId = "s1", CampaignId = "c1" }
        };

        _mockService.Setup(s => s.GetSurveysByCampaignAsync("c1")).ReturnsAsync(surveys);

        var result = await _controller.GetSurveysByCampaign("c1");

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TriggerSurvey_ReturnsBadRequest_WhenSurveyIdMissing()
    {
        var request = new TriggerSurveyRequest { SurveyId = "", PanelistIds = new List<string> { "p1" } };
        var result = await _controller.TriggerSurvey(request);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task TriggerSurvey_ReturnsBadRequest_WhenNoPanelistIds()
    {
        var request = new TriggerSurveyRequest { SurveyId = "s1", PanelistIds = new List<string>() };
        var result = await _controller.TriggerSurvey(request);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task TriggerSurvey_ReturnsOk_WhenValid()
    {
        var request = new TriggerSurveyRequest
        {
            SurveyId = "s1",
            PanelistIds = new List<string> { "p1", "p2" },
            CohortType = "exposed"
        };

        var triggerResult = new SurveyTriggerResult
        {
            SurveyId = "s1",
            TotalTriggered = 2,
            TotalSkipped = 0
        };

        _mockService.Setup(s => s.TriggerSurveyAsync(request)).ReturnsAsync(triggerResult);

        var result = await _controller.TriggerSurvey(request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TriggerSurvey_ReturnsBadRequest_WhenSurveyNotActive()
    {
        var request = new TriggerSurveyRequest
        {
            SurveyId = "s1",
            PanelistIds = new List<string> { "p1" }
        };

        _mockService.Setup(s => s.TriggerSurveyAsync(request))
            .ThrowsAsync(new InvalidOperationException("Survey is not active"));

        var result = await _controller.TriggerSurvey(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SubmitResponse_ReturnsBadRequest_WhenSurveyIdMissing()
    {
        var request = new SubmitSurveyResponseRequest { SurveyId = "", PanelistId = "p1" };
        var result = await _controller.SubmitResponse(request);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SubmitResponse_ReturnsBadRequest_WhenPanelistIdMissing()
    {
        var request = new SubmitSurveyResponseRequest { SurveyId = "s1", PanelistId = "" };
        var result = await _controller.SubmitResponse(request);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SubmitResponse_ReturnsCreated_WhenValid()
    {
        var request = new SubmitSurveyResponseRequest
        {
            SurveyId = "s1",
            PanelistId = "p1",
            Answers = new List<SurveyAnswer>
            {
                new() { QuestionId = "q1", Answer = "Yes", NumericValue = 5 }
            }
        };

        var response = new SurveyResponse
        {
            ResponseId = "response_123",
            SurveyId = "s1",
            PanelistId = "p1",
            Status = "Completed"
        };

        _mockService.Setup(s => s.SubmitResponseAsync(request)).ReturnsAsync(response);

        var result = await _controller.SubmitResponse(request);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task GetAllResponses_ReturnsOk()
    {
        var responses = new List<SurveyResponse>
        {
            new() { ResponseId = "r1", SurveyId = "s1" }
        };

        _mockService.Setup(s => s.GetAllResponsesAsync(null, null)).ReturnsAsync(responses);

        var result = await _controller.GetAllResponses();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSurveyResults_ReturnsNotFound_WhenSurveyMissing()
    {
        _mockService.Setup(s => s.GetSurveyResultsAsync("missing"))
            .ThrowsAsync(new InvalidOperationException("Survey not found"));

        var result = await _controller.GetSurveyResults("missing");

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetSurveyResults_ReturnsOk_WhenExists()
    {
        var results = new SurveyResultsResponse
        {
            SurveyId = "s1",
            TotalResponses = 10,
            ExposedResponses = 5,
            ControlResponses = 5
        };

        _mockService.Setup(s => s.GetSurveyResultsAsync("s1")).ReturnsAsync(results);

        var result = await _controller.GetSurveyResults("s1");

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPanelistResponses_ReturnsOk()
    {
        var responses = new List<SurveyResponse>
        {
            new() { ResponseId = "r1", PanelistId = "p1" }
        };

        _mockService.Setup(s => s.GetResponsesByPanelistAsync("p1")).ReturnsAsync(responses);

        var result = await _controller.GetPanelistResponses("p1");

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateSurveyStatus_ReturnsNotFound_WhenMissing()
    {
        _mockService.Setup(s => s.UpdateSurveyStatusAsync("missing", "Active"))
            .ThrowsAsync(new InvalidOperationException("Survey not found"));

        var result = await _controller.UpdateSurveyStatus("missing", "Active");

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateSurveyStatus_ReturnsOk_WhenSuccessful()
    {
        var survey = new Models.Survey { SurveyId = "s1", Status = "Active" };
        _mockService.Setup(s => s.UpdateSurveyStatusAsync("s1", "Active")).ReturnsAsync(survey);

        var result = await _controller.UpdateSurveyStatus("s1", "Active");

        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
