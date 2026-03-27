using Microsoft.AspNetCore.Mvc;

namespace AdImpactOs.Dashboard.Controllers;

[Route("[controller]")]
public class PanelistsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public PanelistsController(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Panelists";
        return View();
    }

    [HttpGet("api/list")]
    public async Task<IActionResult> List()
    {
        var client = _httpClientFactory.CreateClient("PanelistApi");
        var response = await client.GetAsync("/api/panelists");
        return await Proxy(response);
    }

    [HttpGet("api/{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var client = _httpClientFactory.CreateClient("PanelistApi");
        var response = await client.GetAsync($"/api/panelists/{id}");
        return await Proxy(response);
    }

    [HttpPost("api/create")]
    public async Task<IActionResult> Create()
    {
        var client = _httpClientFactory.CreateClient("PanelistApi");
        var body = await ReadBody();
        var response = await client.PostAsync("/api/panelists", body);
        return await Proxy(response);
    }

    [HttpPut("api/{id}")]
    public async Task<IActionResult> Update(string id)
    {
        var client = _httpClientFactory.CreateClient("PanelistApi");
        var body = await ReadBody();
        var response = await client.PutAsync($"/api/panelists/{id}", body);
        return await Proxy(response);
    }

    [HttpDelete("api/{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var client = _httpClientFactory.CreateClient("PanelistApi");
        var response = await client.DeleteAsync($"/api/panelists/{id}");
        return StatusCode((int)response.StatusCode);
    }

    // Responses for a panelist (goes through Survey API)
    [HttpGet("api/{id}/responses")]
    public async Task<IActionResult> Responses(string id)
    {
        var client = _httpClientFactory.CreateClient("SurveyApi");
        var response = await client.GetAsync($"/api/surveys/panelist/{id}/responses");
        return await Proxy(response);
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
