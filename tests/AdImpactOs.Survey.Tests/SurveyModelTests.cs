using Xunit;
using FluentAssertions;
using AdImpactOs.Survey.Models;

namespace AdImpactOs.Survey.Tests;

public class SurveyModelTests
{
    [Fact]
    public void Survey_DefaultValues_AreSetCorrectly()
    {
        var survey = new Models.Survey();

        survey.SurveyId.Should().Be(string.Empty);
        survey.CampaignId.Should().Be(string.Empty);
        survey.SurveyType.Should().Be("BrandLift");
        survey.Status.Should().Be("Draft");
        survey.Questions.Should().BeEmpty();
        survey.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        survey.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Survey_CanSetProperties()
    {
        var survey = new Models.Survey
        {
            SurveyId = "survey_123",
            CampaignId = "campaign_456",
            SurveyName = "Brand Lift Study",
            Description = "Test survey",
            SurveyType = "BrandLift",
            Status = "Active"
        };

        survey.SurveyId.Should().Be("survey_123");
        survey.CampaignId.Should().Be("campaign_456");
        survey.SurveyName.Should().Be("Brand Lift Study");
        survey.Description.Should().Be("Test survey");
    }

    [Fact]
    public void SurveyQuestion_DefaultValues()
    {
        var question = new SurveyQuestion();

        question.QuestionText.Should().Be(string.Empty);
        question.QuestionType.Should().Be("MultipleChoice");
        question.Required.Should().BeTrue();
        question.Order.Should().Be(0);
    }

    [Fact]
    public void SurveyQuestion_CanSetProperties()
    {
        var question = new SurveyQuestion
        {
            QuestionId = "q1",
            QuestionText = "Have you seen this ad?",
            QuestionType = "YesNo",
            Metric = "ad_recall",
            Options = new List<string> { "Yes", "No" },
            Required = true,
            Order = 1
        };

        question.QuestionId.Should().Be("q1");
        question.QuestionText.Should().Be("Have you seen this ad?");
        question.Metric.Should().Be("ad_recall");
        question.Options.Should().HaveCount(2);
    }

    [Fact]
    public void SurveyResponse_DefaultValues()
    {
        var response = new SurveyResponse();

        response.ResponseId.Should().Be(string.Empty);
        response.SurveyId.Should().Be(string.Empty);
        response.PanelistId.Should().Be(string.Empty);
        response.Answers.Should().BeEmpty();
        response.Status.Should().Be("Completed");
        response.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SurveyResponse_CanSetCohortType()
    {
        var response = new SurveyResponse { CohortType = "exposed" };
        response.CohortType.Should().Be("exposed");

        response.CohortType = "control";
        response.CohortType.Should().Be("control");
    }

    [Fact]
    public void SurveyAnswer_HasProperties()
    {
        var answer = new SurveyAnswer
        {
            QuestionId = "q1",
            Answer = "Yes",
            NumericValue = 5.0
        };

        answer.QuestionId.Should().Be("q1");
        answer.Answer.Should().Be("Yes");
        answer.NumericValue.Should().Be(5.0);
    }

    [Fact]
    public void SurveyAnswer_NumericValueIsOptional()
    {
        var answer = new SurveyAnswer { QuestionId = "q1", Answer = "No" };
        answer.NumericValue.Should().BeNull();
    }

    [Fact]
    public void CreateSurveyRequest_DefaultValues()
    {
        var request = new CreateSurveyRequest();

        request.CampaignId.Should().Be(string.Empty);
        request.SurveyName.Should().Be(string.Empty);
        request.SurveyType.Should().Be("BrandLift");
        request.Questions.Should().BeEmpty();
    }

    [Fact]
    public void SubmitSurveyResponseRequest_DefaultValues()
    {
        var request = new SubmitSurveyResponseRequest();

        request.SurveyId.Should().Be(string.Empty);
        request.PanelistId.Should().Be(string.Empty);
        request.Answers.Should().BeEmpty();
        request.ResponseTimeSeconds.Should().BeNull();
        request.DeviceType.Should().BeNull();
    }

    [Fact]
    public void TriggerSurveyRequest_DefaultValues()
    {
        var request = new TriggerSurveyRequest();

        request.SurveyId.Should().Be(string.Empty);
        request.PanelistIds.Should().BeEmpty();
        request.CohortType.Should().BeNull();
        request.ImpressionCounts.Should().BeNull();
    }

    [Fact]
    public void TriggerSurveyRequest_CanSetImpressionCounts()
    {
        var request = new TriggerSurveyRequest
        {
            SurveyId = "s1",
            PanelistIds = new List<string> { "p1", "p2" },
            CohortType = "exposed",
            ImpressionCounts = new Dictionary<string, int>
            {
                { "p1", 5 },
                { "p2", 3 }
            }
        };

        request.ImpressionCounts.Should().HaveCount(2);
        request.ImpressionCounts["p1"].Should().Be(5);
        request.ImpressionCounts["p2"].Should().Be(3);
    }

    [Fact]
    public void SurveyResponse_ImpressionCount_IsOptional()
    {
        var response = new SurveyResponse();
        response.ImpressionCount.Should().BeNull();
    }

    [Fact]
    public void SurveyResponse_CanSetImpressionCount()
    {
        var response = new SurveyResponse
        {
            ResponseId = "r1",
            PanelistId = "p1",
            ImpressionCount = 7
        };

        response.ImpressionCount.Should().Be(7);
    }

    [Fact]
    public void SurveyResultsResponse_HasProperties()
    {
        var results = new SurveyResultsResponse
        {
            SurveyId = "s1",
            CampaignId = "c1",
            TotalResponses = 100,
            ExposedResponses = 50,
            ControlResponses = 50
        };

        results.TotalResponses.Should().Be(100);
        results.ExposedResponses.Should().Be(50);
        results.ControlResponses.Should().Be(50);
    }

    [Fact]
    public void QuestionResult_HasLiftCalculation()
    {
        var result = new QuestionResult
        {
            QuestionId = "q1",
            QuestionText = "Ad recall",
            Metric = "ad_recall",
            ExposedMean = 4.5,
            ControlMean = 3.0,
            LiftPercent = 50.0,
            ResponseCounts = new Dictionary<string, int> { { "Yes", 30 }, { "No", 20 } }
        };

        result.ExposedMean.Should().Be(4.5);
        result.ControlMean.Should().Be(3.0);
        result.LiftPercent.Should().Be(50.0);
        result.ResponseCounts.Should().HaveCount(2);
    }

    [Fact]
    public void SurveyTriggerResult_HasProperties()
    {
        var result = new SurveyTriggerResult
        {
            SurveyId = "s1",
            CampaignId = "c1",
            TotalTriggered = 5,
            TotalSkipped = 2,
            Results = new List<SurveyTriggerPanelistResult>
            {
                new() { PanelistId = "p1", Status = "Triggered", Message = "OK" },
                new() { PanelistId = "p2", Status = "AlreadyResponded", Message = "Already responded" }
            }
        };

        result.TotalTriggered.Should().Be(5);
        result.TotalSkipped.Should().Be(2);
        result.Results.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(SurveyStatus.Draft)]
    [InlineData(SurveyStatus.Active)]
    [InlineData(SurveyStatus.Completed)]
    [InlineData(SurveyStatus.Paused)]
    [InlineData(SurveyStatus.Archived)]
    public void SurveyStatus_HasExpectedValues(SurveyStatus status)
    {
        Enum.IsDefined(typeof(SurveyStatus), status).Should().BeTrue();
    }

    [Theory]
    [InlineData(QuestionType.MultipleChoice)]
    [InlineData(QuestionType.Rating)]
    [InlineData(QuestionType.LikertScale)]
    [InlineData(QuestionType.OpenEnded)]
    [InlineData(QuestionType.YesNo)]
    public void QuestionType_HasExpectedValues(QuestionType questionType)
    {
        Enum.IsDefined(typeof(QuestionType), questionType).Should().BeTrue();
    }
}
