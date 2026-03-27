using Microsoft.AspNetCore.Mvc;

namespace AdImpactOs.Dashboard.Controllers;

/// <summary>
/// Proxies browser requests to the Campaign microservice API.
/// </summary>
[Route("[controller]")]
public class CampaignsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CampaignsController(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Campaigns";
        return View();
    }

    // ---- API proxy endpoints consumed by the page JavaScript ----

    [HttpGet("api/list")]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] string? industry)
    {
        var client = _httpClientFactory.CreateClient("CampaignApi");
        var qs = BuildQuery(("status", status), ("industry", industry));
        var response = await client.GetAsync($"/api/campaigns{qs}");
        return await ProxyResponse(response);
    }

    [HttpGet("api/{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var client = _httpClientFactory.CreateClient("CampaignApi");
        var response = await client.GetAsync($"/api/campaigns/{id}");
        return await ProxyResponse(response);
    }

    [HttpPost("api/create")]
    public async Task<IActionResult> Create()
    {
        var client = _httpClientFactory.CreateClient("CampaignApi");
        var body = await ReadBodyContent();
        var response = await client.PostAsync("/api/campaigns", body);
        return await ProxyResponse(response);
    }

    [HttpPut("api/{id}")]
    public async Task<IActionResult> Update(string id)
    {
        var client = _httpClientFactory.CreateClient("CampaignApi");
        var body = await ReadBodyContent();
        var response = await client.PutAsync($"/api/campaigns/{id}", body);
        return await ProxyResponse(response);
    }

    [HttpPatch("api/{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id)
    {
        var client = _httpClientFactory.CreateClient("CampaignApi");
        var body = await ReadBodyContent();
        var response = await client.PatchAsync($"/api/campaigns/{id}/status", body);
        return await ProxyResponse(response);
    }

    [HttpDelete("api/{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var client = _httpClientFactory.CreateClient("CampaignApi");
        var response = await client.DeleteAsync($"/api/campaigns/{id}");
        return StatusCode((int)response.StatusCode);
    }

    // ---- Helpers ----

    private async Task<StringContent> ReadBodyContent()
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }

    private static async Task<IActionResult> ProxyResponse(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = "application/json"
        };
    }

    private static string BuildQuery(params (string key, string? value)[] pairs)
    {
        var parts = pairs.Where(p => !string.IsNullOrEmpty(p.value))
                         .Select(p => $"{p.key}={Uri.EscapeDataString(p.value!)}");
        var qs = string.Join("&", parts);
        return string.IsNullOrEmpty(qs) ? "" : $"?{qs}";
    }
}
