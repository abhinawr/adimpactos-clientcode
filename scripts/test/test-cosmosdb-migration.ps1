# CosmosDB Migration Test Script

Write-Host "=== CosmosDB Migration Test ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Check if CosmosDB container is healthy
Write-Host "1. Checking CosmosDB container health..." -ForegroundColor Yellow
$cosmosHealth = docker inspect --format='{{.State.Health.Status}}' adimpactos-cosmosdb 2>$null
if ($cosmosHealth -eq "healthy") {
    Write-Host "   ? CosmosDB is healthy" -ForegroundColor Green
} else {
    Write-Host "   ? CosmosDB is $cosmosHealth" -ForegroundColor Red
    exit 1
}

# Test 2: Check if Panelist API container is running
Write-Host "2. Checking Panelist API container..." -ForegroundColor Yellow
$apiStatus = docker inspect --format='{{.State.Status}}' adimpactos-panelist-api 2>$null
if ($apiStatus -eq "running") {
    Write-Host "   ? Panelist API is running" -ForegroundColor Green
} else {
    Write-Host "   ? Panelist API is $apiStatus" -ForegroundColor Red
    exit 1
}

# Test 3: Check network connectivity
Write-Host "3. Checking network connectivity..." -ForegroundColor Yellow
$cosmosIP = docker inspect --format='{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' adimpactos-cosmosdb
$apiIP = docker inspect --format='{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' adimpactos-panelist-api
Write-Host "   CosmosDB IP: $cosmosIP" -ForegroundColor Gray
Write-Host "   API IP: $apiIP" -ForegroundColor Gray

$cosmosNetwork = docker inspect --format='{{range .NetworkSettings.Networks}}{{.NetworkID}}{{end}}' adimpactos-cosmosdb
$apiNetwork = docker inspect --format='{{range .NetworkSettings.Networks}}{{.NetworkID}}{{end}}' adimpactos-panelist-api
if ($cosmosNetwork -eq $apiNetwork) {
    Write-Host "   ? Both containers on same network" -ForegroundColor Green
} else {
    Write-Host "   ? Containers on different networks" -ForegroundColor Red
}

# Test 4: Check environment variables
Write-Host "4. Checking CosmosDB configuration..." -ForegroundColor Yellow
$endpoint = docker exec adimpactos-panelist-api printenv CosmosDb__Endpoint 2>$null
$dbName = docker exec adimpactos-panelist-api printenv CosmosDb__DatabaseName 2>$null
$container = docker exec adimpactos-panelist-api printenv CosmosDb__ContainerName 2>$null

Write-Host "   Endpoint: $endpoint" -ForegroundColor Gray
Write-Host "   Database: $dbName" -ForegroundColor Gray
Write-Host "   Container: $container" -ForegroundColor Gray

if ($endpoint -match "cosmosdb") {
    Write-Host "   ? Endpoint configured correctly" -ForegroundColor Green
} else {
    Write-Host "   ? Endpoint not configured" -ForegroundColor Red
}

# Test 5: Test CosmosDB endpoint from host
Write-Host "5. Testing CosmosDB endpoint from host..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://localhost:8081/_explorer/emulator.pem" -SkipCertificateCheck -TimeoutSec 5 -ErrorAction Stop
    Write-Host "   ? CosmosDB accessible from host" -ForegroundColor Green
} catch {
    Write-Host "   ? Cannot access CosmosDB from host" -ForegroundColor Red
}

# Test 6: Run migration endpoint
Write-Host "6. Testing migration endpoint..." -ForegroundColor Yellow
Write-Host "   Calling POST /api/migration/run..." -ForegroundColor Gray

try {
    $result = Invoke-RestMethod -Uri "http://localhost:5001/api/migration/run" -Method Post -ErrorAction Stop
    Write-Host "   ? Migration succeeded" -ForegroundColor Green
    Write-Host "   Response: $($result.message)" -ForegroundColor Gray
    
    # Test 7: Verify database was created
    Write-Host "7. Verifying database creation..." -ForegroundColor Yellow
    Start-Sleep -Seconds 2
    
    # Try to get panelists (should return empty array if DB exists)
    try {
        $panelists = Invoke-RestMethod -Uri "http://localhost:5001/api/panelists" -Method Get -ErrorAction Stop
        Write-Host "   ? Database accessible" -ForegroundColor Green
        Write-Host "   Panelists count: $($panelists.Count)" -ForegroundColor Gray
    } catch {
        Write-Host "   ? Database not accessible" -ForegroundColor Red
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "   ? Migration failed" -ForegroundColor Red
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "   Status Code: $statusCode" -ForegroundColor Red
    
    # Get detailed error from logs
    Write-Host ""
    Write-Host "Recent logs from Panelist API:" -ForegroundColor Yellow
    docker logs adimpactos-panelist-api --tail 20 | Select-String -Pattern "error|Error|exception|Exception" | Select-Object -First 5 | ForEach-Object {
        Write-Host "   $_" -ForegroundColor Red
    }
}

# Test 8: Check for known issues
Write-Host ""
Write-Host "8. Checking for known issues..." -ForegroundColor Yellow

# Check if it's a DNS resolution issue
$logs = docker logs adimpactos-panelist-api --tail 50 2>&1 | Out-String
if ($logs -match "Connection refused.*127\.0\.0\.1:8081") {
    Write-Host "   ? DNS Resolution Issue Detected" -ForegroundColor Yellow
    Write-Host "   The container is resolving 'cosmosdb' to 127.0.0.1 instead of the container IP" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   Possible fixes:" -ForegroundColor Cyan
    Write-Host "   1. Restart the containers: docker-compose -f docker-compose.dev.yml restart" -ForegroundColor Gray
    Write-Host "   2. Recreate the network: docker-compose -f docker-compose.dev.yml down && docker-compose -f docker-compose.dev.yml up -d" -ForegroundColor Gray
    Write-Host "   3. Use IP address instead of hostname in docker-compose.yml" -ForegroundColor Gray
}

if ($logs -match "SSL|certificate|TLS") {
    Write-Host "   ? SSL Certificate Issue Detected" -ForegroundColor Yellow
    Write-Host "   The container cannot validate CosmosDB emulator certificate" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan
