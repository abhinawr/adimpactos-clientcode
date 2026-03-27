using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AdImpactOs.PanelistAPI.Models;
using AdImpactOs.PanelistAPI.Services;

namespace AdImpactOs.PanelistAPI.Controllers;

/// <summary>
/// API controller for managing panelist profiles
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PanelistsController : ControllerBase
{
    private readonly PanelistService _panelistService;
    private readonly ILogger<PanelistsController> _logger;

    public PanelistsController(PanelistService panelistService, ILogger<PanelistsController> logger)
    {
        _panelistService = panelistService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new panelist profile
    /// </summary>
    /// <param name="request">Panelist creation request</param>
    /// <returns>Created panelist</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Panelist), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Panelist>> CreatePanelist([FromBody] CreatePanelistRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { error = "Email is required" });
        }

        try
        {
            var panelist = await _panelistService.CreatePanelistAsync(request);
            return CreatedAtAction(nameof(GetPanelistById), new { id = panelist.Id }, panelist);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create panelist");
            return StatusCode(500, new { error = "Failed to create panelist" });
        }
    }

    /// <summary>
    /// Get panelist by pseudonymized ID
    /// </summary>
    /// <param name="id">Panelist ID</param>
    /// <returns>Panelist profile</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Panelist), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Panelist>> GetPanelistById(string id)
    {
        try
        {
            var panelist = await _panelistService.GetPanelistByIdAsync(id);
            if (panelist == null)
            {
                return NotFound(new { error = $"Panelist with ID {id} not found" });
            }

            return Ok(panelist);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get panelist: {PanelistId}", id);
            return StatusCode(500, new { error = "Failed to retrieve panelist" });
        }
    }

    /// <summary>
    /// Update panelist profile - enforces consent on update
    /// </summary>
    /// <param name="id">Panelist ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated panelist</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Panelist), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Panelist>> UpdatePanelist(string id, [FromBody] UpdatePanelistRequest request)
    {
        try
        {
            var panelist = await _panelistService.UpdatePanelistAsync(id, request);
            if (panelist == null)
            {
                return NotFound(new { error = $"Panelist with ID {id} not found" });
            }

            return Ok(panelist);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update panelist: {PanelistId}", id);
            return StatusCode(500, new { error = "Failed to update panelist" });
        }
    }

    /// <summary>
    /// Check consent flag for a panelist
    /// </summary>
    /// <param name="id">Panelist ID</param>
    /// <returns>Consent status</returns>
    [HttpGet("{id}/consent")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CheckConsent(string id)
    {
        try
        {
            var hasConsent = await _panelistService.CheckConsentAsync(id);
            var panelist = await _panelistService.GetPanelistByIdAsync(id);
            
            if (panelist == null)
            {
                return NotFound(new { error = $"Panelist with ID {id} not found" });
            }

            return Ok(new
            {
                panelistId = id,
                consentGiven = hasConsent,
                consentTimestamp = panelist.ConsentTimestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check consent: {PanelistId}", id);
            return StatusCode(500, new { error = "Failed to check consent" });
        }
    }

    /// <summary>
    /// Delete panelist (soft delete)
    /// </summary>
    /// <param name="id">Panelist ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeletePanelist(string id)
    {
        try
        {
            var success = await _panelistService.DeletePanelistAsync(id);
            if (!success)
            {
                return NotFound(new { error = $"Panelist with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete panelist: {PanelistId}", id);
            return StatusCode(500, new { error = "Failed to delete panelist" });
        }
    }

    /// <summary>
    /// Get all active panelists
    /// </summary>
    /// <param name="pageSize">Page size (default 100)</param>
    /// <returns>List of panelists</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<Panelist>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Panelist>>> GetAllPanelists([FromQuery] int pageSize = 100)
    {
        try
        {
            var panelists = await _panelistService.GetAllPanelistsAsync(pageSize);
            return Ok(panelists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get panelists");
            return StatusCode(500, new { error = "Failed to retrieve panelists" });
        }
    }
}
