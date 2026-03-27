# Campaign API Verification Script

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Campaign API Verification" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

try {
    # Test 1: Get all campaigns
    Write-Host "1. Fetching all campaigns..." -ForegroundColor Yellow
    $campaigns = Invoke-RestMethod -Uri "http://localhost:5003/api/campaigns" -Method Get
    Write-Host "   ? Found $($campaigns.Count) campaigns" -ForegroundColor Green
    Write-Host ""

    # Display campaigns
    Write-Host "Campaigns in database:" -ForegroundColor Cyan
    $campaigns | Select-Object campaignId, campaignName, advertiser, industry, status, @{Name='Budget';Expression={"`$$($_.budget/1000)K"}} | Format-Table -AutoSize
    Write-Host ""

    # Test 2: Get active campaigns
    Write-Host "2. Fetching active campaigns..." -ForegroundColor Yellow
    $active = Invoke-RestMethod -Uri "http://localhost:5003/api/campaigns/active" -Method Get
    Write-Host "   ? Found $($active.Count) active campaigns" -ForegroundColor Green
    $active | ForEach-Object { Write-Host "     • $($_.campaignName)" -ForegroundColor Gray }
    Write-Host ""

    # Test 3: Get specific campaign
    Write-Host "3. Testing campaign details..." -ForegroundColor Yellow
    $testId = "campaign_summer_beverage_2024"
    $campaign = Invoke-RestMethod -Uri "http://localhost:5003/api/campaigns/$testId" -Method Get
    Write-Host "   ? Retrieved: $($campaign.campaignName)" -ForegroundColor Green
    Write-Host "     Advertiser: $($campaign.advertiser)" -ForegroundColor Gray
    Write-Host "     Industry: $($campaign.industry)" -ForegroundColor Gray
    Write-Host "     Budget: `$$($campaign.budget)" -ForegroundColor Gray
    Write-Host "     Status: $($campaign.status)" -ForegroundColor Gray
    Write-Host "     Creatives: $($campaign.creatives.Count)" -ForegroundColor Gray
    Write-Host ""

    # Test 4: Filter by industry
    Write-Host "4. Testing filters..." -ForegroundColor Yellow
    $cpg = Invoke-RestMethod -Uri "http://localhost:5003/api/campaigns?industry=CPG" -Method Get
    Write-Host "   ? CPG campaigns: $($cpg.Count)" -ForegroundColor Green
    
    $retail = Invoke-RestMethod -Uri "http://localhost:5003/api/campaigns?industry=Retail" -Method Get
    Write-Host "   ? Retail campaigns: $($retail.Count)" -ForegroundColor Green
    Write-Host ""

    # Test 5: Campaign-Survey integration
    Write-Host "5. Testing Campaign-Survey integration..." -ForegroundColor Yellow
    $surveyCount = 0
    foreach ($camp in $campaigns) {
        try {
            $surveys = Invoke-RestMethod -Uri "http://localhost:5002/api/surveys/campaign/$($camp.campaignId)" -Method Get -ErrorAction SilentlyContinue
            if ($surveys.Count -gt 0) {
                $surveyCount += $surveys.Count
                Write-Host "   ? Campaign '$($camp.campaignName)' has $($surveys.Count) survey(s)" -ForegroundColor Green
            }
        } catch {
            # No surveys for this campaign
        }
    }
    Write-Host "   Total surveys linked to campaigns: $surveyCount" -ForegroundColor Cyan
    Write-Host ""

    # Summary
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "           Summary" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "? Campaign API: " -NoNewline; Write-Host "OPERATIONAL" -ForegroundColor Green
    Write-Host "? Total Campaigns: $($campaigns.Count)" -ForegroundColor White
    Write-Host "? Active Campaigns: $($active.Count)" -ForegroundColor White
    Write-Host "? Completed Campaigns: $(($campaigns | Where-Object { $_.status -eq 'Completed' }).Count)" -ForegroundColor White
    Write-Host "? Scheduled Campaigns: $(($campaigns | Where-Object { $_.status -eq 'Scheduled' }).Count)" -ForegroundColor White
    
    $totalBudget = ($campaigns | Measure-Object -Property budget -Sum).Sum
    Write-Host "? Total Budget: `$$($totalBudget/1000000)M" -ForegroundColor White
    Write-Host ""

    Write-Host "Industries represented:" -ForegroundColor Cyan
    $campaigns | Group-Object industry | ForEach-Object {
        Write-Host "  • $($_.Name): $($_.Count) campaign(s)" -ForegroundColor Gray
    }
    Write-Host ""

    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "     Service Endpoints" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Campaign API Swagger: " -NoNewline
    Write-Host "http://localhost:5003/swagger" -ForegroundColor Cyan
    Write-Host "Survey API Swagger:   " -NoNewline
    Write-Host "http://localhost:5002/swagger" -ForegroundColor Cyan
    Write-Host "Panelist API Swagger: " -NoNewline
    Write-Host "http://localhost:5001/swagger" -ForegroundColor Cyan
    Write-Host "CosmosDB Explorer:    " -NoNewline
    Write-Host "https://localhost:8081/_explorer/index.html" -ForegroundColor Cyan
    Write-Host ""

    $openSwagger = Read-Host "Would you like to open Campaign API Swagger? (Y/N)"
    if ($openSwagger -eq "Y" -or $openSwagger -eq "y") {
        Start-Process "http://localhost:5003/swagger"
        Write-Host "? Opening Campaign API Swagger..." -ForegroundColor Green
    }

} catch {
    Write-Host "? Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure Campaign API is running on port 5003" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "? Campaign API verification complete!" -ForegroundColor Green
Write-Host ""
