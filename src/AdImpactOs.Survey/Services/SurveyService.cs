using Microsoft.Azure.Cosmos;
using AdImpactOs.Survey.Models;
using System.Net;

namespace AdImpactOs.Survey.Services;

public class SurveyService
{
    private readonly Container _surveyContainer;
    private readonly Container _responseContainer;
    private readonly ILogger<SurveyService> _logger;
    private readonly SurveyTokenService _tokenService;
    private readonly IConfiguration _configuration;

    public SurveyService(
        CosmosClient cosmosClient,
        ILogger<SurveyService> logger,
        IConfiguration configuration,
        SurveyTokenService tokenService)
    {
        var databaseName = configuration["CosmosDb:DatabaseName"] ?? "AdTrackingDB";
        var surveyContainerName = configuration["CosmosDb:SurveyContainerName"] ?? "Surveys";
        var responseContainerName = configuration["CosmosDb:SurveyResponseContainerName"] ?? "SurveyResponses";

        _surveyContainer = cosmosClient.GetContainer(databaseName, surveyContainerName);
        _responseContainer = cosmosClient.GetContainer(databaseName, responseContainerName);
        _logger = logger;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public virtual async Task<Models.Survey> CreateSurveyAsync(CreateSurveyRequest request)
    {
        var survey = new Models.Survey
        {
            SurveyId = $"survey_{Guid.NewGuid():N}",
            CampaignId = request.CampaignId,
            SurveyName = request.SurveyName,
            Description = request.Description,
            SurveyType = request.SurveyType,
            Questions = request.Questions,
            TargetAudience = request.TargetAudience,
            DistributionStartDate = request.DistributionStartDate,
            DistributionEndDate = request.DistributionEndDate,
            Status = "Active"
        };

        survey.Id = survey.SurveyId;

        var response = await _surveyContainer.CreateItemAsync(survey, new PartitionKey(survey.SurveyId));
        _logger.LogInformation("Created survey {SurveyId} for campaign {CampaignId}", survey.SurveyId, survey.CampaignId);

        return response.Resource;
    }

    public virtual async Task<Models.Survey?> GetSurveyAsync(string surveyId)
    {
        try
        {
            var response = await _surveyContainer.ReadItemAsync<Models.Survey>(surveyId, new PartitionKey(surveyId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<Models.Survey>> GetSurveysByCampaignAsync(string campaignId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.campaignId = @campaignId")
            .WithParameter("@campaignId", campaignId);

        var iterator = _surveyContainer.GetItemQueryIterator<Models.Survey>(query);
        var results = new List<Models.Survey>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    /// <summary>
    /// Get all surveys with optional status filter
    /// </summary>
    public virtual async Task<List<Models.Survey>> GetAllSurveysAsync(string? status = null)
    {
        var queryText = "SELECT * FROM c";

        if (!string.IsNullOrEmpty(status))
        {
            queryText += " WHERE c.status = @status";
        }

        queryText += " ORDER BY c.createdAt DESC";

        var query = new QueryDefinition(queryText);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.WithParameter("@status", status);
        }

        var iterator = _surveyContainer.GetItemQueryIterator<Models.Survey>(query);
        var results = new List<Models.Survey>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    /// <summary>
    /// Trigger a survey for a specific panelist - creates a pending survey invitation
    /// </summary>
    public virtual async Task<SurveyTriggerResult> TriggerSurveyAsync(TriggerSurveyRequest request)
    {
        var survey = await GetSurveyAsync(request.SurveyId);
        if (survey == null)
        {
            throw new InvalidOperationException($"Survey {request.SurveyId} not found");
        }

        if (survey.Status != "Active")
        {
            throw new InvalidOperationException($"Survey {request.SurveyId} is not active (current status: {survey.Status})");
        }

        var baseUrl = _configuration["SurveyToken:BaseUrl"] ?? "http://localhost:5002";
        var results = new List<SurveyTriggerPanelistResult>();

        foreach (var panelistId in request.PanelistIds)
        {
            // Check if panelist already responded
            var existingQuery = new QueryDefinition(
                "SELECT VALUE COUNT(1) FROM c WHERE c.surveyId = @surveyId AND c.panelistId = @panelistId")
                .WithParameter("@surveyId", request.SurveyId)
                .WithParameter("@panelistId", panelistId);

            var countIterator = _responseContainer.GetItemQueryIterator<int>(existingQuery);
            int existingCount = 0;
            if (countIterator.HasMoreResults)
            {
                var countResponse = await countIterator.ReadNextAsync();
                existingCount = countResponse.FirstOrDefault();
            }

            if (existingCount > 0)
            {
                results.Add(new SurveyTriggerPanelistResult
                {
                    PanelistId = panelistId,
                    Status = "AlreadyResponded",
                    Message = "Panelist has already responded to this survey"
                });
                continue;
            }

            // Create a pending response entry to track the invitation
            int? impressionCount = request.ImpressionCounts?.TryGetValue(panelistId, out var count) == true ? count : null;

            var pendingResponse = new SurveyResponse
            {
                ResponseId = $"pending_{Guid.NewGuid():N}",
                SurveyId = request.SurveyId,
                CampaignId = survey.CampaignId,
                PanelistId = panelistId,
                CohortType = request.CohortType ?? "exposed",
                Answers = new List<SurveyAnswer>(),
                Status = "Pending",
                ImpressionCount = impressionCount,
                CreatedAt = DateTime.UtcNow
            };
            pendingResponse.Id = pendingResponse.ResponseId;

            try
            {
                await _responseContainer.CreateItemAsync(pendingResponse, new PartitionKey(pendingResponse.ResponseId));

                var token = _tokenService.GenerateToken(
                    request.SurveyId,
                    panelistId,
                    request.CohortType ?? "exposed",
                    pendingResponse.ResponseId);

                var surveyUrl = $"{baseUrl.TrimEnd('/')}/survey/take/{token}";

                results.Add(new SurveyTriggerPanelistResult
                {
                    PanelistId = panelistId,
                    Status = "Triggered",
                    Message = "Survey invitation created with link",
                    ResponseId = pendingResponse.ResponseId,
                    SurveyUrl = surveyUrl
                });
                _logger.LogInformation("Triggered survey {SurveyId} for panelist {PanelistId} with token link", request.SurveyId, panelistId);
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Failed to trigger survey for panelist {PanelistId}", panelistId);
                results.Add(new SurveyTriggerPanelistResult
                {
                    PanelistId = panelistId,
                    Status = "Failed",
                    Message = $"Failed to trigger: {ex.Message}"
                });
            }
        }

        return new SurveyTriggerResult
        {
            SurveyId = request.SurveyId,
            CampaignId = survey.CampaignId,
            TotalTriggered = results.Count(r => r.Status == "Triggered"),
            TotalSkipped = results.Count(r => r.Status != "Triggered"),
            Results = results
        };
    }

    /// <summary>
    /// Get all survey responses (with optional filters)
    /// </summary>
    public virtual async Task<List<SurveyResponse>> GetAllResponsesAsync(string? surveyId = null, string? campaignId = null)
    {
        var queryText = "SELECT * FROM c";
        var conditions = new List<string>();

        if (!string.IsNullOrEmpty(surveyId))
            conditions.Add("c.surveyId = @surveyId");
        if (!string.IsNullOrEmpty(campaignId))
            conditions.Add("c.campaignId = @campaignId");

        if (conditions.Any())
            queryText += " WHERE " + string.Join(" AND ", conditions);

        queryText += " ORDER BY c.createdAt DESC";

        var query = new QueryDefinition(queryText);

        if (!string.IsNullOrEmpty(surveyId))
            query = query.WithParameter("@surveyId", surveyId);
        if (!string.IsNullOrEmpty(campaignId))
            query = query.WithParameter("@campaignId", campaignId);

        var iterator = _responseContainer.GetItemQueryIterator<SurveyResponse>(query);
        var results = new List<SurveyResponse>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public virtual async Task<SurveyResponse> SubmitResponseAsync(SubmitSurveyResponseRequest request)
    {
        var survey = await GetSurveyAsync(request.SurveyId);
        if (survey == null)
        {
            throw new InvalidOperationException($"Survey {request.SurveyId} not found");
        }

        // If a pending response ID is provided, update it instead of creating a new one
        if (!string.IsNullOrEmpty(request.PendingResponseId))
        {
            return await CompletePendingResponseAsync(request);
        }

        var response = new SurveyResponse
        {
            ResponseId = $"response_{Guid.NewGuid():N}",
            SurveyId = request.SurveyId,
            CampaignId = survey.CampaignId,
            PanelistId = request.PanelistId,
            Answers = request.Answers,
            CompletedAt = DateTime.UtcNow,
            ResponseTimeSeconds = request.ResponseTimeSeconds,
            DeviceType = request.DeviceType,
            Status = "Completed"
        };

        response.Id = response.ResponseId;

        var result = await _responseContainer.CreateItemAsync(response, new PartitionKey(response.ResponseId));
        _logger.LogInformation("Recorded survey response {ResponseId} for panelist {PanelistId}", response.ResponseId, response.PanelistId);

        return result.Resource;
    }

    /// <summary>
    /// Complete a pending survey response (from a triggered invitation)
    /// </summary>
    private async Task<SurveyResponse> CompletePendingResponseAsync(SubmitSurveyResponseRequest request)
    {
        try
        {
            var pendingResponse = await _responseContainer.ReadItemAsync<SurveyResponse>(
                request.PendingResponseId, new PartitionKey(request.PendingResponseId));

            var existing = pendingResponse.Resource;

            if (existing.Status == "Completed")
            {
                throw new InvalidOperationException("This survey has already been completed");
            }

            existing.Answers = request.Answers;
            existing.CompletedAt = DateTime.UtcNow;
            existing.ResponseTimeSeconds = request.ResponseTimeSeconds;
            existing.DeviceType = request.DeviceType;
            existing.Status = "Completed";

            var result = await _responseContainer.ReplaceItemAsync(
                existing, existing.Id, new PartitionKey(existing.ResponseId));

            _logger.LogInformation("Completed pending survey response {ResponseId} for panelist {PanelistId}",
                existing.ResponseId, existing.PanelistId);

            return result.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Pending response {ResponseId} not found, creating new response", request.PendingResponseId);
            // Fall back to creating a new response
            var survey = await GetSurveyAsync(request.SurveyId);
            var response = new SurveyResponse
            {
                ResponseId = $"response_{Guid.NewGuid():N}",
                SurveyId = request.SurveyId,
                CampaignId = survey!.CampaignId,
                PanelistId = request.PanelistId,
                Answers = request.Answers,
                CompletedAt = DateTime.UtcNow,
                ResponseTimeSeconds = request.ResponseTimeSeconds,
                DeviceType = request.DeviceType,
                Status = "Completed"
            };
            response.Id = response.ResponseId;
            var result = await _responseContainer.CreateItemAsync(response, new PartitionKey(response.ResponseId));
            return result.Resource;
        }
    }

    public virtual async Task<SurveyResultsResponse> GetSurveyResultsAsync(string surveyId)
    {
        var survey = await GetSurveyAsync(surveyId);
        if (survey == null)
        {
            throw new InvalidOperationException($"Survey {surveyId} not found");
        }

        var query = new QueryDefinition("SELECT * FROM c WHERE c.surveyId = @surveyId")
            .WithParameter("@surveyId", surveyId);

        var iterator = _responseContainer.GetItemQueryIterator<SurveyResponse>(query);
        var responses = new List<SurveyResponse>();

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync();
            responses.AddRange(page);
        }

        var exposedResponses = responses.Where(r => r.CohortType == "exposed").ToList();
        var controlResponses = responses.Where(r => r.CohortType == "control").ToList();

        var questionResults = new List<QuestionResult>();

        foreach (var question in survey.Questions)
        {
            var exposedAnswers = exposedResponses
                .SelectMany(r => r.Answers)
                .Where(a => a.QuestionId == question.QuestionId)
                .ToList();

            var controlAnswers = controlResponses
                .SelectMany(r => r.Answers)
                .Where(a => a.QuestionId == question.QuestionId)
                .ToList();

            double? exposedMean = null;
            double? controlMean = null;
            double? liftPercent = null;

            if (exposedAnswers.Any(a => a.NumericValue.HasValue) && controlAnswers.Any(a => a.NumericValue.HasValue))
            {
                exposedMean = exposedAnswers.Where(a => a.NumericValue.HasValue).Average(a => a.NumericValue!.Value);
                controlMean = controlAnswers.Where(a => a.NumericValue.HasValue).Average(a => a.NumericValue!.Value);

                if (controlMean > 0)
                {
                    liftPercent = ((exposedMean!.Value - controlMean.Value) / controlMean.Value) * 100;
                }
            }

            questionResults.Add(new QuestionResult
            {
                QuestionId = question.QuestionId,
                QuestionText = question.QuestionText,
                Metric = question.Metric,
                ExposedMean = exposedMean,
                ControlMean = controlMean,
                LiftPercent = liftPercent,
                ResponseCounts = exposedAnswers.GroupBy(a => a.Answer ?? "").ToDictionary(g => g.Key, g => g.Count())
            });
        }

        return new SurveyResultsResponse
        {
            SurveyId = surveyId,
            CampaignId = survey.CampaignId,
            TotalResponses = responses.Count,
            ExposedResponses = exposedResponses.Count,
            ControlResponses = controlResponses.Count,
            QuestionResults = questionResults
        };
    }

    public virtual async Task<List<SurveyResponse>> GetResponsesByPanelistAsync(string panelistId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.panelistId = @panelistId")
            .WithParameter("@panelistId", panelistId);

        var iterator = _responseContainer.GetItemQueryIterator<SurveyResponse>(query);
        var results = new List<SurveyResponse>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public virtual async Task<Models.Survey> UpdateSurveyStatusAsync(string surveyId, string status)
    {
        var survey = await GetSurveyAsync(surveyId);
        if (survey == null)
        {
            throw new InvalidOperationException($"Survey {surveyId} not found");
        }

        survey.Status = status;
        survey.UpdatedAt = DateTime.UtcNow;

        var response = await _surveyContainer.ReplaceItemAsync(survey, survey.Id, new PartitionKey(survey.SurveyId));
        _logger.LogInformation("Updated survey {SurveyId} status to {Status}", surveyId, status);

        return response.Resource;
    }
}
