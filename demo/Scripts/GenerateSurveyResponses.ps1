# Generate Survey Responses
# Creates realistic survey responses with brand lift for exposed vs control groups

$ErrorActionPreference = "Stop"

Write-Host "=== Survey Response Generator ===" -ForegroundColor Cyan
Write-Host ""

# Load data files
$surveysFile = "demo/SampleData/surveys.json"
$panelistsFile = "demo/SampleData/panelists.json"
$outputFile = "demo/SampleData/survey-responses.json"

Write-Host "Loading surveys from $surveysFile..." -ForegroundColor Yellow
$surveys = Get-Content $surveysFile | ConvertFrom-Json

Write-Host "Loading panelists from $panelistsFile..." -ForegroundColor Yellow
$panelists = Get-Content $panelistsFile | ConvertFrom-Json

Write-Host ""

# Configuration
$responseRateExposed = 0.25  # 25% response rate for exposed
$responseRateControl = 0.20  # 20% response rate for control
$liftPercentages = @{
    brand_awareness = 18
    purchase_intent = 12
    brand_favorability = 15
    message_recall = 65  # Much higher for exposed
    brand_consideration = 14
    value_perception = 10
    brand_perception = 16
    application_intent = 11
    booking_intent = 13
    download_intent = 15
}

# Likert scale mapping (1-5)
$likertMapping = @{
    1 = "Not at all familiar|Not at all likely|Very unfavorable"
    2 = "Slightly familiar|Slightly likely|Somewhat unfavorable"
    3 = "Moderately familiar|Moderately likely|Neutral"
    4 = "Very familiar|Very likely|Somewhat favorable"
    5 = "Extremely familiar|Extremely likely|Very favorable"
}

function Get-ExposedScore {
    param($baseScore, $liftPercent)
    $lifted = $baseScore * (1 + ($liftPercent / 100))
    return [math]::Min([math]::Round($lifted), 5)
}

function Get-ControlScore {
    param($questionType, $metric)
    # Control group baseline scores
    switch ($metric) {
        "brand_awareness" { return Get-Random -Minimum 2 -Maximum 4 }
        "purchase_intent" { return Get-Random -Minimum 2 -Maximum 3 }
        "brand_favorability" { return Get-Random -Minimum 2 -Maximum 4 }
        "message_recall" { return $false }  # Control rarely recalls
        "brand_consideration" { return Get-Random -Minimum 4 -Maximum 7 }
        "value_perception" { return Get-Random -Minimum 5 -Maximum 7 }
        "brand_perception" { return Get-Random -Minimum 5 -Maximum 7 }
        default { return Get-Random -Minimum 2 -Maximum 4 }
    }
}

function Get-ExposedAnswer {
    param($question, $cohortType)
    
    $baseScore = Get-ControlScore -questionType $question.questionType -metric $question.metric
    
    if ($cohortType -eq "control") {
        # Control group - baseline scores
        if ($question.questionType -eq "YesNo") {
            $answer = if ((Get-Random -Minimum 0 -Maximum 100) -lt 15) { "Yes" } else { "No" }
            return @{
                questionId = $question.questionId
                answer = $answer
                numericValue = if ($answer -eq "Yes") { 1 } else { 0 }
            }
        }
        elseif ($question.questionType -eq "Rating") {
            return @{
                questionId = $question.questionId
                answer = $baseScore.ToString()
                numericValue = $baseScore
            }
        }
        else {
            # Likert scale
            $answerText = $question.options[$baseScore - 1]
            return @{
                questionId = $question.questionId
                answer = $answerText
                numericValue = $baseScore
            }
        }
    }
    else {
        # Exposed group - apply lift
        $liftPercent = $liftPercentages[$question.metric]
        if (-not $liftPercent) { $liftPercent = 15 }
        
        if ($question.questionType -eq "YesNo") {
            # Much higher recall for exposed group
            $recallRate = if ($question.metric -eq "message_recall") { 65 } else { 30 }
            $answer = if ((Get-Random -Minimum 0 -Maximum 100) -lt $recallRate) { "Yes" } else { "No" }
            return @{
                questionId = $question.questionId
                answer = $answer
                numericValue = if ($answer -eq "Yes") { 1 } else { 0 }
            }
        }
        elseif ($question.questionType -eq "Rating") {
            $liftedScore = Get-ExposedScore -baseScore $baseScore -liftPercent $liftPercent
            $liftedScore = [math]::Min($liftedScore, $question.scale)
            return @{
                questionId = $question.questionId
                answer = $liftedScore.ToString()
                numericValue = $liftedScore
            }
        }
        else {
            # Likert scale with lift
            $liftedScore = Get-ExposedScore -baseScore $baseScore -liftPercent $liftPercent
            $answerText = $question.options[$liftedScore - 1]
            return @{
                questionId = $question.questionId
                answer = $answerText
                numericValue = $liftedScore
            }
        }
    }
}

Write-Host "Generating survey responses..." -ForegroundColor Yellow
Write-Host ""

$responses = @()
$responseCount = 0

foreach ($survey in $surveys) {
    Write-Host "Processing survey: $($survey.surveyName)" -ForegroundColor Cyan
    
    # Filter panelists who have consent
    $eligiblePanelists = $panelists | Where-Object { $_.consentGiven -eq $true -and $_.cohortType -ne $null }
    
    # Separate exposed and control
    $exposedPanelists = $eligiblePanelists | Where-Object { $_.cohortType -eq "exposed" }
    $controlPanelists = $eligiblePanelists | Where-Object { $_.cohortType -eq "control" }
    
    # Calculate response counts
    $exposedResponders = [math]::Floor($exposedPanelists.Count * $responseRateExposed)
    $controlResponders = [math]::Floor($controlPanelists.Count * $responseRateControl)
    
    Write-Host "  Exposed respondents: $exposedResponders / $($exposedPanelists.Count)" -ForegroundColor Gray
    Write-Host "  Control respondents: $controlResponders / $($controlPanelists.Count)" -ForegroundColor Gray
    
    # Generate responses for exposed group
    $selectedExposed = $exposedPanelists | Get-Random -Count $exposedResponders
    foreach ($panelist in $selectedExposed) {
        $answers = @()
        foreach ($question in $survey.questions) {
            $answers += Get-ExposedAnswer -question $question -cohortType "exposed"
        }
        
        $responseId = "response_$($responseCount.ToString("D7"))"
        $response = [PSCustomObject]@{
            responseId = $responseId
            id = $responseId
            surveyId = $survey.surveyId
            campaignId = $survey.campaignId
            panelistId = $panelist.panelistId
            cohortType = "exposed"
            answers = $answers
            completedAt = (Get-Date).AddDays(-(Get-Random -Minimum 0 -Maximum 30)).ToString("o")
            responseTimeSeconds = Get-Random -Minimum 45 -Maximum 180
            deviceType = $panelist.deviceType
            status = "Completed"
        }
        
        $responses += $response
        $responseCount++
    }
    
    # Generate responses for control group
    $selectedControl = $controlPanelists | Get-Random -Count $controlResponders
    foreach ($panelist in $selectedControl) {
        $answers = @()
        foreach ($question in $survey.questions) {
            $answers += Get-ExposedAnswer -question $question -cohortType "control"
        }
        
        $responseId = "response_$($responseCount.ToString("D7"))"
        $response = [PSCustomObject]@{
            responseId = $responseId
            id = $responseId
            surveyId = $survey.surveyId
            campaignId = $survey.campaignId
            panelistId = $panelist.panelistId
            cohortType = "control"
            answers = $answers
            completedAt = (Get-Date).AddDays(-(Get-Random -Minimum 0 -Maximum 30)).ToString("o")
            responseTimeSeconds = Get-Random -Minimum 45 -Maximum 180
            deviceType = $panelist.deviceType
            status = "Completed"
        }
        
        $responses += $response
        $responseCount++
    }
    
    Write-Host "  Generated $($exposedResponders + $controlResponders) responses" -ForegroundColor Green
    Write-Host ""
}

Write-Host "Converting to JSON..." -ForegroundColor Yellow
$json = $responses | ConvertTo-Json -Depth 10

Write-Host "Writing to file: $outputFile" -ForegroundColor Yellow
$json | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host ""
Write-Host "? Successfully generated survey responses!" -ForegroundColor Green
Write-Host ""
Write-Host "Statistics:" -ForegroundColor Cyan
Write-Host "  - Total responses: $(($responses.Count).ToString('N0'))"
Write-Host "  - Exposed responses: $(($responses | Where-Object { $_.cohortType -eq 'exposed' }).Count)"
Write-Host "  - Control responses: $(($responses | Where-Object { $_.cohortType -eq 'control' }).Count)"
Write-Host "  - Surveys covered: $($surveys.Count)"
Write-Host ""
Write-Host "File saved to: $outputFile" -ForegroundColor Green
