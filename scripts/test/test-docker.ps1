# Ad Tracking JiangXi - Docker Test Script (PowerShell)
# This script tests the containerized environment

param(
    [switch]$FullStack
)

$ErrorActionPreference = "Continue"

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Ad Tracking JiangXi - Docker Tests" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Function to check if a service is running
function Test-Service {
    param([string]$ServiceName)
    
    $containers = docker ps --format "{{.Names}}"
    if ($containers -match $ServiceName) {
        Write-Host "? $ServiceName is running" -ForegroundColor Green
        return $true
    } else {
        Write-Host "? $ServiceName is NOT running" -ForegroundColor Red
        return $false
    }
}

# Function to check if a URL is accessible
function Test-Url {
    param(
        [string]$Url,
        [string]$Description,
        [int]$ExpectedStatus = 200
    )
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method GET -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        if ($response.StatusCode -eq $ExpectedStatus -or $response.StatusCode -eq 200) {
            Write-Host "? $Description ($Url)" -ForegroundColor Green
            return $true
        }
    } catch {
        Write-Host "? $Description ($Url) - Not responding" -ForegroundColor Red
        return $false
    }
    return $false
}

# Check if Docker is running
Write-Host "Checking Docker..." -ForegroundColor Yellow
try {
    docker info 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker not running"
    }
    Write-Host "? Docker is running" -ForegroundColor Green
} catch {
    Write-Host "? Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}
Write-Host ""

# Check running containers
Write-Host "Checking Services..." -ForegroundColor Yellow
Test-Service "adtracking-cosmosdb"
Test-Service "adtracking-zookeeper"
Test-Service "adtracking-eventhub"
Test-Service "adtracking-azurite"
Test-Service "adtracking-panelist-api"
Write-Host ""

# Test endpoints
Write-Host "Testing Endpoints..." -ForegroundColor Yellow
Start-Sleep -Seconds 2  # Give services time to be ready

# Test Panelist API
if (Test-Url "http://localhost:5001/health" "Panelist API Health") {
    Write-Host "  Testing API endpoints..." -ForegroundColor Gray
    
    # Test GET all panelists
    try {
        $panelists = Invoke-RestMethod -Uri "http://localhost:5001/api/panelists" -Method GET -ErrorAction Stop
        Write-Host "  ? GET /api/panelists works" -ForegroundColor Green
    } catch {
        Write-Host "  ? GET /api/panelists failed" -ForegroundColor Red
    }
    
    # Test Swagger
    Test-Url "http://localhost:5001/swagger" "Swagger UI" | Out-Null
}
Write-Host ""

# Test Cosmos DB Emulator
Write-Host "Testing Cosmos DB..." -ForegroundColor Yellow
try {
    # Skip certificate validation for Cosmos DB emulator
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
    $cosmosResponse = Invoke-WebRequest -Uri "https://localhost:8081/_explorer/emulator.pem" -UseBasicParsing -ErrorAction Stop
    Write-Host "? Cosmos DB Emulator is accessible" -ForegroundColor Green
} catch {
    Write-Host "? Cosmos DB Emulator is not accessible" -ForegroundColor Red
    Write-Host "  Note: Cosmos DB can take 2-3 minutes to start" -ForegroundColor Gray
}
Write-Host ""

# Test Azurite
Write-Host "Testing Azurite (Storage Emulator)..." -ForegroundColor Yellow
try {
    $azuriteResponse = Invoke-WebRequest -Uri "http://localhost:10000/devstoreaccount1?comp=list" -UseBasicParsing -ErrorAction Stop
    Write-Host "? Azurite Blob Service is accessible" -ForegroundColor Green
} catch {
    Write-Host "? Azurite is not accessible" -ForegroundColor Red
}
Write-Host ""

# Check if full stack is running
Write-Host "Checking Full Stack Services (if running)..." -ForegroundColor Yellow
$isFullStack = docker ps --format "{{.Names}}" | Select-String "adtracking-functions"
if ($isFullStack) {
    Test-Service "adtracking-functions"
    Test-Service "adtracking-incentives-api"
    Test-Service "adtracking-event-consumer"
    Test-Service "adtracking-spark-master"
    
    # Test additional endpoints
    Write-Host ""
    Write-Host "Testing Full Stack Endpoints..." -ForegroundColor Yellow
    Test-Url "http://localhost:7071/admin/host/status" "Azure Functions"
    Test-Url "http://localhost:5002/health" "Incentives API"
    Test-Url "http://localhost:8080" "Spark Master UI"
} else {
    Write-Host "? Full stack not running (development stack detected)" -ForegroundColor Yellow
}
Write-Host ""

# Test API with sample data
Write-Host "Testing API with Sample Data..." -ForegroundColor Yellow
Write-Host "Creating test panelist..." -ForegroundColor Gray

$testPanelist = @{
    email = "dockertest@example.com"
    firstName = "Docker"
    lastName = "Test"
    age = 30
    gender = "M"
    consentGdpr = $true
    consentCcpa = $true
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5001/api/panelists" `
        -Method POST `
        -Body $testPanelist `
        -ContentType "application/json" `
        -ErrorAction Stop
    
    Write-Host "? Successfully created test panelist" -ForegroundColor Green
    Write-Host "  Panelist ID: $($response.id)" -ForegroundColor Gray
    
    # Test GET by ID
    if ($response.id) {
        Write-Host "  Retrieving panelist by ID..." -ForegroundColor Gray
        $retrievedPanelist = Invoke-RestMethod -Uri "http://localhost:5001/api/panelists/$($response.id)" -Method GET -ErrorAction Stop
        Write-Host "  ? Successfully retrieved panelist" -ForegroundColor Green
    }
} catch {
    Write-Host "? Failed to create test panelist" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Gray
}
Write-Host ""

# Docker stats
Write-Host "Docker Resource Usage..." -ForegroundColor Yellow
docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}" | Select-Object -First 10
Write-Host ""

# Summary
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Service Endpoints:" -ForegroundColor Yellow
Write-Host "  Panelist API:       http://localhost:5001"
Write-Host "  Swagger UI:         http://localhost:5001/swagger"
Write-Host "  Cosmos DB Explorer: https://localhost:8081/_explorer/index.html"
Write-Host "  Azurite (Blob):     http://localhost:10000"
Write-Host ""

if ($isFullStack) {
    Write-Host "Full Stack Endpoints:" -ForegroundColor Yellow
    Write-Host "  Azure Functions:    http://localhost:7071"
    Write-Host "  Incentives API:     http://localhost:5002"
    Write-Host "  Spark Master UI:    http://localhost:8080"
    Write-Host ""
}

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  - Open Swagger UI to explore API: http://localhost:5001/swagger"
Write-Host "  - View logs: docker-compose logs -f"
Write-Host "  - Stop services: docker-compose down"
Write-Host ""

Write-Host "? Tests completed!" -ForegroundColor Green
