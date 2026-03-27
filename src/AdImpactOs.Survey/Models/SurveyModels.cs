using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AdImpactOs.Survey.Models;

public class Survey
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("surveyId")]
    public string SurveyId { get; set; } = string.Empty;

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

    [JsonProperty("status")]
    public string Status { get; set; } = "Draft";

    [JsonProperty("createdBy")]
    public string? CreatedBy { get; set; }

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SurveyQuestion
{
    [JsonProperty("questionId")]
    public string QuestionId { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("questionText")]
    public string QuestionText { get; set; } = string.Empty;

    [JsonProperty("questionType")]
    public string QuestionType { get; set; } = "MultipleChoice";

    [JsonProperty("metric")]
    public string? Metric { get; set; }

    [JsonProperty("options")]
    public List<string>? Options { get; set; }

    [JsonProperty("required")]
    public bool Required { get; set; } = true;

    [JsonProperty("order")]
    public int Order { get; set; }
}

public class SurveyResponse
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("responseId")]
    public string ResponseId { get; set; } = string.Empty;

    [JsonProperty("surveyId")]
    public string SurveyId { get; set; } = string.Empty;

    [JsonProperty("campaignId")]
    public string CampaignId { get; set; } = string.Empty;

    [JsonProperty("panelistId")]
    public string PanelistId { get; set; } = string.Empty;

    [JsonProperty("cohortType")]
    public string? CohortType { get; set; }

    [JsonProperty("answers")]
    public List<SurveyAnswer> Answers { get; set; } = new();

    [JsonProperty("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [JsonProperty("responseTime")]
    public int? ResponseTimeSeconds { get; set; }

    [JsonProperty("deviceType")]
    public string? DeviceType { get; set; }

    [JsonProperty("impressionCount")]
    public int? ImpressionCount { get; set; }

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonProperty("status")]
    public string Status { get; set; } = "Completed";
}

public class SurveyAnswer
{
    [JsonProperty("questionId")]
    public string QuestionId { get; set; } = string.Empty;

    [JsonProperty("answer")]
    public string? Answer { get; set; }

    [JsonProperty("numericValue")]
    public double? NumericValue { get; set; }
}

public enum SurveyStatus
{
    Draft,
    Active,
    Paused,
    Completed,
    Archived
}

public enum QuestionType
{
    MultipleChoice,
    Rating,
    LikertScale,
    OpenEnded,
    YesNo
}
