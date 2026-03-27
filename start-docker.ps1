# AdImpact Os - Docker Quick Start Script

Write-Host "=== Ad Tracking JiangXi - Docker Startup ===" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is running
Write-Host "Checking Docker Desktop status..." -ForegroundColor Yellow
try {
    docker info 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker not running"
    }
    Write-Host "? Docker Desktop is running" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Docker Desktop is not running!" -ForegroundColor Red
    Write-Host "Please start Docker Desktop and try again." -ForegroundColor Red
    exit 1
}

# Ask user which stack to start
Write-Host ""
Write-Host "Select deployment option:" -ForegroundColor Cyan
Write-Host "1. Development Stack (Lightweight - Recommended)" -ForegroundColor White
Write-Host "   - Cosmos DB, Kafka, Azurite, Panelist API" -ForegroundColor Gray
Write-Host "   - ~4GB RAM required" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Full Stack (Heavy)" -ForegroundColor White
Write-Host "   - All above + Spark cluster + Azure Functions + Event Consumer" -ForegroundColor Gray
Write-Host "   - ~8GB RAM required" -ForegroundColor Gray
Write-Host ""
$choice = Read-Host "Enter choice (1 or 2)"

$composeFile = ""
if ($choice -eq "1") {
    $composeFile = "docker-compose.dev.yml"
    Write-Host "Starting Development Stack..." -ForegroundColor Cyan
} elseif ($choice -eq "2") {
    $composeFile = "docker-compose.yml"
    Write-Host "Starting Full Stack..." -ForegroundColor Cyan
} else {
    Write-Host "Invalid choice. Exiting." -ForegroundColor Red
    exit 1
}

# Stop any existing containers
Write-Host ""
Write-Host "Stopping existing containers..." -ForegroundColor Yellow
docker-compose -f $composeFile down 2>&1 | Out-Null

# Start services
Write-Host "Starting services (this may take a few minutes)..." -ForegroundColor Yellow
docker-compose -f $composeFile up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: Failed to start services!" -ForegroundColor Red
    Write-Host "Check Docker Desktop logs for details." -ForegroundColor Red
    exit 1
}

# Wait for services to be healthy
Write-Host ""
Write-Host "Waiting for services to be ready..." -ForegroundColor Yellow
Write-Host "This can take 2-3 minutes for first-time startup..." -ForegroundColor Gray
Start-Sleep -Seconds 15

# Check service status
Write-Host ""
Write-Host "Service Status:" -ForegroundColor Cyan
docker-compose -f $composeFile ps

# Test if Panelist API is responding
Write-Host ""
Write-Host "Testing Panelist API..." -ForegroundColor Yellow
$retries = 0
$maxRetries = 30
$apiReady = $false

while ($retries -lt $maxRetries -and -not $apiReady) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5001/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            $apiReady = $true
            Write-Host "? Panelist API is responding" -ForegroundColor Green
        }
    } catch {
        $retries++
        if ($retries -lt $maxRetries) {
            Write-Host "  Waiting for API... ($retries/$maxRetries)" -ForegroundColor Gray
            Start-Sleep -Seconds 2
        }
    }
}

if (-not $apiReady) {
    Write-Host "? API took longer than expected to start. It may still be initializing." -ForegroundColor Yellow
    Write-Host "  Check logs: docker-compose -f $composeFile logs panelist-api" -ForegroundColor Gray
}

# Display endpoints
Write-Host ""
Write-Host "=== Service Endpoints ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "--- Infrastructure ---" -ForegroundColor DarkCyan
Write-Host "Cosmos DB Explorer:  https://localhost:8081/_explorer/index.html" -ForegroundColor White
Write-Host "Kafka (EventHub):    localhost:29092" -ForegroundColor White
Write-Host "Azurite Storage:     http://localhost:10000 (Blob) / :10001 (Queue) / :10002 (Table)" -ForegroundColor White
Write-Host ""
Write-Host "--- Microservice APIs ---" -ForegroundColor DarkCyan
Write-Host "Panelist API:        http://localhost:5001          Swagger: http://localhost:5001/swagger" -ForegroundColor White
Write-Host "Survey API:          http://localhost:5002          Swagger: http://localhost:5002/swagger" -ForegroundColor White
Write-Host "Campaign API:        http://localhost:5003          Swagger: http://localhost:5003/swagger" -ForegroundColor White
Write-Host ""
Write-Host "--- Azure Functions (Ad Tracking) ---" -ForegroundColor DarkCyan
Write-Host "Pixel Tracker:       http://localhost:7071/api/pixel?cid=CAMPAIGN&crid=CREATIVE&uid=USER" -ForegroundColor White
Write-Host "S2S Tracker:         http://localhost:7071/api/s2s/track  (POST)" -ForegroundColor White
Write-Host ""
Write-Host "--- Web UIs ---" -ForegroundColor DarkCyan
Write-Host "Dashboard:           http://localhost:5004" -ForegroundColor White
Write-Host "Demo UI (External):  http://localhost:5010" -ForegroundColor White
Write-Host "Survey Take Page:    http://localhost:5002/survey/take/{token}  (generated via Trigger)" -ForegroundColor White

# Display test commands
Write-Host "=== Quick Test Commands ===" -ForegroundColor Cyan
Write-Host "Get All Panelists:" -ForegroundColor Yellow
Write-Host '  curl http://localhost:5001/api/panelists' -ForegroundColor Gray
Write-Host ""
Write-Host "Get All Campaigns:" -ForegroundColor Yellow
Write-Host '  curl http://localhost:5003/api/campaigns' -ForegroundColor Gray
Write-Host ""
Write-Host "Get All Surveys:" -ForegroundColor Yellow
Write-Host '  curl http://localhost:5002/api/surveys' -ForegroundColor Gray
Write-Host ""
Write-Host "Test Pixel Tracker:" -ForegroundColor Yellow
Write-Host '  curl "http://localhost:7071/api/pixel?cid=campaign_summer_beverage_2024&crid=creative_banner_728x90_v1&uid=panelist_001"' -ForegroundColor Gray
Write-Host ""
Write-Host "Create Panelist:" -ForegroundColor Yellow
Write-Host '  curl -X POST http://localhost:5001/api/panelists -H "Content-Type: application/json" -d "{\"email\":\"test@example.com\",\"firstName\":\"John\",\"lastName\":\"Doe\",\"age\":30,\"consentGdpr\":true}"' -ForegroundColor Gray
Write-Host ""

Write-Host "View Logs:" -ForegroundColor Yellow
Write-Host "  docker-compose -f $composeFile logs -f" -ForegroundColor Gray
Write-Host ""
Write-Host "Stop Services:" -ForegroundColor Yellow
Write-Host "  docker-compose -f $composeFile down" -ForegroundColor Gray
Write-Host ""

Write-Host "=== Startup Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Open Dashboard:     http://localhost:5004" -ForegroundColor White
Write-Host "2. Open Demo UI:       http://localhost:5010" -ForegroundColor White
Write-Host "3. Explore Swagger:    http://localhost:5001/swagger" -ForegroundColor White
Write-Host "4. View logs:          docker-compose -f $composeFile logs -f" -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C to exit (services will keep running)" -ForegroundColor Gray
Write-Host ""

# Optionally follow logs
$followLogs = Read-Host "Follow logs now? (y/n)"
if ($followLogs -eq "y" -or $followLogs -eq "Y") {
    docker-compose -f $composeFile logs -f
}
