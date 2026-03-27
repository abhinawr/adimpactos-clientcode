# Test Script to View CosmosDB Data

Write-Host "=== CosmosDB Data Viewer ===" -ForegroundColor Cyan
Write-Host ""

# Check if sample data exists
Write-Host "1. Fetching all panelists..." -ForegroundColor Yellow
try {
    $panelists = Invoke-RestMethod -Uri "http://localhost:5001/api/panelists" -Method Get -ErrorAction Stop
    
    if ($panelists.Count -eq 0) {
        Write-Host "   No panelists found in database" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "   Would you like to seed sample data? (Y/N)" -ForegroundColor Cyan
        $response = Read-Host
        
        if ($response -eq "Y" -or $response -eq "y") {
            Write-Host "   Seeding sample data..." -ForegroundColor Yellow
            $seedResult = Invoke-RestMethod -Uri "http://localhost:5001/api/migration/seed" -Method Post
            Write-Host "   ? $($seedResult.message)" -ForegroundColor Green
            
            # Fetch again
            $panelists = Invoke-RestMethod -Uri "http://localhost:5001/api/panelists" -Method Get
        }
    }
    
    Write-Host "   Found $($panelists.Count) panelist(s)" -ForegroundColor Green
    Write-Host ""
    
    # Display panelists
    foreach ($panelist in $panelists) {
        Write-Host "   ?? Panelist: $($panelist.firstName) $($panelist.lastName)" -ForegroundColor White
        Write-Host "      ID: $($panelist.id)" -ForegroundColor Gray
        Write-Host "      Email: $($panelist.email)" -ForegroundColor Gray
        Write-Host "      Age: $($panelist.age), Gender: $($panelist.gender)" -ForegroundColor Gray
        Write-Host "      Country: $($panelist.country)" -ForegroundColor Gray
        Write-Host "      Device: $($panelist.deviceType), Browser: $($panelist.browser)" -ForegroundColor Gray
        Write-Host "      Consent: $(if ($panelist.consentGdpr) { '? Given' } else { '? Not Given' })" -ForegroundColor Gray
        Write-Host "      Created: $($panelist.createdAt)" -ForegroundColor Gray
        Write-Host ""
    }
    
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 401) {
        Write-Host "   ? Authentication required" -ForegroundColor Yellow
        Write-Host "   The API requires authentication. Use Swagger UI instead:" -ForegroundColor Yellow
        Write-Host "   http://localhost:5001/swagger" -ForegroundColor Cyan
    } else {
        Write-Host "   ? Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Test creating a new panelist
Write-Host ""
Write-Host "2. Would you like to create a test panelist? (Y/N)" -ForegroundColor Cyan
$createNew = Read-Host

if ($createNew -eq "Y" -or $createNew -eq "y") {
    Write-Host "   Creating new panelist..." -ForegroundColor Yellow
    
    $newPanelist = @{
        email = "test.user$(Get-Random -Maximum 9999)@example.com"
        firstName = "Test"
        lastName = "User"
        age = 30
        gender = "M"
        country = "US"
        postalCode = "90210"
        deviceType = "Desktop"
        browser = "Chrome"
        consentGdpr = $true
        consentCcpa = $true
    } | ConvertTo-Json
    
    try {
        $result = Invoke-RestMethod -Uri "http://localhost:5001/api/panelists" `
            -Method Post `
            -Body $newPanelist `
            -ContentType "application/json" `
            -ErrorAction Stop
        
        Write-Host "   ? Created panelist: $($result.id)" -ForegroundColor Green
        Write-Host "   Email: $($result.email)" -ForegroundColor Gray
    } catch {
        Write-Host "   ? Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "3. Access Options:" -ForegroundColor Cyan
Write-Host "   • Swagger UI: http://localhost:5001/swagger" -ForegroundColor White
Write-Host "   • CosmosDB Explorer: https://localhost:8081/_explorer/index.html" -ForegroundColor White
Write-Host "   • Direct API: http://localhost:5001/api/panelists" -ForegroundColor White
Write-Host ""
