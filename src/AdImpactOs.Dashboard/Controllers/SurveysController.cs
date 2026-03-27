using Microsoft.AspNetCore.Mvc;

namespace AdImpactOs.Dashboard.Controllers;

[Route("[controller]")]
public class SurveysController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public SurveysController(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Surveys";
        return View();
    }

    // ---- API proxy ----

    [HttpGet("api/list")]
    public async Task<IActionResult> List([FromQuery] string? status)
    {
        var client = _httpClientFactory.CreateClient("SurveyApi");
        var qs = string.IsNullOrEmpty(status) ? "" : $"?status={Uri.EscapeDataString(status)}";
        var response = await client.GetAsync($"/api/surveys{qs}");
        return await Proxy(response);
    }

    [HttpGet("api/{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var client = _httpClientFactory.CreateClient("SurveyApi");
        var response = await client.GetAsync($"/api/surveys/{id}");
        return await Proxy(response);
    }

    [HttpPost("api/create")]
    public async Task<IActionResult> Create()
    {
        var client = _httpClientFactory.CreateClient("SurveyApi");
        var body = await ReadBody();
        var response = await client.PostAsync("/api/surveys", body);
        return await Proxy(response);
    }

    [HttpGet("api/{id}/results")]
    public async Task<IActionResult> Results(string id)
    {
        var client = _httpClientFactory.CreateClient("SurveyApi");
        var response = await client.GetAsync($"/api/surveys/{id}/results");
        return await Proxy(response);
    }

    [HttpPost("api/trigger")]
    public async Task<IActionResult> Trigger()
    {
        var client = _httpClientFactory.CreateClient("SurveyApi");
        var body = await ReadBody();
        var response = await client.PostAsync("/api/surveys/trigger", body);
        return await Proxy(response);
    }

    [HttpPatch("api/{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id)
    {
        var client = _httpClientFactory.CreateClient("SurveyApi");
        var body = await ReadBody();
        var response = await client.PatchAsync($"/api/surveys/{id}/status", body);
        return await Proxy(response);
    }

    [HttpGet("api/responses/all")]
    public async Task<IActionResult> AllResponses([FromQuery] string? surveyId, [FromQuery] string? campaignId)
    {
        var client = _httpClientFactory.CreateClient("SurveyApi");
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(surveyId)) parts.Add($"surveyId={Uri.EscapeDataString(surveyId)}");
        if (!string.IsNullOrEmpty(campaignId)) parts.Add($"campaignId={Uri.EscapeDataString(campaignId)}");
        var qs = parts.Any() ? "?" + string.Join("&", parts) : "";
        var response = await client.GetAsync($"/api/surveys/responses/all{qs}");
        return await Proxy(response);
    }

    [HttpGet("api/campaign/{campaignId}")]
    public async Task<IActionResult> ByCampaign(string campaignId)
    {
        var client = _httpClientFactory.CreateClient("SurveyApi");
        var response = await client.GetAsync($"/api/surveys/campaign/{campaignId}");
        return await Proxy(response);
    }

    /// <summary>
    /// Proxies to the Reports controller's demographics endpoint for use by the Surveys page.
    /// </summary>
    [HttpGet("api/{id}/demographics")]
    public async Task<IActionResult> Demographics(string id)
    {
        // Reuse the same Reports/api endpoint via internal redirect
        var surveyClient = _httpClientFactory.CreateClient("SurveyApi");
        var panelistClient = _httpClientFactory.CreateClient("PanelistApi");

        var responsesTask = surveyClient.GetAsync($"/api/surveys/responses/all?surveyId={Uri.EscapeDataString(id)}");
        var panelistsTask = panelistClient.GetAsync("/api/panelists");

        await Task.WhenAll(responsesTask, panelistsTask);

        var responsesJson = await responsesTask.Result.Content.ReadAsStringAsync();
        var panelistsJson = await panelistsTask.Result.Content.ReadAsStringAsync();

        // Return both datasets for client-side joining
        var combined = $"{{\"responses\":{responsesJson},\"panelists\":{panelistsJson}}}";
        return new ContentResult
        {
            StatusCode = 200,
            Content = combined,
            ContentType = "application/json"
        };
    }

    private async Task<StringContent> ReadBody()
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
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
}
