# Load All Demo Data
# Master script to generate and load all demo data into Cosmos DB

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  AdImpactOs - Demo Data Loader" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$startTime = Get-Date

# Configuration
$panelistApiUrl = "http://localhost:5001"
$surveyApiUrl = "http://localhost:5002"
$scriptPath = "demo/Scripts"
$dataPath = "demo/SampleData"

# Step 1: Generate Panelists
Write-Host "Step 1: Generating Panelist Profiles" -ForegroundColor Yellow
Write-Host "======================================" -ForegroundColor Yellow
if (-not (Test-Path "$dataPath/panelists.json")) {
    & "$scriptPath/GeneratePanelists.ps1"
} else {
    Write-Host "? Panelists data already exists: $dataPath/panelists.json" -ForegroundColor Green
}
Write-Host ""

# Step 2: Generate Impressions
Write-Host "Step 2: Generating Historical Impressions" -ForegroundColor Yellow
Write-Host "==========================================" -ForegroundColor Yellow
if (-not (Test-Path "$dataPath/impressions.json")) {
    & "$scriptPath/GenerateImpressions.ps1"
} else {
    Write-Host "? Impressions data already exists: $dataPath/impressions.json" -ForegroundColor Green
}
Write-Host ""

# Step 3: Generate Survey Responses
Write-Host "Step 3: Generating Survey Responses" -ForegroundColor Yellow
Write-Host "====================================" -ForegroundColor Yellow
if (-not (Test-Path "$dataPath/survey-responses.json")) {
    & "$scriptPath/GenerateSurveyResponses.ps1"
} else {
    Write-Host "? Survey responses already exist: $dataPath/survey-responses.json" -ForegroundColor Green
}
Write-Host ""

# Step 4: Generate Lift Results
Write-Host "Step 4: Calculating Lift Analysis Results" -ForegroundColor Yellow
Write-Host "==========================================" -ForegroundColor Yellow
if (-not (Test-Path "$dataPath/lift-results.json")) {
    & "$scriptPath/GenerateLiftResults.ps1"
} else {
    Write-Host "? Lift results already exist: $dataPath/lift-results.json" -ForegroundColor Green
}
Write-Host ""

# Step 5: Load Campaigns to Database
Write-Host "Step 5: Loading Campaigns to Database" -ForegroundColor Yellow
Write-Host "======================================" -ForegroundColor Yellow
Write-Host "  Note: Campaigns are static JSON - no API endpoint for bulk load yet" -ForegroundColor Gray
Write-Host "  Campaigns file: $dataPath/campaigns.json" -ForegroundColor Gray
Write-Host ""

# Step 6: Load Panelists to Panelist API
Write-Host "Step 6: Loading Panelists to Cosmos DB" -ForegroundColor Yellow
Write-Host "=======================================" -ForegroundColor Yellow
$panelists = Get-Content "$dataPath/panelists.json" | ConvertFrom-Json
Write-Host "  Total panelists to load: $($panelists.Count)" -ForegroundColor Gray

$loadedCount = 0
$errorCount = 0

foreach ($panelist in $panelists) {
    try {
        $body = @{
            email = $panelist.email
            phone = $panelist.phone
            firstName = $panelist.firstName
            lastName = $panelist.lastName
            age = $panelist.age
            gender = $panelist.gender
            country = $panelist.country
            postalCode = $panelist.postalCode
            deviceType = $panelist.deviceType
            browser = $panelist.browser
            consentGdpr = $panelist.consentGdpr
            consentCcpa = $panelist.consentCcpa
            consentGiven = $panelist.consentGiven
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$panelistApiUrl/api/panelists" -Method POST -Body $body -ContentType "application/json" -ErrorAction SilentlyContinue
        $loadedCount++
        
        if ($loadedCount % 100 -eq 0) {
            Write-Host "    Loaded $loadedCount panelists..." -ForegroundColor Gray
        }
    }
    catch {
        $errorCount++
        if ($errorCount -eq 1) {
            Write-Host "    Errors occurred during loading (continuing...):" -ForegroundColor Yellow
        }
    }
}

Write-Host "  ? Loaded $loadedCount panelists ($errorCount errors)" -ForegroundColor Green
Write-Host ""

# Step 7: Load Surveys to Survey API
Write-Host "Step 7: Loading Surveys to Cosmos DB" -ForegroundColor Yellow
Write-Host "=====================================" -ForegroundColor Yellow
$surveys = Get-Content "$dataPath/surveys.json" | ConvertFrom-Json
Write-Host "  Total surveys to load: $($surveys.Count)" -ForegroundColor Gray

$loadedSurveys = 0
$errorCount = 0

foreach ($survey in $surveys) {
    try {
        $body = @{
            campaignId = $survey.campaignId
            surveyName = $survey.surveyName
            description = $survey.description
            surveyType = $survey.surveyType
            questions = $survey.questions
            targetAudience = $survey.targetAudience
            distributionStartDate = $survey.distributionStartDate
            distributionEndDate = $survey.distributionEndDate
        } | ConvertTo-Json -Depth 10
        
        $response = Invoke-RestMethod -Uri "$surveyApiUrl/api/surveys" -Method POST -Body $body -ContentType "application/json" -ErrorAction SilentlyContinue
        $loadedSurveys++
        Write-Host "    Loaded survey: $($survey.surveyName)" -ForegroundColor Gray
    }
    catch {
        $errorCount++
        Write-Host "    Error loading survey: $($survey.surveyName)" -ForegroundColor Yellow
    }
}

Write-Host "  ? Loaded $loadedSurveys surveys ($errorCount errors)" -ForegroundColor Green
Write-Host ""

# Step 8: Load Survey Responses
Write-Host "Step 8: Loading Survey Responses to Cosmos DB" -ForegroundColor Yellow
Write-Host "==============================================" -ForegroundColor Yellow
$surveyResponses = Get-Content "$dataPath/survey-responses.json" | ConvertFrom-Json
Write-Host "  Total responses to load: $($surveyResponses.Count)" -ForegroundColor Gray

$loadedResponses = 0
$errorCount = 0

foreach ($response in $surveyResponses) {
    try {
        $body = @{
            surveyId = $response.surveyId
            panelistId = $response.panelistId
            cohortType = $response.cohortType
            answers = $response.answers
            responseTimeSeconds = $response.responseTimeSeconds
            deviceType = $response.deviceType
        } | ConvertTo-Json -Depth 10
        
        $result = Invoke-RestMethod -Uri "$surveyApiUrl/api/surveys/responses" -Method POST -Body $body -ContentType "application/json" -ErrorAction SilentlyContinue
        $loadedResponses++
        
        if ($loadedResponses % 100 -eq 0) {
            Write-Host "    Loaded $loadedResponses responses..." -ForegroundColor Gray
        }
    }
    catch {
        $errorCount++
        if ($errorCount -eq 1) {
            Write-Host "    Errors occurred during loading (continuing...):" -ForegroundColor Yellow
        }
    }
}

Write-Host "  ? Loaded $loadedResponses responses ($errorCount errors)" -ForegroundColor Green
Write-Host ""

# Summary
$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Demo Data Load Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  - Panelists: $($panelists.Count)" -ForegroundColor White
Write-Host "  - Campaigns: $($campaigns.Count) (static JSON)" -ForegroundColor White
Write-Host "  - Impressions: Generated ($(($impressions.Count).ToString('N0')))" -ForegroundColor White
Write-Host "  - Surveys: $loadedSurveys" -ForegroundColor White
Write-Host "  - Survey Responses: $loadedResponses" -ForegroundColor White
Write-Host "  - Lift Results: Pre-computed" -ForegroundColor White
Write-Host ""
Write-Host "Duration: $($duration.TotalSeconds) seconds" -ForegroundColor Gray
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. View Panelist API: $panelistApiUrl/swagger" -ForegroundColor White
Write-Host "  2. View Survey API: $surveyApiUrl/swagger" -ForegroundColor White
Write-Host "  3. Query Cosmos DB: https://localhost:8081/_explorer" -ForegroundColor White
Write-Host "  4. Start Traffic Simulator to generate live impressions" -ForegroundColor White
Write-Host ""
