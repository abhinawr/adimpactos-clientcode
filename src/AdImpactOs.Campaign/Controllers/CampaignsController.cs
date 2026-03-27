using Microsoft.AspNetCore.Mvc;
using AdImpactOs.Campaign.Services;
using AdImpactOs.Campaign.Models;

namespace AdImpactOs.Campaign.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CampaignsController : ControllerBase
{
    private readonly CampaignService _campaignService;
    private readonly ILogger<CampaignsController> _logger;

    public CampaignsController(
        CampaignService campaignService,
        ILogger<CampaignsController> logger)
    {
        _campaignService = campaignService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Models.Campaign), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Models.Campaign>> CreateCampaign([FromBody] CreateCampaignRequest request)
    {
        if (string.IsNullOrEmpty(request.CampaignName) || string.IsNullOrEmpty(request.Advertiser))
        {
            return BadRequest("CampaignName and Advertiser are required");
        }

        try
        {
            var campaign = await _campaignService.CreateCampaignAsync(request);
            return CreatedAtAction(nameof(GetCampaign), new { id = campaign.CampaignId }, campaign);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating campaign");
            return StatusCode(500, "An error occurred while creating the campaign");
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<Models.Campaign>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Models.Campaign>>> GetAllCampaigns(
        [FromQuery] string? status = null,
        [FromQuery] string? industry = null)
    {
        try
        {
            var campaigns = await _campaignService.GetAllCampaignsAsync(status, industry);
            return Ok(campaigns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving campaigns");
            return StatusCode(500, "An error occurred while retrieving campaigns");
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Models.Campaign), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Models.Campaign>> GetCampaign(string id)
    {
        try
        {
            var campaign = await _campaignService.GetCampaignAsync(id);
            if (campaign == null)
            {
                return NotFound($"Campaign {id} not found");
            }

            return Ok(campaign);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving campaign {CampaignId}", id);
            return StatusCode(500, "An error occurred while retrieving the campaign");
        }
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(List<Models.Campaign>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Models.Campaign>>> GetActiveCampaigns()
    {
        try
        {
            var campaigns = await _campaignService.GetActiveCampaignsAsync();
            return Ok(campaigns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active campaigns");
            return StatusCode(500, "An error occurred while retrieving active campaigns");
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Models.Campaign), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Models.Campaign>> UpdateCampaign(string id, [FromBody] UpdateCampaignRequest request)
    {
        try
        {
            var campaign = await _campaignService.UpdateCampaignAsync(id, request);
            return Ok(campaign);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating campaign {CampaignId}", id);
            return StatusCode(500, "An error occurred while updating the campaign");
        }
    }

    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(Models.Campaign), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Models.Campaign>> UpdateCampaignStatus(string id, [FromBody] string status)
    {
        try
        {
            var campaign = await _campaignService.UpdateCampaignStatusAsync(id, status);
            return Ok(campaign);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating campaign status for {CampaignId}", id);
            return StatusCode(500, "An error occurred while updating campaign status");
        }
    }

    [HttpPatch("{id}/metrics")]
    [ProducesResponseType(typeof(Models.Campaign), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Models.Campaign>> UpdateCampaignMetrics(string id, [FromBody] UpdateCampaignMetricsRequest request)
    {
        try
        {
            var campaign = await _campaignService.UpdateCampaignMetricsAsync(id, request);
            return Ok(campaign);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating campaign metrics for {CampaignId}", id);
            return StatusCode(500, "An error occurred while updating campaign metrics");
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteCampaign(string id)
    {
        try
        {
            var deleted = await _campaignService.DeleteCampaignAsync(id);
            if (!deleted)
            {
                return NotFound($"Campaign {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting campaign {CampaignId}", id);
            return StatusCode(500, "An error occurred while deleting the campaign");
        }
    }
}
