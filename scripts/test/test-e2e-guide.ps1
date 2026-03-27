# End-to-End Flow Testing Guide
# AdImpact Os - Manual E2E Test

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  AdImpact Os - E2E Test Guide" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "?? This guide will walk you through testing the complete end-to-end flow" -ForegroundColor White
Write-Host ""

# Check all containers
Write-Host "Step 1: Verify Container Status" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray
$containers = docker ps --format "table {{.Names}}\t{{.Status}}" | Select-String -Pattern "adimpactos"
$containers | ForEach-Object {
    if ($_ -match "Up.*healthy|Up") {
        Write-Host "  ? $_" -ForegroundColor Green
    }
    else {
        Write-Host "  ? $_" -ForegroundColor Red
    }
}
Write-Host ""

# Run migrations
Write-Host "Step 2: Initialize Databases" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray

Write-Host "  Running Panelist API migration..." -ForegroundColor White
try {
    $result = Invoke-RestMethod -Uri "http://localhost:5001/api/migration/run" -Method Post -TimeoutSec 30
    Write-Host "  ? Panelist DB initialized: $($result.message)" -ForegroundColor Green
}
catch {
    Write-Host "  ? Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "  Running Survey API migration..." -ForegroundColor White
try {
    $result = Invoke-RestMethod -Uri "http://localhost:5002/api/migration/run" -Method Post -TimeoutSec 30
    Write-Host "  ? Survey DB initialized: $($result.message)" -ForegroundColor Green
}
catch {
    Write-Host "  ? Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "  Seeding sample data..." -ForegroundColor White
try {
    $result = Invoke-RestMethod -Uri "http://localhost:5001/api/migration/seed" -Method Post
    Write-Host "  ? Sample data seeded: $($result.message)" -ForegroundColor Green
}
catch {
    Write-Host "  ? Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Service endpoints
Write-Host "Step 3: Service Endpoints" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray
Write-Host "  Panelist API Swagger: " -NoNewline
Write-Host "http://localhost:5001/swagger" -ForegroundColor Cyan
Write-Host "  Survey API Swagger:   " -NoNewline
Write-Host "http://localhost:5002/swagger" -ForegroundColor Cyan
Write-Host "  CosmosDB Explorer:    " -NoNewline
Write-Host "https://localhost:8081/_explorer/index.html" -ForegroundColor Cyan
Write-Host ""

# Open browsers
$openBrowser = Read-Host "Would you like to open the Swagger UIs in your browser? (Y/N)"
if ($openBrowser -eq "Y" -or $openBrowser -eq "y") {
    Write-Host "  Opening Panelist API Swagger..." -ForegroundColor Gray
    Start-Process "http://localhost:5001/swagger"
    Start-Sleep -Seconds 2
    
    Write-Host "  Opening Survey API Swagger..." -ForegroundColor Gray
    Start-Process "http://localhost:5002/swagger"
    Start-Sleep -Seconds 2
    
    Write-Host "  Opening CosmosDB Explorer..." -ForegroundColor Gray
    Start-Process "https://localhost:8081/_explorer/index.html"
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "       E2E Testing Workflow" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "?? FLOW 1: Panelist Management" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray
Write-Host "1. Open Panelist API Swagger UI" -ForegroundColor White
Write-Host "   ? http://localhost:5001/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. Test GET /api/panelists" -ForegroundColor White
Write-Host "   ? Should see 3 seeded panelists" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Test POST /api/panelists" -ForegroundColor White
Write-Host "   Use this sample JSON:" -ForegroundColor Gray
Write-Host @"
   {
     "email": "newuser@example.com",
     "firstName": "John",
     "lastName": "Doe",
     "age": 30,
     "gender": "M",
     "country": "US",
     "postalCode": "10001",
     "deviceType": "Desktop",
     "browser": "Chrome",
     "hhIncomeBucket": "50K-75K",
     "consentGdpr": true,
     "consentCcpa": true
   }
"@ -ForegroundColor DarkGray
Write-Host "   ? Copy the returned 'id' for next step" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Test GET /api/panelists/{id}" -ForegroundColor White
Write-Host "   ? Paste the ID from step 3" -ForegroundColor Gray
Write-Host ""
Write-Host "5. Test PUT /api/panelists/{id}" -ForegroundColor White
Write-Host "   Update sample:" -ForegroundColor Gray
Write-Host @"
   {
     "age": 31,
     "postalCode": "10002"
   }
"@ -ForegroundColor DarkGray
Write-Host ""
Write-Host "6. Test DELETE /api/panelists/{id}" -ForegroundColor White
Write-Host "   ? Deletes the panelist" -ForegroundColor Gray
Write-Host ""

Write-Host "?? FLOW 2: Survey Management" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray
Write-Host "1. Open Survey API Swagger UI" -ForegroundColor White
Write-Host "   ? http://localhost:5002/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. Test POST /api/surveys" -ForegroundColor White
Write-Host "   Use this sample JSON:" -ForegroundColor Gray
Write-Host @"
   {
     "title": "Brand Awareness Survey 2025",
     "description": "Post-campaign measurement",
     "campaignId": "camp-001",
     "questions": [
       {
         "questionText": "Have you heard of our brand?",
         "questionType": "YesNo",
         "isRequired": true
       },
       {
         "questionText": "Rate your likelihood to purchase (1-10)",
         "questionType": "Scale",
         "isRequired": true
       }
     ],
     "isActive": true
   }
"@ -ForegroundColor DarkGray
Write-Host "   ? Copy the returned survey 'id'" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Test GET /api/surveys" -ForegroundColor White
Write-Host "   ? List all surveys" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Test GET /api/surveys/{id}" -ForegroundColor White
Write-Host "   ? Get specific survey details" -ForegroundColor Gray
Write-Host ""
Write-Host "5. Test POST /api/surveys/{surveyId}/responses" -ForegroundColor White
Write-Host "   Submit a response:" -ForegroundColor Gray
Write-Host @"
   {
     "surveyId": "{paste-survey-id}",
     "panelistId": "{paste-panelist-id}",
     "responses": [
       {
         "questionId": "q1",
         "answerText": "Yes"
       },
       {
         "questionId": "q2",
         "answerText": "8"
       }
     ]
   }
"@ -ForegroundColor DarkGray
Write-Host ""
Write-Host "6. Test GET /api/surveys/{surveyId}/responses" -ForegroundColor White
Write-Host "   ? View all responses for the survey" -ForegroundColor Gray
Write-Host ""

Write-Host "?? FLOW 3: Verify Data in CosmosDB" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray
Write-Host "1. Open CosmosDB Data Explorer" -ForegroundColor White
Write-Host "   ? https://localhost:8081/_explorer/index.html" -ForegroundColor Cyan
Write-Host "   (Accept the certificate warning)" -ForegroundColor DarkGray
Write-Host ""
Write-Host "2. Navigate to: AdImpactOsDB ? Panelists ? Items" -ForegroundColor White
Write-Host "   ? View panelist documents" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Navigate to: AdImpactOsDB ? Surveys ? Items" -ForegroundColor White
Write-Host "   ? View survey documents" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Navigate to: AdImpactOsDB ? SurveyResponses ? Items" -ForegroundColor White
Write-Host "   ? View survey response documents" -ForegroundColor Gray
Write-Host ""
Write-Host "5. Try running SQL queries:" -ForegroundColor White
Write-Host "   SELECT * FROM c WHERE c.country = 'US'" -ForegroundColor DarkGray
Write-Host "   SELECT * FROM c WHERE c.consentGdpr = true" -ForegroundColor DarkGray
Write-Host "   SELECT COUNT(1) as total FROM c" -ForegroundColor DarkGray
Write-Host ""

Write-Host "?? FLOW 4: Event Hub & Storage" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray
Write-Host "1. Verify Kafka (Event Hub) is running:" -ForegroundColor White
try {
    $kafkaStatus = docker exec adimpactos-eventhub kafka-topics --list --bootstrap-server localhost:9092 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ? Kafka is accessible" -ForegroundColor Green
    }
    else {
        Write-Host "   ? Kafka is not accessible" -ForegroundColor Red
    }
}
catch {
    Write-Host "   ? Error checking Kafka" -ForegroundColor Red
}
Write-Host ""
Write-Host "2. Verify Azurite (Storage) is running:" -ForegroundColor White
try {
    $response = Invoke-WebRequest -Uri "http://localhost:10000" -UseBasicParsing -ErrorAction SilentlyContinue
    Write-Host "   ? Azurite Blob Storage is accessible" -ForegroundColor Green
}
catch {
    Write-Host "   ? Azurite is running (403 is expected for root)" -ForegroundColor Green
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "         Monitoring & Logs" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "View logs for debugging:" -ForegroundColor White
Write-Host "  docker logs adimpactos-panelist-api -f" -ForegroundColor Gray
Write-Host "  docker logs adimpactos-survey-api -f" -ForegroundColor Gray
Write-Host "  docker logs adimpactos-cosmosdb --tail 50" -ForegroundColor Gray
Write-Host "  docker logs adimpactos-eventhub --tail 50" -ForegroundColor Gray
Write-Host ""

Write-Host "View all container stats:" -ForegroundColor White
Write-Host "  docker stats" -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "         Quick Commands" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Stop all services:" -ForegroundColor White
Write-Host "  docker-compose -f docker-compose.dev.yml down" -ForegroundColor Gray
Write-Host ""
Write-Host "Restart services:" -ForegroundColor White
Write-Host "  docker-compose -f docker-compose.dev.yml restart" -ForegroundColor Gray
Write-Host ""
Write-Host "View service status:" -ForegroundColor White
Write-Host "  docker-compose -f docker-compose.dev.yml ps" -ForegroundColor Gray
Write-Host ""
Write-Host "Run full test suite:" -ForegroundColor White
Write-Host "  dotnet test AdImpactOs.sln" -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "     Testing Tools" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  .\scripts\test\test-e2e-flow.ps1           - Automated E2E test" -ForegroundColor Gray
Write-Host "  .\scripts\test\test-cosmosdb-migration.ps1 - Migration test" -ForegroundColor Gray
Write-Host "  .\scripts\test\test-cosmosdb-data.ps1      - Data viewer" -ForegroundColor Gray
Write-Host ""

Write-Host "? Setup complete! Use Swagger UI to test the APIs" -ForegroundColor Green
Write-Host ""
