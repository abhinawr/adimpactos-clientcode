using Microsoft.AspNetCore.Mvc;

namespace AdImpactOs.Dashboard.Controllers;

[Route("[controller]")]
public class TrackingController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TrackingController(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Ad Tracking";
        return View();
    }

    // ---- API proxy endpoints consumed by the page JavaScript ----

    [HttpGet("api/impressions")]
    public async Task<IActionResult> GetAllImpressions([FromQuery] int limit = 500)
    {
        var client = _httpClientFactory.CreateClient("CampaignApi");
        var response = await client.GetAsync($"/api/impressions?limit={limit}");
        return await ProxyResponse(response);
    }

    [HttpGet("api/impressions/campaign/{campaignId}")]
    public async Task<IActionResult> GetCampaignImpressions(string campaignId, [FromQuery] int limit = 100)
    {
        var client = _httpClientFactory.CreateClient("CampaignApi");
        var response = await client.GetAsync($"/api/impressions/campaign/{campaignId}?limit={limit}");
        return await ProxyResponse(response);
    }

    [HttpGet("api/impressions/campaign/{campaignId}/summary")]
    public async Task<IActionResult> GetCampaignSummary(string campaignId)
    {
        var client = _httpClientFactory.CreateClient("CampaignApi");
        var response = await client.GetAsync($"/api/impressions/campaign/{campaignId}/summary");
        return await ProxyResponse(response);
    }

    [HttpGet("api/impressions/summaries")]
    public async Task<IActionResult> GetAllSummaries()
    {
        var client = _httpClientFactory.CreateClient("CampaignApi");
        var response = await client.GetAsync("/api/impressions/summaries");
        return await ProxyResponse(response);
    }

    [HttpGet("api/campaigns")]
    public async Task<IActionResult> GetCampaigns()
    {
        var client = _httpClientFactory.CreateClient("CampaignApi");
        var response = await client.GetAsync("/api/campaigns");
        return await ProxyResponse(response);
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
}
