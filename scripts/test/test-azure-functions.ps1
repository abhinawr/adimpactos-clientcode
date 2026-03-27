# Azure Functions Test Script
# Tests both Pixel Tracker and S2S Tracker endpoints

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Azure Functions Test Suite" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$functionsUrl = "http://localhost:7071"
$testsPassed = 0
$testsFailed = 0

# Test 1: Pixel Tracker - Valid Request
Write-Host "[TEST 1] Pixel Tracker - Valid Request" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$functionsUrl/api/pixel?cid=test_campaign&crid=test_creative&uid=test_user" `
        -Method GET `
        -Headers @{
            "User-Agent" = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
            "Referer" = "https://example.com/page"
        } `
        -UseBasicParsing

    if ($response.StatusCode -eq 200 -and $response.Headers["Content-Type"] -like "*image/gif*") {
        Write-Host "  ? PASS: Returned 200 OK with GIF content type" -ForegroundColor Green
        Write-Host "  ? Response size: $($response.Content.Length) bytes (expected 43)" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  ? FAIL: Unexpected response" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "  ? FAIL: $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}
Write-Host ""

# Test 2: Pixel Tracker - Missing Parameters
Write-Host "[TEST 2] Pixel Tracker - Missing Parameters" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$functionsUrl/api/pixel?cid=test" `
        -Method GET `
        -UseBasicParsing

    if ($response.StatusCode -eq 200 -and $response.Headers["Content-Type"] -like "*image/gif*") {
        Write-Host "  ? PASS: Still returns GIF even with validation errors" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  ? FAIL: Should still return GIF" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "  ? FAIL: $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}
Write-Host ""

# Test 3: S2S Tracker - Valid Request
Write-Host "[TEST 3] S2S Tracker - Valid Request" -ForegroundColor Yellow
try {
    $body = @{
        campaign_id = "summer2024"
        creative_id = "banner300x250"
        panelist_token = "user123"
        ad_server = "doubleclick"
        user_agent = "TestClient/1.0"
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "$functionsUrl/api/s2s/track" `
        -Method POST `
        -ContentType "application/json" `
        -Body $body

    if ($response.success -eq $true -and $response.event_id) {
        Write-Host "  ? PASS: Successfully created tracking event" -ForegroundColor Green
        Write-Host "  ? Event ID: $($response.event_id)" -ForegroundColor Green
        Write-Host "  ? Tracking Hash: $($response.tracking_hash)" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  ? FAIL: Invalid response structure" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "  ? FAIL: $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}
Write-Host ""

# Test 4: S2S Tracker - Missing Required Fields
Write-Host "[TEST 4] S2S Tracker - Missing Required Fields" -ForegroundColor Yellow
try {
    $body = @{
        campaign_id = "test"
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "$functionsUrl/api/s2s/track" `
        -Method POST `
        -ContentType "application/json" `
        -Body $body `
        -ErrorAction Stop

    Write-Host "  ? FAIL: Should have returned 400 Bad Request" -ForegroundColor Red
    $testsFailed++
} catch {
    if ($_.Exception.Response.StatusCode -eq 400) {
        Write-Host "  ? PASS: Correctly returned 400 Bad Request for invalid payload" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  ? FAIL: Unexpected error: $($_.Exception.Message)" -ForegroundColor Red
        $testsFailed++
    }
}
Write-Host ""

# Test 5: S2S Tracker - Idempotency
Write-Host "[TEST 5] S2S Tracker - Idempotency" -ForegroundColor Yellow
try {
    $idempotencyKey = [guid]::NewGuid().ToString()
    $body = @{
        campaign_id = "test"
        creative_id = "test"
        panelist_token = "test"
        idempotency_key = $idempotencyKey
    } | ConvertTo-Json

    # First request
    $response1 = Invoke-RestMethod -Uri "$functionsUrl/api/s2s/track" `
        -Method POST `
        -ContentType "application/json" `
        -Body $body

    # Second request with same idempotency key
    $response2 = Invoke-RestMethod -Uri "$functionsUrl/api/s2s/track" `
        -Method POST `
        -ContentType "application/json" `
        -Body $body

    if ($response1.success -and $response2.success -and $response2.processed_before -eq $true) {
        Write-Host "  ? PASS: Idempotency working correctly" -ForegroundColor Green
        Write-Host "  ? First request created event: $($response1.event_id)" -ForegroundColor Green
        Write-Host "  ? Second request detected as duplicate" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  ? FAIL: Idempotency not working" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "  ? FAIL: $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}
Write-Host ""

# Test 6: S2S Tracker - Invalid JSON
Write-Host "[TEST 6] S2S Tracker - Invalid JSON" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$functionsUrl/api/s2s/track" `
        -Method POST `
        -ContentType "application/json" `
        -Body "invalid json{" `
        -ErrorAction Stop

    Write-Host "  ? FAIL: Should have returned 400 Bad Request" -ForegroundColor Red
    $testsFailed++
} catch {
    if ($_.Exception.Response.StatusCode -eq 400) {
        Write-Host "  ? PASS: Correctly rejected invalid JSON" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  ? FAIL: Unexpected error: $($_.Exception.Message)" -ForegroundColor Red
        $testsFailed++
    }
}
Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Tests Passed: $testsPassed" -ForegroundColor Green
Write-Host "Tests Failed: $testsFailed" -ForegroundColor Red
Write-Host ""

if ($testsFailed -eq 0) {
    Write-Host "? All tests passed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "? Some tests failed. Please check the output above." -ForegroundColor Red
    exit 1
}
