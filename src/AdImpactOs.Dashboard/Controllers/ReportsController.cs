using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AdImpactOs.Dashboard.Controllers;

[Route("[controller]")]
public class ReportsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ReportsController(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Reports";
        return View();
    }

    [HttpGet("api/campaigns")]
    public async Task<IActionResult> Campaigns()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("CampaignApi");
            var response = await client.GetAsync("/api/campaigns");
            if (!response.IsSuccessStatusCode)
                return new JsonResult(Array.Empty<object>());
            return await Proxy(response);
        }
        catch
        {
            return new JsonResult(Array.Empty<object>());
        }
    }

    [HttpGet("api/surveys")]
    public async Task<IActionResult> Surveys()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("SurveyApi");
            var response = await client.GetAsync("/api/surveys");
            if (!response.IsSuccessStatusCode)
                return new JsonResult(Array.Empty<object>());
            return await Proxy(response);
        }
        catch
        {
            return new JsonResult(Array.Empty<object>());
        }
    }

    [HttpGet("api/responses")]
    public async Task<IActionResult> Responses([FromQuery] string? surveyId, [FromQuery] string? campaignId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("SurveyApi");
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(surveyId)) parts.Add($"surveyId={Uri.EscapeDataString(surveyId)}");
            if (!string.IsNullOrEmpty(campaignId)) parts.Add($"campaignId={Uri.EscapeDataString(campaignId)}");
            var qs = parts.Any() ? "?" + string.Join("&", parts) : "";
            var response = await client.GetAsync($"/api/surveys/responses/all{qs}");
            if (!response.IsSuccessStatusCode)
                return new JsonResult(Array.Empty<object>());
            return await Proxy(response);
        }
        catch
        {
            return new JsonResult(Array.Empty<object>());
        }
    }

    [HttpGet("api/surveys/{id}/results")]
    public async Task<IActionResult> SurveyResults(string id)
    {
        var client = _httpClientFactory.CreateClient("SurveyApi");
        var response = await client.GetAsync($"/api/surveys/{id}/results");
        return await Proxy(response);
    }

    [HttpGet("api/panelists")]
    public async Task<IActionResult> Panelists()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("PanelistApi");
            var response = await client.GetAsync("/api/panelists");
            if (!response.IsSuccessStatusCode)
                return new JsonResult(Array.Empty<object>());
            return await Proxy(response);
        }
        catch
        {
            return new JsonResult(Array.Empty<object>());
        }
    }

    /// <summary>
    /// Joins survey responses with panelist demographics to produce a demographic breakdown
    /// for a given survey. This is done in the Dashboard (BFF) layer to avoid coupling
    /// the Survey and Panelist microservices.
    /// </summary>
    [HttpGet("api/surveys/{surveyId}/demographics")]
    public async Task<IActionResult> SurveyDemographics(string surveyId)
    {
        try
        {
            var surveyClient = _httpClientFactory.CreateClient("SurveyApi");
            var panelistClient = _httpClientFactory.CreateClient("PanelistApi");

            // Fetch survey, responses, and panelists in parallel
            var surveyTask = surveyClient.GetAsync($"/api/surveys/{surveyId}");
            var responsesTask = surveyClient.GetAsync($"/api/surveys/responses/all?surveyId={Uri.EscapeDataString(surveyId)}");
            var panelistsTask = panelistClient.GetAsync("/api/panelists");

            await Task.WhenAll(surveyTask, responsesTask, panelistsTask);

            var surveyResp = await surveyTask;
            var responsesResp = await responsesTask;
            var panelistsResp = await panelistsTask;

            if (!surveyResp.IsSuccessStatusCode)
                return StatusCode((int)surveyResp.StatusCode, "Survey not found");

            var survey = await JsonSerializer.DeserializeAsync<SurveyDto>(
                await surveyResp.Content.ReadAsStreamAsync(), JsonOpts);
            var responses = await JsonSerializer.DeserializeAsync<List<ResponseDto>>(
                await responsesResp.Content.ReadAsStreamAsync(), JsonOpts) ?? new();
            var panelists = await JsonSerializer.DeserializeAsync<List<PanelistDto>>(
                await panelistsResp.Content.ReadAsStreamAsync(), JsonOpts) ?? new();

            var panelistMap = panelists.ToDictionary(p => p.Id ?? p.PanelistId ?? "", p => p);

            // Enrich responses with demographics
            var enriched = responses.Select(r =>
            {
                panelistMap.TryGetValue(r.PanelistId ?? "", out var p);
                return new
                {
                    r.ResponseId,
                    r.SurveyId,
                    r.CampaignId,
                    r.PanelistId,
                    r.CohortType,
                    r.Status,
                    r.DeviceType,
                    r.CompletedAt,
                    r.CreatedAt,
                    r.Answers,
                    PanelistName = p != null ? $"{p.FirstName} {p.LastName}".Trim() : null,
                    p?.AgeRange,
                    p?.Gender,
                    p?.Country,
                    p?.HhIncomeBucket,
                    p?.Interests
                };
            }).ToList();

            // Build demographic breakdowns for completed responses only
            var completed = enriched.Where(r => r.Status == "Completed").ToList();

            var byAgeRange = BuildDemographicSegment(completed, r => r.AgeRange ?? "Unknown",
                survey?.Questions ?? new());
            var byGender = BuildDemographicSegment(completed, r => r.Gender ?? "Unknown",
                survey?.Questions ?? new());
            var byCountry = BuildDemographicSegment(completed, r => r.Country ?? "Unknown",
                survey?.Questions ?? new());
            var byIncome = BuildDemographicSegment(completed, r => r.HhIncomeBucket ?? "Unknown",
                survey?.Questions ?? new());

            var result = new
            {
                SurveyId = surveyId,
                survey?.CampaignId,
                survey?.SurveyName,
                TotalResponses = completed.Count,
                EnrichedResponses = enriched,
                DemographicBreakdown = new
                {
                    ByAgeRange = byAgeRange,
                    ByGender = byGender,
                    ByCountry = byCountry,
                    ByIncome = byIncome
                }
            };

            return new JsonResult(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error building demographic breakdown: {ex.Message}");
        }
    }

    private static List<object> BuildDemographicSegment<T>(
        List<T> responses,
        Func<T, string> segmentSelector,
        List<QuestionDto> questions) where T : class
    {
        // We need to access Answers, CohortType via reflection or dynamic — use dynamic
        var groups = responses.GroupBy(segmentSelector);
        var segments = new List<object>();

        foreach (var group in groups.OrderBy(g => g.Key))
        {
            var items = group.ToList();
            var exposed = items.Where(r => GetCohort(r) == "exposed").ToList();
            var control = items.Where(r => GetCohort(r) == "control").ToList();

            var questionMetrics = new List<object>();
            foreach (var q in questions)
            {
                var exposedAnswers = exposed.SelectMany(r => GetAnswers(r))
                    .Where(a => a.QuestionId == q.QuestionId).ToList();
                var controlAnswers = control.SelectMany(r => GetAnswers(r))
                    .Where(a => a.QuestionId == q.QuestionId).ToList();

                double? exposedMean = null, controlMean = null, lift = null;

                if (exposedAnswers.Any(a => a.NumericValue.HasValue) &&
                    controlAnswers.Any(a => a.NumericValue.HasValue))
                {
                    exposedMean = exposedAnswers.Where(a => a.NumericValue.HasValue)
                        .Average(a => a.NumericValue!.Value);
                    controlMean = controlAnswers.Where(a => a.NumericValue.HasValue)
                        .Average(a => a.NumericValue!.Value);
                    if (controlMean > 0)
                        lift = ((exposedMean!.Value - controlMean.Value) / controlMean.Value) * 100;
                }

                questionMetrics.Add(new
                {
                    q.QuestionId,
                    q.QuestionText,
                    q.Metric,
                    ExposedMean = exposedMean,
                    ControlMean = controlMean,
                    LiftPercent = lift
                });
            }

            segments.Add(new
            {
                Segment = group.Key,
                TotalResponses = items.Count,
                ExposedCount = exposed.Count,
                ControlCount = control.Count,
                QuestionMetrics = questionMetrics
            });
        }

        return segments;
    }

    private static string GetCohort<T>(T item)
    {
        var prop = typeof(T).GetProperty("CohortType");
        return prop?.GetValue(item)?.ToString() ?? "";
    }

    private static List<AnswerDto> GetAnswers<T>(T item)
    {
        var prop = typeof(T).GetProperty("Answers");
        return prop?.GetValue(item) as List<AnswerDto> ?? new();
    }

    private static async Task<IActionResult> Proxy(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = "application/json"
        };
    }

    // Lightweight DTOs for deserialization
    private class SurveyDto
    {
        public string? SurveyId { get; set; }
        public string? CampaignId { get; set; }
        public string? SurveyName { get; set; }
        public List<QuestionDto> Questions { get; set; } = new();
    }

    private class QuestionDto
    {
        public string? QuestionId { get; set; }
        public string? QuestionText { get; set; }
        public string? Metric { get; set; }
    }

    private class ResponseDto
    {
        public string? ResponseId { get; set; }
        public string? SurveyId { get; set; }
        public string? CampaignId { get; set; }
        public string? PanelistId { get; set; }
        public string? CohortType { get; set; }
        public string? Status { get; set; }
        public string? DeviceType { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<AnswerDto> Answers { get; set; } = new();
    }

    private class AnswerDto
    {
        public string? QuestionId { get; set; }
        public string? Answer { get; set; }
        public double? NumericValue { get; set; }
    }

    private class PanelistDto
    {
        public string? Id { get; set; }
        public string? PanelistId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? AgeRange { get; set; }
        public string? Gender { get; set; }
        public string? Country { get; set; }
        public string? HhIncomeBucket { get; set; }
        public string? Interests { get; set; }
    }
}
