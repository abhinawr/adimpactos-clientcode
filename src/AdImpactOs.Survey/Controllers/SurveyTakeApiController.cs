using Microsoft.AspNetCore.Mvc;
using AdImpactOs.Survey.Models;
using AdImpactOs.Survey.Services;

namespace AdImpactOs.Survey.Controllers;

/// <summary>
/// Public-facing controller for panelists to take surveys via tokenized links.
/// No authentication required — the HMAC-signed token is the authorization.
/// </summary>
[ApiController]
[Route("api/surveys/take")]
public class SurveyTakeApiController : ControllerBase
{
    private readonly SurveyService _surveyService;
    private readonly SurveyTokenService _tokenService;
    private readonly ILogger<SurveyTakeApiController> _logger;

    public SurveyTakeApiController(
        SurveyService surveyService,
        SurveyTokenService tokenService,
        ILogger<SurveyTakeApiController> logger)
    {
        _surveyService = surveyService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Validate token and return survey questions for the panelist to answer.
    /// </summary>
    [HttpGet("{token}")]
    [ProducesResponseType(typeof(SurveyTakeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SurveyTakeResponse>> GetSurveyForToken(string token)
    {
        var payload = _tokenService.ValidateToken(token);
        if (payload == null)
        {
            return BadRequest(new { error = "Invalid or expired survey link" });
        }

        var survey = await _surveyService.GetSurveyAsync(payload.SurveyId);
        if (survey == null)
        {
            return NotFound(new { error = "Survey not found" });
        }

        if (survey.Status != "Active")
        {
            return BadRequest(new { error = "This survey is no longer accepting responses" });
        }

        return Ok(new SurveyTakeResponse
        {
            SurveyId = survey.SurveyId,
            SurveyName = survey.SurveyName,
            Description = survey.Description,
            SurveyType = survey.SurveyType,
            Questions = survey.Questions,
            PanelistId = payload.PanelistId,
            CohortType = payload.CohortType,
            ResponseId = payload.ResponseId
        });
    }

    /// <summary>
    /// Submit survey answers using a tokenized link.
    /// </summary>
    [HttpPost("{token}")]
    [ProducesResponseType(typeof(SurveyTakeSubmitResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SurveyTakeSubmitResult>> SubmitSurveyResponse(
        string token,
        [FromBody] SurveyTakeSubmitRequest request)
    {
        var payload = _tokenService.ValidateToken(token);
        if (payload == null)
        {
            return BadRequest(new { error = "Invalid or expired survey link" });
        }

        if (request.Answers == null || !request.Answers.Any())
        {
            return BadRequest(new { error = "At least one answer is required" });
        }

        try
        {
            var submitRequest = new SubmitSurveyResponseRequest
            {
                SurveyId = payload.SurveyId,
                PanelistId = payload.PanelistId,
                Answers = request.Answers,
                ResponseTimeSeconds = request.ResponseTimeSeconds,
                DeviceType = request.DeviceType,
                PendingResponseId = payload.ResponseId
            };

            var response = await _surveyService.SubmitResponseAsync(submitRequest);

            _logger.LogInformation("Panelist {PanelistId} completed survey {SurveyId} via token link",
                payload.PanelistId, payload.SurveyId);

            return Ok(new SurveyTakeSubmitResult
            {
                Success = true,
                Message = "Thank you for completing the survey!",
                ResponseId = response.ResponseId
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting survey response via token");
            return StatusCode(500, new { error = "An error occurred while submitting your response" });
        }
    }
}

/// <summary>
/// Response model containing survey data for the panelist to fill out.
/// </summary>
public class SurveyTakeResponse
{
    public string SurveyId { get; set; } = string.Empty;
    public string SurveyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SurveyType { get; set; } = string.Empty;
    public List<SurveyQuestion> Questions { get; set; } = new();
    public string PanelistId { get; set; } = string.Empty;
    public string CohortType { get; set; } = string.Empty;
    public string ResponseId { get; set; } = string.Empty;
}

/// <summary>
/// Request model for submitting answers via token link.
/// </summary>
public class SurveyTakeSubmitRequest
{
    public List<SurveyAnswer> Answers { get; set; } = new();
    public int? ResponseTimeSeconds { get; set; }
    public string? DeviceType { get; set; }
}

/// <summary>
/// Result returned after successful submission.
/// </summary>
public class SurveyTakeSubmitResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ResponseId { get; set; } = string.Empty;
}
