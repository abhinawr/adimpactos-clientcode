using Microsoft.AspNetCore.Mvc;
using AdImpactOs.Campaign.Models;
using AdImpactOs.Campaign.Services;

namespace AdImpactOs.Campaign.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImpressionsController : ControllerBase
{
    private readonly ImpressionService _impressionService;
    private readonly ILogger<ImpressionsController> _logger;

    public ImpressionsController(ImpressionService impressionService, ILogger<ImpressionsController> logger)
    {
        _impressionService = impressionService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Impression), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Impression>> RecordImpression([FromBody] Impression impression)
    {
        if (string.IsNullOrEmpty(impression.CampaignId) || string.IsNullOrEmpty(impression.ImpressionId))
        {
            return BadRequest("CampaignId and ImpressionId are required");
        }

        try
        {
            var result = await _impressionService.RecordImpressionAsync(impression);
            return Created($"/api/impressions/{result.ImpressionId}", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording impression");
            return StatusCode(500, "An error occurred while recording the impression");
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<Impression>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Impression>>> GetAllImpressions([FromQuery] int limit = 500)
    {
        try
        {
            var impressions = await _impressionService.GetAllImpressionsAsync(limit);
            return Ok(impressions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving impressions");
            return StatusCode(500, "An error occurred while retrieving impressions");
        }
    }

    [HttpGet("campaign/{campaignId}")]
    [ProducesResponseType(typeof(List<Impression>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Impression>>> GetCampaignImpressions(string campaignId, [FromQuery] int limit = 100)
    {
        try
        {
            var impressions = await _impressionService.GetImpressionsByCampaignAsync(campaignId, limit);
            return Ok(impressions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving impressions for campaign {CampaignId}", campaignId);
            return StatusCode(500, "An error occurred while retrieving campaign impressions");
        }
    }

    [HttpGet("campaign/{campaignId}/summary")]
    [ProducesResponseType(typeof(ImpressionSummary), StatusCodes.Status200OK)]
    public async Task<ActionResult<ImpressionSummary>> GetCampaignSummary(string campaignId)
    {
        try
        {
            var summary = await _impressionService.GetCampaignImpressionSummaryAsync(campaignId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving impression summary for campaign {CampaignId}", campaignId);
            return StatusCode(500, "An error occurred while retrieving campaign summary");
        }
    }

    [HttpGet("campaign/{campaignId}/exposed-panelists")]
    [ProducesResponseType(typeof(ExposedPanelistsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExposedPanelistsResponse>> GetExposedPanelists(
        string campaignId, [FromQuery] int minImpressions = 1)
    {
        if (minImpressions < 1)
        {
            return BadRequest("minImpressions must be at least 1");
        }

        try
        {
            var result = await _impressionService.GetExposedPanelistIdsAsync(campaignId, minImpressions);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving exposed panelists for campaign {CampaignId}", campaignId);
            return StatusCode(500, "An error occurred while retrieving exposed panelists");
        }
    }

    [HttpGet("summaries")]
    [ProducesResponseType(typeof(Dictionary<string, ImpressionSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, ImpressionSummary>>> GetAllSummaries()
    {
        try
        {
            var summaries = await _impressionService.GetAllCampaignSummariesAsync();
            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving impression summaries");
            return StatusCode(500, "An error occurred while retrieving summaries");
        }
    }
}
