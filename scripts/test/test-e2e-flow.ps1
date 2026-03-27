# End-to-End Flow Test Script for AdImpact Os
# This script tests the complete flow from panelist registration to survey completion

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  AdImpact Os - E2E Flow Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Continue"
$testsPassed = 0
$testsFailed = 0

function Test-Step {
    param(
        [string]$StepNumber,
        [string]$Description,
        [scriptblock]$Test
    )
    
    Write-Host "$StepNumber. $Description" -ForegroundColor Yellow
    try {
        & $Test
        $script:testsPassed++
        Write-Host "   ? PASSED" -ForegroundColor Green
        Write-Host ""
        return $true
    } catch {
        $script:testsFailed++
        Write-Host "   ? FAILED: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        return $false
    }
}

# Step 0: Check Container Health
Test-Step "0" "Verify all containers are running" {
    $containers = @(
        "adtracking-cosmosdb",
        "adtracking-panelist-api",
        "adtracking-survey-api",
        "adtracking-eventhub",
        "adtracking-zookeeper",
        "adtracking-azurite"
    )
    
    foreach ($container in $containers) {
        $status = docker inspect --format='{{.State.Status}}' $container 2>$null
        if ($status -ne "running") {
            throw "Container $container is not running (status: $status)"
        }
        Write-Host "   ? $container is running" -ForegroundColor Gray
    }
}

# Step 1: Run Database Migration
Test-Step "1" "Run CosmosDB migration" {
    $result = Invoke-RestMethod -Uri "http://localhost:5001/api/migration/run" -Method Post -TimeoutSec 30
    if ($result.message -notmatch "success") {
        throw "Migration did not complete successfully"
    }
    Write-Host "   Database: AdTrackingDB created" -ForegroundColor Gray
    Write-Host "   Container: Panelists created" -ForegroundColor Gray
}

# Step 2: Seed Sample Data (Optional - can also test with fresh data)
$seedData = Read-Host "   Seed sample panelist data? (Y/N)"
if ($seedData -eq "Y" -or $seedData -eq "y") {
    Test-Step "2" "Seed sample panelist data" {
        $result = Invoke-RestMethod -Uri "http://localhost:5001/api/migration/seed" -Method Post
        Write-Host "   3 sample panelists created" -ForegroundColor Gray
    }
} else {
    Write-Host "2. Skipping sample data seeding" -ForegroundColor Yellow
    Write-Host ""
}

# Step 3: Create a New Panelist
$panelistId = $null
Test-Step "3" "Create a new panelist" {
    $newPanelist = @{
        email = "e2etest.$(Get-Random -Maximum 99999)@example.com"
        firstName = "E2E"
        lastName = "TestUser"
        age = 28
        gender = "F"
        country = "US"
        postalCode = "10001"
        deviceType = "Desktop"
        browser = "Chrome"
        hhIncomeBucket = "50K-75K"
        consentGdpr = $true
        consentCcpa = $true
    } | ConvertTo-Json
    
    $result = Invoke-RestMethod -Uri "http://localhost:5001/api/panelists" `
        -Method Post `
        -Body $newPanelist `
        -ContentType "application/json"
    
    $script:panelistId = $result.id
    Write-Host "   Panelist ID: $($result.id)" -ForegroundColor Gray
    Write-Host "   Email: $($result.email)" -ForegroundColor Gray
    Write-Host "   Name: $($result.firstName) $($result.lastName)" -ForegroundColor Gray
}

# Step 4: Retrieve the Panelist
Test-Step "4" "Retrieve panelist by ID" {
    if (-not $script:panelistId) {
        throw "No panelist ID available from previous step"
    }
    
    $panelist = Invoke-RestMethod -Uri "http://localhost:5001/api/panelists/$($script:panelistId)" -Method Get
    
    if ($panelist.id -ne $script:panelistId) {
        throw "Retrieved panelist ID doesn't match"
    }
    
    Write-Host "   Retrieved: $($panelist.firstName) $($panelist.lastName)" -ForegroundColor Gray
    Write-Host "   Status: Active = $($panelist.isActive)" -ForegroundColor Gray
}

# Step 5: Update Panelist Information
Test-Step "5" "Update panelist information" {
    if (-not $script:panelistId) {
        throw "No panelist ID available"
    }
    
    $updateData = @{
        age = 29
        postalCode = "10002"
    } | ConvertTo-Json
    
    $updated = Invoke-RestMethod -Uri "http://localhost:5001/api/panelists/$($script:panelistId)" `
        -Method Put `
        -Body $updateData `
        -ContentType "application/json"
    
    if ($updated.age -ne 29) {
        throw "Age was not updated correctly"
    }
    
    Write-Host "   Age updated: 28 ? 29" -ForegroundColor Gray
    Write-Host "   Postal code updated: 10001 ? 10002" -ForegroundColor Gray
}

# Step 6: List All Panelists
Test-Step "6" "List all panelists" {
    $allPanelists = Invoke-RestMethod -Uri "http://localhost:5001/api/panelists" -Method Get
    
    if ($allPanelists.Count -eq 0) {
        throw "No panelists found in database"
    }
    
    Write-Host "   Total panelists in database: $($allPanelists.Count)" -ForegroundColor Gray
    
    # Find our test panelist
    $ourPanelist = $allPanelists | Where-Object { $_.id -eq $script:panelistId }
    if (-not $ourPanelist) {
        throw "Could not find our test panelist in the list"
    }
    Write-Host "   ? Test panelist found in list" -ForegroundColor Gray
}

# Step 7: Run Survey Migration
Test-Step "7" "Run Survey API migration" {
    $result = Invoke-RestMethod -Uri "http://localhost:5002/api/migration/run" -Method Post -TimeoutSec 30
    if ($result.message -notmatch "success") {
        throw "Survey migration did not complete successfully"
    }
    Write-Host "   Surveys container created" -ForegroundColor Gray
    Write-Host "   SurveyResponses container created" -ForegroundColor Gray
}

# Step 8: Create a Survey
$surveyId = $null
Test-Step "8" "Create a brand awareness survey" {
    $newSurvey = @{
        title = "E2E Test - Brand Awareness Survey"
        description = "Post-campaign brand awareness measurement"
        campaignId = "camp-e2e-test-001"
        questions = @(
            @{
                questionText = "Have you heard of our brand before?"
                questionType = "YesNo"
                isRequired = $true
            },
            @{
                questionText = "How likely are you to purchase our product?"
                questionType = "Scale"
                isRequired = $true
            }
        )
        isActive = $true
    } | ConvertTo-Json -Depth 10
    
    $result = Invoke-RestMethod -Uri "http://localhost:5002/api/surveys" `
        -Method Post `
        -Body $newSurvey `
        -ContentType "application/json"
    
    $script:surveyId = $result.id
    Write-Host "   Survey ID: $($result.id)" -ForegroundColor Gray
    Write-Host "   Title: $($result.title)" -ForegroundColor Gray
    Write-Host "   Questions: $($result.questions.Count)" -ForegroundColor Gray
}

# Step 9: Retrieve Survey
Test-Step "9" "Retrieve survey by ID" {
    if (-not $script:surveyId) {
        throw "No survey ID available"
    }
    
    $survey = Invoke-RestMethod -Uri "http://localhost:5002/api/surveys/$($script:surveyId)" -Method Get
    
    if ($survey.id -ne $script:surveyId) {
        throw "Retrieved survey ID doesn't match"
    }
    
    Write-Host "   Survey: $($survey.title)" -ForegroundColor Gray
    Write-Host "   Active: $($survey.isActive)" -ForegroundColor Gray
}

# Step 10: Submit Survey Response
$responseId = $null
Test-Step "10" "Submit survey response from panelist" {
    if (-not $script:surveyId -or -not $script:panelistId) {
        throw "Missing survey ID or panelist ID"
    }
    
    $surveyResponse = @{
        surveyId = $script:surveyId
        panelistId = $script:panelistId
        responses = @(
            @{
                questionId = "q1"
                answerText = "Yes"
            },
            @{
                questionId = "q2"
                answerText = "8"
            }
        )
    } | ConvertTo-Json -Depth 10
    
    $result = Invoke-RestMethod -Uri "http://localhost:5002/api/surveys/$($script:surveyId)/responses" `
        -Method Post `
        -Body $surveyResponse `
        -ContentType "application/json"
    
    $script:responseId = $result.id
    Write-Host "   Response ID: $($result.id)" -ForegroundColor Gray
    Write-Host "   Panelist: $($script:panelistId)" -ForegroundColor Gray
    Write-Host "   Answers submitted: $($result.responses.Count)" -ForegroundColor Gray
}

# Step 11: Retrieve Survey Responses
Test-Step "11" "Retrieve all responses for survey" {
    if (-not $script:surveyId) {
        throw "No survey ID available"
    }
    
    $responses = Invoke-RestMethod -Uri "http://localhost:5002/api/surveys/$($script:surveyId)/responses" -Method Get
    
    if ($responses.Count -eq 0) {
        throw "No responses found for survey"
    }
    
    Write-Host "   Total responses: $($responses.Count)" -ForegroundColor Gray
    
    # Find our response
    $ourResponse = $responses | Where-Object { $_.id -eq $script:responseId }
    if (-not $ourResponse) {
        throw "Could not find our test response"
    }
    Write-Host "   ? Test response found" -ForegroundColor Gray
}

# Step 12: Test Event Hub Connection (Kafka)
Test-Step "12" "Verify Event Hub (Kafka) is accessible" {
    $kafkaStatus = docker exec adtracking-eventhub kafka-topics --list --bootstrap-server localhost:9092 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Cannot connect to Kafka"
    }
    Write-Host "   Kafka broker is accessible" -ForegroundColor Gray
}

# Step 13: Test Azurite Storage
Test-Step "13" "Verify Azurite storage is accessible" {
    $response = Invoke-WebRequest -Uri "http://localhost:10000/devstoreaccount1?comp=list" -UseBasicParsing -ErrorAction SilentlyContinue
    if ($response.StatusCode -ne 200) {
        throw "Cannot connect to Azurite"
    }
    Write-Host "   Blob storage is accessible" -ForegroundColor Gray
}

# Step 14: Test CosmosDB Data Explorer
Test-Step "14" "Verify CosmosDB Explorer is accessible" {
    $response = Invoke-WebRequest -Uri "https://localhost:8081/_explorer/index.html" -SkipCertificateCheck -UseBasicParsing -ErrorAction SilentlyContinue
    if ($response.StatusCode -ne 200) {
        throw "Cannot access CosmosDB Explorer"
    }
    Write-Host "   CosmosDB Explorer is accessible" -ForegroundColor Gray
}

# Step 15: Cleanup (Optional)
Write-Host ""
$cleanup = Read-Host "15. Would you like to clean up test data? (Y/N)"
if ($cleanup -eq "Y" -or $cleanup -eq "y") {
    Write-Host "   Cleaning up..." -ForegroundColor Yellow
    
    # Delete survey response
    if ($script:responseId) {
        try {
            Invoke-RestMethod -Uri "http://localhost:5002/api/surveys/$($script:surveyId)/responses/$($script:responseId)" -Method Delete -ErrorAction SilentlyContinue
            Write-Host "   ? Deleted survey response" -ForegroundColor Gray
        } catch {
            Write-Host "   ? Could not delete survey response" -ForegroundColor Yellow
        }
    }
    
    # Delete survey
    if ($script:surveyId) {
        try {
            Invoke-RestMethod -Uri "http://localhost:5002/api/surveys/$($script:surveyId)" -Method Delete -ErrorAction SilentlyContinue
            Write-Host "   ? Deleted survey" -ForegroundColor Gray
        } catch {
            Write-Host "   ? Could not delete survey" -ForegroundColor Yellow
        }
    }
    
    # Delete panelist
    if ($script:panelistId) {
        try {
            Invoke-RestMethod -Uri "http://localhost:5001/api/panelists/$($script:panelistId)" -Method Delete -ErrorAction SilentlyContinue
            Write-Host "   ? Deleted panelist" -ForegroundColor Gray
        } catch {
            Write-Host "   ? Could not delete panelist" -ForegroundColor Yellow
        }
    }
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "           Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Tests Passed: $testsPassed" -ForegroundColor Green
Write-Host "Tests Failed: $testsFailed" -ForegroundColor $(if ($testsFailed -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($testsFailed -eq 0) {
    Write-Host "? All E2E tests passed successfully!" -ForegroundColor Green
} else {
    Write-Host "? Some tests failed. Check the output above." -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "        Service Endpoints" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Panelist API:       http://localhost:5001" -ForegroundColor White
Write-Host "Panelist Swagger:   http://localhost:5001/swagger" -ForegroundColor White
Write-Host "Survey API:         http://localhost:5002" -ForegroundColor White
Write-Host "Survey Swagger:     http://localhost:5002/swagger" -ForegroundColor White
Write-Host "CosmosDB Explorer:  https://localhost:8081/_explorer/index.html" -ForegroundColor White
Write-Host "Event Hub (Kafka):  localhost:9092" -ForegroundColor White
Write-Host "Azurite Storage:    http://localhost:10000" -ForegroundColor White
Write-Host ""

Write-Host "Test IDs for manual verification:" -ForegroundColor Cyan
Write-Host "  Panelist ID: $panelistId" -ForegroundColor Gray
Write-Host "  Survey ID: $surveyId" -ForegroundColor Gray
Write-Host "  Response ID: $responseId" -ForegroundColor Gray
Write-Host ""
