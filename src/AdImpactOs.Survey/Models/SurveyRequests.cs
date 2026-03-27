using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AdImpactOs.Survey.Models;

public class CreateSurveyRequest
{
    [JsonProperty("campaignId")]
    public string CampaignId { get; set; } = string.Empty;

    [JsonProperty("surveyName")]
    public string SurveyName { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("surveyType")]
    public string SurveyType { get; set; } = "BrandLift";

    [JsonProperty("questions")]
    public List<SurveyQuestion> Questions { get; set; } = new();

    [JsonProperty("targetAudience")]
    public JToken? TargetAudience { get; set; }

    [JsonProperty("distributionStartDate")]
    public DateTime? DistributionStartDate { get; set; }

    [JsonProperty("distributionEndDate")]
    public DateTime? DistributionEndDate { get; set; }
}

public class SubmitSurveyResponseRequest
{
    [JsonProperty("surveyId")]
    public string SurveyId { get; set; } = string.Empty;

    [JsonProperty("panelistId")]
    public string PanelistId { get; set; } = string.Empty;

    [JsonProperty("answers")]
    public List<SurveyAnswer> Answers { get; set; } = new();

    [JsonProperty("responseTimeSeconds")]
    public int? ResponseTimeSeconds { get; set; }

    [JsonProperty("deviceType")]
    public string? DeviceType { get; set; }

    [JsonProperty("pendingResponseId")]
    public string? PendingResponseId { get; set; }
}

public class GetSurveyResultsRequest
{
    [JsonProperty("surveyId")]
    public string? SurveyId { get; set; }

    [JsonProperty("campaignId")]
    public string? CampaignId { get; set; }

    [JsonProperty("cohortType")]
    public string? CohortType { get; set; }
}

public class SurveyResultsResponse
{
    [JsonProperty("surveyId")]
    public string SurveyId { get; set; } = string.Empty;

    [JsonProperty("campaignId")]
    public string CampaignId { get; set; } = string.Empty;

    [JsonProperty("totalResponses")]
    public int TotalResponses { get; set; }

    [JsonProperty("exposedResponses")]
    public int ExposedResponses { get; set; }

    [JsonProperty("controlResponses")]
    public int ControlResponses { get; set; }

    [JsonProperty("questionResults")]
    public List<QuestionResult> QuestionResults { get; set; } = new();

    [JsonProperty("generatedAt")]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class QuestionResult
{
    [JsonProperty("questionId")]
    public string QuestionId { get; set; } = string.Empty;

    [JsonProperty("questionText")]
    public string QuestionText { get; set; } = string.Empty;

    [JsonProperty("metric")]
    public string? Metric { get; set; }

    [JsonProperty("exposedMean")]
    public double? ExposedMean { get; set; }

    [JsonProperty("controlMean")]
    public double? ControlMean { get; set; }

    [JsonProperty("liftPercent")]
    public double? LiftPercent { get; set; }

    [JsonProperty("responseCounts")]
    public Dictionary<string, int>? ResponseCounts { get; set; }
}

public class TriggerSurveyRequest
{
    [JsonProperty("surveyId")]
    public string SurveyId { get; set; } = string.Empty;

    [JsonProperty("panelistIds")]
    public List<string> PanelistIds { get; set; } = new();

    [JsonProperty("cohortType")]
    public string? CohortType { get; set; }

    [JsonProperty("impressionCounts")]
    public Dictionary<string, int>? ImpressionCounts { get; set; }
}

public class SurveyTriggerResult
{
    [JsonProperty("surveyId")]
    public string SurveyId { get; set; } = string.Empty;

    [JsonProperty("campaignId")]
    public string CampaignId { get; set; } = string.Empty;

    [JsonProperty("totalTriggered")]
    public int TotalTriggered { get; set; }

    [JsonProperty("totalSkipped")]
    public int TotalSkipped { get; set; }

    [JsonProperty("results")]
    public List<SurveyTriggerPanelistResult> Results { get; set; } = new();

    [JsonProperty("triggeredAt")]
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
}

public class SurveyTriggerPanelistResult
{
    [JsonProperty("panelistId")]
    public string PanelistId { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("responseId")]
    public string? ResponseId { get; set; }

    [JsonProperty("surveyUrl")]
    public string? SurveyUrl { get; set; }
}
