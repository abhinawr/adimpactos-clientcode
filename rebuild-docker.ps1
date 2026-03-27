# AdImpact Os - Rebuild Docker Containers & Run Migrations
# This script tears down all containers, rebuilds images, and starts fresh.
# Migrations and data seeding run automatically on startup.

param(
    [string]$ComposeFile = "docker-compose.dev.yml",
    [switch]$FullStack,
    [switch]$CleanVolumes
)

$ErrorActionPreference = "Stop"

if ($FullStack) { $ComposeFile = "docker-compose.yml" }

Write-Host "=== AdImpact Os - Rebuild ===" -ForegroundColor Cyan
Write-Host "Compose file: $ComposeFile" -ForegroundColor Gray
Write-Host ""

# Check Docker
try {
    docker info 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Docker not running" }
} catch {
    Write-Host "ERROR: Docker Desktop is not running!" -ForegroundColor Red
    exit 1
}

# Tear down
Write-Host "[1/5] Stopping and removing containers..." -ForegroundColor Yellow
if ($CleanVolumes) {
    docker-compose -f $ComposeFile down -v --remove-orphans 2>&1 | Out-Null
    Write-Host "  Volumes removed (clean slate)" -ForegroundColor Gray
} else {
    docker-compose -f $ComposeFile down --remove-orphans 2>&1 | Out-Null
}

# Rebuild images
Write-Host "[2/5] Rebuilding Docker images (no cache)..." -ForegroundColor Yellow
docker-compose -f $ComposeFile build --no-cache
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Docker build failed!" -ForegroundColor Red
    exit 1
}

# Start infrastructure first (cosmosdb needs time)
Write-Host "[3/5] Starting infrastructure services..." -ForegroundColor Yellow
docker-compose -f $ComposeFile up -d cosmosdb zookeeper eventhub azurite
Write-Host "  Waiting for Cosmos DB emulator to be healthy (up to 3 minutes)..." -ForegroundColor Gray

$retries = 0
$maxRetries = 36  # 3 minutes at 5s intervals
$cosmosReady = $false
while ($retries -lt $maxRetries -and -not $cosmosReady) {
    $health = docker inspect --format='{{.State.Health.Status}}' adimpactos-cosmosdb 2>&1
    if ($health -eq "healthy") {
        $cosmosReady = $true
        Write-Host "  Cosmos DB is healthy" -ForegroundColor Green
    } else {
        $retries++
        Write-Host "  Cosmos DB: $health ($retries/$maxRetries)" -ForegroundColor Gray
        Start-Sleep -Seconds 5
    }
}

if (-not $cosmosReady) {
    Write-Host "WARNING: Cosmos DB not healthy yet, continuing anyway..." -ForegroundColor Yellow
}

# Start all services
Write-Host "[4/5] Starting all services..." -ForegroundColor Yellow
docker-compose -f $ComposeFile up -d
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to start services!" -ForegroundColor Red
    exit 1
}

# Wait for APIs and migrations to complete
Write-Host "[5/5] Waiting for APIs to be ready (migrations run automatically on startup)..." -ForegroundColor Yellow

$services = @(
    @{ Name = "Panelist API"; Url = "http://localhost:5001/api/panelists"; Container = "adimpactos-panelist-api" },
    @{ Name = "Survey API";   Url = "http://localhost:5002/api/surveys";   Container = "adimpactos-survey-api" },
    @{ Name = "Campaign API"; Url = "http://localhost:5003/api/campaigns"; Container = "adimpactos-campaign-api" },
    @{ Name = "Dashboard";    Url = "http://localhost:5004";               Container = "adimpactos-dashboard" }
)

foreach ($svc in $services) {
    $ready = $false
    $retries = 0
    $maxRetries = 40  # ~80 seconds
    while ($retries -lt $maxRetries -and -not $ready) {
        try {
            $resp = Invoke-WebRequest -Uri $svc.Url -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
            if ($resp.StatusCode -eq 200) {
                $ready = $true
                Write-Host "  $($svc.Name) is ready" -ForegroundColor Green
            }
        } catch {
            $retries++
            if ($retries % 5 -eq 0) {
                Write-Host "  Waiting for $($svc.Name)... ($retries/$maxRetries)" -ForegroundColor Gray
            }
            Start-Sleep -Seconds 2
        }
    }
    if (-not $ready) {
        Write-Host "  $($svc.Name) not responding yet. Check: docker logs $($svc.Container)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== Rebuild Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "All services started with auto-migration and seeding." -ForegroundColor White
Write-Host "Migrations create Cosmos DB containers and seed sample data automatically." -ForegroundColor Gray
Write-Host ""
Write-Host "Endpoints:" -ForegroundColor Cyan
Write-Host "  Dashboard:     http://localhost:5004" -ForegroundColor White
Write-Host "  Reports:       http://localhost:5004/Reports" -ForegroundColor White
Write-Host "  Panelist API:  http://localhost:5001/swagger" -ForegroundColor White
Write-Host "  Survey API:    http://localhost:5002/swagger" -ForegroundColor White
Write-Host "  Campaign API:  http://localhost:5003/swagger" -ForegroundColor White
Write-Host ""
Write-Host "View logs:" -ForegroundColor Yellow
Write-Host "  docker-compose -f $ComposeFile logs -f" -ForegroundColor Gray
Write-Host ""
