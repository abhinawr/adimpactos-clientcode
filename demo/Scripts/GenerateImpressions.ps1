# Generate Historical Impression Data
# Creates 30 days of realistic impression data for demo campaigns

$ErrorActionPreference = "Stop"

Write-Host "=== Historical Impression Data Generator ===" -ForegroundColor Cyan
Write-Host ""

# Load campaigns
$campaignsFile = "demo/SampleData/campaigns.json"
$panelistsFile = "demo/SampleData/panelists.json"
$outputFile = "demo/SampleData/impressions.json"

Write-Host "Loading campaigns from $campaignsFile..." -ForegroundColor Yellow
$campaigns = Get-Content $campaignsFile | ConvertFrom-Json

Write-Host "Loading panelists from $panelistsFile..." -ForegroundColor Yellow
$panelists = Get-Content $panelistsFile | ConvertFrom-Json

Write-Host ""

# Configuration
$daysOfHistory = 30
$totalImpressions = 500000  # Total impressions to generate
$impressionsPerDay = [math]::Floor($totalImpressions / $daysOfHistory)

# Time distribution (hourly weight for realistic patterns)
$hourlyWeights = @(
    2,  # 00:00
    1,  # 01:00
    1,  # 02:00
    1,  # 03:00
    1,  # 04:00
    2,  # 05:00
    4,  # 06:00
    6,  # 07:00
    8,  # 08:00
    9,  # 09:00
    10, # 10:00
    10, # 11:00
    9,  # 12:00
    8,  # 13:00
    9,  # 14:00
    10, # 15:00
    10, # 16:00
    9,  # 17:00
    8,  # 18:00
    7,  # 19:00
    6,  # 20:00
    5,  # 21:00
    4,  # 22:00
    3   # 23:00
)
$totalWeight = ($hourlyWeights | Measure-Object -Sum).Sum

function Get-WeightedHour {
    $random = Get-Random -Minimum 0 -Maximum $totalWeight
    $cumulative = 0
    for ($hour = 0; $hour -lt 24; $hour++) {
        $cumulative += $hourlyWeights[$hour]
        if ($random -lt $cumulative) {
            return $hour
        }
    }
    return 12
}

function Get-RandomUserAgent {
    $userAgents = @(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Safari/605.1.15",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (iPad; CPU OS 17_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0"
    )
    return $userAgents[(Get-Random -Minimum 0 -Maximum $userAgents.Length)]
}

function Get-RandomReferrer {
    $referrers = @(
        "https://www.google.com/",
        "https://www.facebook.com/",
        "https://www.twitter.com/",
        "https://www.youtube.com/",
        "https://www.reddit.com/",
        "https://www.instagram.com/",
        "https://news.google.com/",
        "https://www.bing.com/",
        "https://www.linkedin.com/",
        $null  # Direct traffic
    )
    return $referrers[(Get-Random -Minimum 0 -Maximum $referrers.Length)]
}

function Get-RandomIP {
    # Generate random IP addresses
    return "$(Get-Random -Minimum 1 -Maximum 255).$(Get-Random -Minimum 0 -Maximum 255).$(Get-Random -Minimum 0 -Maximum 255).$(Get-Random -Minimum 1 -Maximum 255)"
}

Write-Host "Generating impressions for $daysOfHistory days..." -ForegroundColor Yellow
Write-Host "Target: ~$impressionsPerDay impressions per day" -ForegroundColor Gray
Write-Host ""

$impressions = @()
$impressionCount = 0

# Filter active campaigns only
$activeCampaigns = $campaigns | Where-Object { $_.status -eq "Active" }
Write-Host "Active campaigns: $($activeCampaigns.Count)" -ForegroundColor Gray

# Generate impressions for each day
for ($day = 0; $day -lt $daysOfHistory; $day++) {
    $date = (Get-Date).AddDays(-$daysOfHistory + $day)
    $dayImpressions = $impressionsPerDay + (Get-Random -Minimum -500 -Maximum 500)  # Add variance
    
    for ($i = 0; $i -lt $dayImpressions; $i++) {
        # Select random campaign
        $campaign = $activeCampaigns[(Get-Random -Minimum 0 -Maximum $activeCampaigns.Count)]
        
        # Select random creative
        $creative = $campaign.creatives[(Get-Random -Minimum 0 -Maximum $campaign.creatives.Count)]
        
        # Select random panelist (80% chance) or anonymous (20% chance)
        $panelist = if ((Get-Random -Minimum 0 -Maximum 100) -lt 80) {
            $panelists[(Get-Random -Minimum 0 -Maximum $panelists.Count)]
        } else {
            $null
        }
        
        # Generate timestamp with realistic hourly distribution
        $hour = Get-WeightedHour
        $minute = Get-Random -Minimum 0 -Maximum 60
        $second = Get-Random -Minimum 0 -Maximum 60
        $timestamp = Get-Date -Year $date.Year -Month $date.Month -Day $date.Day -Hour $hour -Minute $minute -Second $second
        
        # 5% bot traffic
        $isBot = (Get-Random -Minimum 0 -Maximum 100) -lt 5
        
        $impressionId = "impression_$($impressionCount.ToString("D9"))"
        
        $impression = [PSCustomObject]@{
            impressionId = $impressionId
            timestamp = $timestamp.ToString("o")
            campaignId = $campaign.campaignId
            creativeId = $creative.creativeId
            panelistId = if ($panelist) { $panelist.panelistId } else { "anonymous_$(Get-Random)" }
            userToken = if ($panelist) { "encrypted_token_$($panelist.panelistId)" } else { $null }
            userAgent = Get-RandomUserAgent
            ipAddress = Get-RandomIP
            referrer = Get-RandomReferrer
            country = if ($panelist) { $panelist.country } else { "US" }
            deviceType = if ($panelist) { $panelist.deviceType } else { @("Desktop", "Mobile", "Tablet")[(Get-Random -Minimum 0 -Maximum 3)] }
            browser = if ($panelist) { $panelist.browser } else { @("Chrome", "Safari", "Firefox", "Edge")[(Get-Random -Minimum 0 -Maximum 4)] }
            isBot = $isBot
            source = @("pixel", "s2s")[(Get-Random -Minimum 0 -Maximum 2)]
            sessionId = "session_$(Get-Random -Minimum 100000 -Maximum 999999)"
            pageUrl = "https://example.com/page$(Get-Random -Minimum 1 -Maximum 100)"
        }
        
        $impressions += $impression
        $impressionCount++
    }
    
    if (($day + 1) % 5 -eq 0) {
        Write-Host "  Generated day $($day + 1)/$daysOfHistory ($(($impressionCount).ToString('N0')) impressions)..." -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Converting to JSON..." -ForegroundColor Yellow
$json = $impressions | ConvertTo-Json -Depth 10

Write-Host "Writing to file: $outputFile" -ForegroundColor Yellow
$json | Out-File -FilePath $outputFile -Encoding UTF8

$fileSize = (Get-Item $outputFile).Length / 1MB

Write-Host ""
Write-Host "? Successfully generated impression data!" -ForegroundColor Green
Write-Host ""
Write-Host "Statistics:" -ForegroundColor Cyan
Write-Host "  - Total impressions: $(($impressions.Count).ToString('N0'))"
Write-Host "  - Days of history: $daysOfHistory"
Write-Host "  - Avg per day: $([math]::Round($impressions.Count / $daysOfHistory, 0))"
Write-Host "  - Bot traffic: $(($impressions | Where-Object { $_.isBot }).Count) ($([math]::Round((($impressions | Where-Object { $_.isBot }).Count / $impressions.Count) * 100, 1))%)"
Write-Host "  - With panelist ID: $(($impressions | Where-Object { $_.panelistId -notlike 'anonymous_*' }).Count)"
Write-Host "  - Pixel source: $(($impressions | Where-Object { $_.source -eq 'pixel' }).Count)"
Write-Host "  - S2S source: $(($impressions | Where-Object { $_.source -eq 's2s' }).Count)"
Write-Host "  - File size: $([math]::Round($fileSize, 2)) MB"
Write-Host ""
Write-Host "File saved to: $outputFile" -ForegroundColor Green
