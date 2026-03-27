# AdImpact Os - Local Development Startup Script
# =====================================================
# Runs infrastructure in Docker, APIs locally via 'dotnet run',
# waits for migrations to complete, then launches the UI projects.
#
# Usage:  .\start-local.ps1
# Stop:   Press Ctrl+C (script cleans up all child processes on exit)

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot
if (-not $scriptRoot) { $scriptRoot = (Get-Location).Path }

# ?? Colour helpers ??????????????????????????????????????????????
function Write-Step  { param($msg) Write-Host "`n>> $msg" -ForegroundColor Cyan }
function Write-Ok    { param($msg) Write-Host "   OK  $msg" -ForegroundColor Green }
function Write-Warn  { param($msg) Write-Host "   !   $msg" -ForegroundColor Yellow }
function Write-Err   { param($msg) Write-Host "   ERR $msg" -ForegroundColor Red }
function Write-Info  { param($msg) Write-Host "       $msg" -ForegroundColor Gray }

# ?? Configuration ???????????????????????????????????????????????
$composeFile      = Join-Path $scriptRoot "docker-compose.infra.yml"
$solutionRoot     = $scriptRoot

$apiProjects = @(
    @{ Name = "PanelistAPI"; Path = "src\AdImpactOs.PanelistAPI"; Port = 5001; HealthPath = "/api/panelists" },
    @{ Name = "SurveyAPI";   Path = "src\AdImpactOs.Survey";      Port = 5002; HealthPath = "/api/surveys" },
    @{ Name = "CampaignAPI"; Path = "src\AdImpactOs.Campaign";    Port = 5003; HealthPath = "/api/campaigns" }
)

$uiProjects = @(
    @{ Name = "Dashboard"; Path = "src\AdImpactOs.Dashboard"; Port = 5004; HealthPath = "/" },
    @{ Name = "DemoUI";    Path = "src\AdImpactOs.DemoUI";    Port = 5010; HealthPath = "/" }
)

# Track background jobs so we can clean up on exit
$script:bgJobs = @()

# ?? Cleanup handler ????????????????????????????????????????????
function Stop-Everything {
    Write-Host ""
    Write-Step "Shutting down..."

    foreach ($job in $script:bgJobs) {
        if ($job -and $job.Id) {
            try {
                Stop-Job -Id $job.Id -ErrorAction SilentlyContinue
                Remove-Job -Id $job.Id -Force -ErrorAction SilentlyContinue
            } catch {}
        }
    }

    Write-Info "Stopping infrastructure containers..."
    $savedEAP = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    docker-compose -f $composeFile down 2>&1 | Out-Null
    $ErrorActionPreference = $savedEAP
    Write-Ok "All services stopped."
}

# Register Ctrl+C handler
$null = Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action { Stop-Everything } -ErrorAction SilentlyContinue
trap { Stop-Everything; break }

# ================================================================
#  STEP 1 � Validate prerequisites
# ================================================================
Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  AdImpact Os - Local Dev Startup" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Infrastructure: Docker containers" -ForegroundColor Gray
Write-Host "  APIs:           dotnet run (local)" -ForegroundColor Gray
Write-Host "  UIs:            dotnet run (after migration)" -ForegroundColor Gray

Write-Step "Checking prerequisites..."

# Docker
Write-Host "Checking Docker Desktop status..." -ForegroundColor Yellow
try {
    $oldEAP = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $null = docker info 2>&1
    $ErrorActionPreference = $oldEAP
    if ($LASTEXITCODE -ne 0) {
        throw "Docker not running"
    }
    Write-Host "Docker Desktop is running" -ForegroundColor Green
} catch {
    $ErrorActionPreference = $oldEAP
    Write-Host "ERROR: Docker Desktop is not running!" -ForegroundColor Red
    Write-Host "Please start Docker Desktop and try again." -ForegroundColor Red
    exit 1
}

# .NET SDK
try {
    $dotnetVersion = dotnet --version 2>&1
    Write-Ok ".NET SDK $dotnetVersion"
} catch {
    Write-Err ".NET SDK not found. Please install the .NET 8 SDK."
    exit 1
}

# ================================================================
#  STEP 2 � Start infrastructure (Docker)
# ================================================================
Write-Step "Starting infrastructure containers..."
Write-Info "Using $composeFile"

# Stop any previous run
$oldEAP = $ErrorActionPreference
$ErrorActionPreference = "Continue"
docker-compose -f $composeFile down 2>&1 | Out-Null
$ErrorActionPreference = $oldEAP

$ErrorActionPreference = "Continue"
docker-compose -f $composeFile up -d
$ErrorActionPreference = "Stop"
if ($LASTEXITCODE -ne 0) {
    Write-Err "Failed to start infrastructure. Check Docker Desktop."
    exit 1
}

Write-Ok "Containers created. Waiting for Cosmos DB to become healthy..."
Write-Info "Cosmos DB emulator cold-start typically takes 90-150 seconds."

# Wait for Cosmos DB health check
$cosmosReady = $false
$cosmosAttempts = 0
$cosmosMaxAttempts = 60  # 60 * 5s = 5 minutes max

while (-not $cosmosReady -and $cosmosAttempts -lt $cosmosMaxAttempts) {
    $cosmosAttempts++
    try {
        $health = (docker inspect --format='{{.State.Health.Status}}' adimpactos-cosmosdb 2>&1) | Out-String
        $health = $health.Trim()
        if ($health -eq "healthy") {
            $cosmosReady = $true
        } else {
            $elapsed = $cosmosAttempts * 5
            if ($cosmosAttempts % 6 -eq 0) {
                Write-Info "Still waiting... ($elapsed`s elapsed, status: $health)"
            }
            Start-Sleep -Seconds 5
        }
    } catch {
        Start-Sleep -Seconds 5
    }
}

if (-not $cosmosReady) {
    Write-Err "Cosmos DB did not become healthy within 5 minutes."
    Write-Info "Check: docker logs adimpactos-cosmosdb"
    Stop-Everything
    exit 1
}

Write-Ok "Cosmos DB emulator is healthy"

# Quick check on other infra
Write-Info "Checking Kafka..."
$kafkaReady = $false
for ($i = 0; $i -lt 12; $i++) {
    try {
        $kh = (docker inspect --format='{{.State.Health.Status}}' adimpactos-eventhub 2>&1) | Out-String
        $kh = $kh.Trim()
        if ($kh -eq "healthy") { $kafkaReady = $true; break }
    } catch {}
    Start-Sleep -Seconds 5
}
if ($kafkaReady) { Write-Ok "Kafka is healthy" } else { Write-Warn "Kafka not yet healthy (APIs will still start)" }

Write-Ok "Infrastructure is ready"
Write-Host ""
Write-Host "   Cosmos DB Explorer:  https://localhost:8081/_explorer/index.html" -ForegroundColor White
Write-Host "   Kafka:               localhost:29092" -ForegroundColor White
Write-Host "   Azurite Blob:        http://localhost:10000" -ForegroundColor White

# ================================================================
#  STEP 3 � Build solution
# ================================================================
Write-Step "Building solution..."

Push-Location $solutionRoot
$null = dotnet build --configuration Debug --verbosity quiet 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Err "Solution build failed. Fix compilation errors and try again."
    Pop-Location
    Stop-Everything
    exit 1
}
Pop-Location
Write-Ok "Solution built successfully"

# ================================================================
#  STEP 4 � Start API projects (background, staggered)
# ================================================================
Write-Step "Starting API services locally (staggered to avoid migration conflicts)..."

# Start APIs one at a time so migrations don't race on the Cosmos DB emulator.
# The first API creates the shared database; subsequent APIs are fast.
for ($apiIdx = 0; $apiIdx -lt $apiProjects.Count; $apiIdx++) {
    $api = $apiProjects[$apiIdx]
    $projectDir = Join-Path $solutionRoot $api.Path
    Write-Info "Starting $($api.Name) on port $($api.Port)..."

    $logFile = Join-Path $env:TEMP "adimpactos-$($api.Name).log"

    $job = Start-Job -ScriptBlock {
        param($dir, $log)
        Set-Location $dir
        dotnet run --no-build --launch-profile http 2>&1 | Tee-Object -FilePath $log
    } -ArgumentList $projectDir, $logFile

    $script:bgJobs += $job
    Write-Info "  PID job $($job.Id) -> log: $logFile"

    # Wait for this API to be healthy before starting the next one
    $url = "http://localhost:$($api.Port)$($api.HealthPath)"
    $ready = $false
    $attempts = 0
    $maxAttempts = 80  # 80 * 3s = 4 minutes (first API migration can be slow)

    Write-Info "  Waiting for $($api.Name) to complete migration ($url)..."

    while (-not $ready -and $attempts -lt $maxAttempts) {
        $attempts++
        try {
            $resp = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
            if ($resp.StatusCode -eq 200) {
                $ready = $true
            }
        } catch {
            if ($attempts % 10 -eq 0) {
                $elapsed = $attempts * 3
                Write-Info "  $($api.Name) still starting... ($elapsed`s)"
            }
            Start-Sleep -Seconds 3
        }
    }

    if ($ready) {
        Write-Ok "$($api.Name) is ready (port $($api.Port)) - migration complete"
    } else {
        Write-Err "$($api.Name) did not respond within 4 minutes."
        Write-Info "Check log: $logFile"
        Write-Err "Aborting � later APIs depend on the shared database."
        Stop-Everything
        exit 1
    }
}

Write-Host ""
Write-Ok "All APIs started and migrations complete!"

# ================================================================
#  STEP 5 � Start Azure Functions (background)
# ================================================================
Write-Step "Starting Azure Functions (Ad Tracker)"

$funcDir = Join-Path $solutionRoot "src\AdImpactOs"
$funcLogFile = Join-Path $env:TEMP "adimpactos-Functions.log"
$funcBinDir = Join-Path $funcDir "bin\Debug\net10.0"

# Check Azure Functions Core Tools
$oldEAP = $ErrorActionPreference
$ErrorActionPreference = "Continue"
$funcVer = (func --version 2>&1) | Out-String
$funcAvailable = $LASTEXITCODE -eq 0
$ErrorActionPreference = $oldEAP

if ($funcAvailable -and (Test-Path (Join-Path $funcBinDir "host.json"))) {
    Write-Info "Azure Functions Core Tools $($funcVer.Trim()) detected"

    $funcJob = Start-Job -ScriptBlock {
        param($dir, $log)
        Set-Location $dir
        func start 2>&1 | Tee-Object -FilePath $log
    } -ArgumentList $funcBinDir, $funcLogFile

    $script:bgJobs += $funcJob
    Write-Info "  PID job $($funcJob.Id) -> log: $funcLogFile"

    # Wait briefly for Functions host
    $funcReady = $false
    for ($i = 0; $i -lt 15; $i++) {
        try {
            $resp = Invoke-WebRequest -Uri "http://localhost:7071/api/pixel?cid=healthcheck&crid=hc&uid=hc" -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
            if ($resp.StatusCode -eq 200) { $funcReady = $true; break }
        } catch {}
        Start-Sleep -Seconds 3
    }
    if ($funcReady) {
        Write-Ok "Azure Functions host is ready (port 7071)"
    } else {
        Write-Warn "Azure Functions is starting up (port 7071) - may need a few more seconds"
    }
} else {
    if (-not $funcAvailable) {
        Write-Warn "Azure Functions Core Tools not found. Skipping Functions host."
        Write-Info "  Install: npm install -g azure-functions-core-tools@4"
    } else {
        Write-Warn "Functions build output not found at $funcBinDir. Skipping."
    }
}

# ================================================================
#  STEP 5b � Start Event Consumer (background)
# ================================================================
Write-Step "Starting Event Consumer..."

$ecDir = Join-Path $solutionRoot "src\AdImpactOs.EventConsumer"
$ecLogFile = Join-Path $env:TEMP "adimpactos-EventConsumer.log"

$ecJob = Start-Job -ScriptBlock {
    param($dir, $log)
    $env:DOTNET_ENVIRONMENT = "Development"
    Set-Location $dir
    dotnet run --no-build 2>&1 | Tee-Object -FilePath $log
} -ArgumentList $ecDir, $ecLogFile

$script:bgJobs += $ecJob
Write-Info "  PID job $($ecJob.Id) -> log: $ecLogFile"
Write-Warn "Event Consumer connects to Azure Event Hubs (AMQP). It will log errors if only local Kafka is available."
Write-Info "  This is expected for local dev - the consumer will work when pointed at a real Event Hub."

# ================================================================
#  STEP 6 � Start UI projects (background)
# ================================================================
Write-Step "Starting UI projects..."

foreach ($ui in $uiProjects) {
    $projectDir = Join-Path $solutionRoot $ui.Path
    Write-Info "Starting $($ui.Name) on port $($ui.Port)..."

    $logFile = Join-Path $env:TEMP "adimpactos-$($ui.Name).log"

    $job = Start-Job -ScriptBlock {
        param($dir, $log)
        Set-Location $dir
        dotnet run --no-build --launch-profile http 2>&1 | Tee-Object -FilePath $log
    } -ArgumentList $projectDir, $logFile

    $script:bgJobs += $job
    Write-Info "  PID job $($job.Id) -> log: $logFile"
}

# Wait briefly for UIs to come up
foreach ($ui in $uiProjects) {
    $url = "http://localhost:$($ui.Port)$($ui.HealthPath)"
    $ready = $false

    for ($i = 0; $i -lt 20; $i++) {
        try {
            $resp = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
            if ($resp.StatusCode -eq 200) { $ready = $true; break }
        } catch {}
        Start-Sleep -Seconds 3
    }

    if ($ready) {
        Write-Ok "$($ui.Name) is ready (port $($ui.Port))"
    } else {
        Write-Warn "$($ui.Name) is starting up (port $($ui.Port)) - may need a few more seconds"
    }
}

# ================================================================
#  STEP 7 � Summary
# ================================================================
Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host "  All Services Running!" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""
Write-Host "  --- Infrastructure (Docker) ---" -ForegroundColor DarkCyan
Write-Host "  Cosmos DB Explorer:   https://localhost:8081/_explorer/index.html" -ForegroundColor White
Write-Host "  Kafka (EventHub):     localhost:29092" -ForegroundColor White
Write-Host "  Azurite Storage:      localhost:10000 / :10001 / :10002" -ForegroundColor White
Write-Host ""
Write-Host "  --- APIs (Local) ---" -ForegroundColor DarkCyan
Write-Host "  Panelist API:         http://localhost:5001/swagger" -ForegroundColor White
Write-Host "  Survey API:           http://localhost:5002/swagger" -ForegroundColor White
Write-Host "  Campaign API:         http://localhost:5003/swagger" -ForegroundColor White
Write-Host ""
Write-Host "  --- Azure Functions (Local) ---" -ForegroundColor DarkCyan
Write-Host "  Pixel Tracker:        http://localhost:7071/api/pixel?cid=CAMP&crid=CRE&uid=USER" -ForegroundColor White
Write-Host "  S2S Tracker:          http://localhost:7071/api/s2s/track  (POST)" -ForegroundColor White
Write-Host "  Event Consumer:       Running (logs: $env:TEMP\adimpactos-EventConsumer.log)" -ForegroundColor White
Write-Host ""
Write-Host "  --- UIs (Local) ---" -ForegroundColor DarkCyan
Write-Host "  Dashboard:            http://localhost:5004" -ForegroundColor White
Write-Host "  Demo UI:              http://localhost:5010" -ForegroundColor White
Write-Host ""
Write-Host "  --- Logs ---" -ForegroundColor DarkCyan
Write-Host "  API/UI logs:          $env:TEMP\adimpactos-*.log" -ForegroundColor White
Write-Host "  Infra logs:           docker-compose -f docker-compose.infra.yml logs -f" -ForegroundColor White
Write-Host ""
Write-Host "  Press Ctrl+C to stop everything." -ForegroundColor Yellow
Write-Host ""

# Open Dashboard in browser
try {
    Start-Process "http://localhost:5004"
} catch {}

# ================================================================
#  Keep alive � stream API logs until Ctrl+C
# ================================================================
try {
    while ($true) {
        # Check if any jobs have failed
        foreach ($job in $script:bgJobs) {
            if ($job.State -eq "Failed") {
                Write-Warn "Job $($job.Id) ($($job.Name)) has stopped. Check logs."
            }
        }
        Start-Sleep -Seconds 10
    }
} finally {
    Stop-Everything
}
