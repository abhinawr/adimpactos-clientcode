using Microsoft.AspNetCore.Mvc;
using AdImpactOs.Survey.Models;
using AdImpactOs.Survey.Services;

namespace AdImpactOs.Survey.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SurveysController : ControllerBase
{
    private readonly SurveyService _surveyService;
    private readonly ILogger<SurveysController> _logger;

    public SurveysController(
        SurveyService surveyService,
        ILogger<SurveysController> logger)
    {
        _surveyService = surveyService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Models.Survey), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Models.Survey>> CreateSurvey([FromBody] CreateSurveyRequest request)
    {
        if (string.IsNullOrEmpty(request.CampaignId) || string.IsNullOrEmpty(request.SurveyName))
        {
            return BadRequest("CampaignId and SurveyName are required");
        }

        try
        {
            var survey = await _surveyService.CreateSurveyAsync(request);
            return CreatedAtAction(nameof(GetSurvey), new { id = survey.SurveyId }, survey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating survey");
            return StatusCode(500, "An error occurred while creating the survey");
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<Models.Survey>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Models.Survey>>> GetAllSurveys([FromQuery] string? status = null)
    {
        try
        {
            var surveys = await _surveyService.GetAllSurveysAsync(status);
            return Ok(surveys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving surveys");
            return StatusCode(500, "An error occurred while retrieving surveys");
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Models.Survey), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Models.Survey>> GetSurvey(string id)
    {
        try
        {
            var survey = await _surveyService.GetSurveyAsync(id);
            if (survey == null)
            {
                return NotFound($"Survey {id} not found");
            }

            return Ok(survey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving survey {SurveyId}", id);
            return StatusCode(500, "An error occurred while retrieving the survey");
        }
    }

    [HttpGet("campaign/{campaignId}")]
    [ProducesResponseType(typeof(List<Models.Survey>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Models.Survey>>> GetSurveysByCampaign(string campaignId)
    {
        try
        {
            var surveys = await _surveyService.GetSurveysByCampaignAsync(campaignId);
            return Ok(surveys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving surveys for campaign {CampaignId}", campaignId);
            return StatusCode(500, "An error occurred while retrieving surveys");
        }
    }

    [HttpPost("trigger")]
    [ProducesResponseType(typeof(SurveyTriggerResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SurveyTriggerResult>> TriggerSurvey([FromBody] TriggerSurveyRequest request)
    {
        if (string.IsNullOrEmpty(request.SurveyId) || request.PanelistIds == null || !request.PanelistIds.Any())
        {
            return BadRequest("SurveyId and at least one PanelistId are required");
        }

        try
        {
            var result = await _surveyService.TriggerSurveyAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering survey {SurveyId}", request.SurveyId);
            return StatusCode(500, "An error occurred while triggering the survey");
        }
    }

    [HttpPost("responses")]
    [ProducesResponseType(typeof(SurveyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SurveyResponse>> SubmitResponse([FromBody] SubmitSurveyResponseRequest request)
    {
        if (string.IsNullOrEmpty(request.SurveyId) || string.IsNullOrEmpty(request.PanelistId))
        {
            return BadRequest("SurveyId and PanelistId are required");
        }

        try
        {
            var response = await _surveyService.SubmitResponseAsync(request);
            return CreatedAtAction(nameof(GetSurvey), new { id = response.SurveyId }, response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting survey response");
            return StatusCode(500, "An error occurred while submitting the response");
        }
    }

    [HttpGet("responses/all")]
    [ProducesResponseType(typeof(List<SurveyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SurveyResponse>>> GetAllResponses(
        [FromQuery] string? surveyId = null,
        [FromQuery] string? campaignId = null)
    {
        try
        {
            var responses = await _surveyService.GetAllResponsesAsync(surveyId, campaignId);
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving survey responses");
            return StatusCode(500, "An error occurred while retrieving survey responses");
        }
    }

    [HttpGet("{surveyId}/results")]
    [ProducesResponseType(typeof(SurveyResultsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SurveyResultsResponse>> GetSurveyResults(string surveyId)
    {
        try
        {
            var results = await _surveyService.GetSurveyResultsAsync(surveyId);
            return Ok(results);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting survey results for {SurveyId}", surveyId);
            return StatusCode(500, "An error occurred while retrieving survey results");
        }
    }

    [HttpGet("panelist/{panelistId}/responses")]
    [ProducesResponseType(typeof(List<SurveyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SurveyResponse>>> GetPanelistResponses(string panelistId)
    {
        try
        {
            var responses = await _surveyService.GetResponsesByPanelistAsync(panelistId);
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving responses for panelist {PanelistId}", panelistId);
            return StatusCode(500, "An error occurred while retrieving responses");
        }
    }

    [HttpPatch("{surveyId}/status")]
    [ProducesResponseType(typeof(Models.Survey), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Models.Survey>> UpdateSurveyStatus(string surveyId, [FromBody] string status)
    {
        try
        {
            var survey = await _surveyService.UpdateSurveyStatusAsync(surveyId, status);
            return Ok(survey);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating survey status for {SurveyId}", surveyId);
            return StatusCode(500, "An error occurred while updating survey status");
        }
    }
}
