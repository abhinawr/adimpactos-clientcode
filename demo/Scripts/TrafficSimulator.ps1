# Real-Time Traffic Simulator for Client Demos
# Generates live impression traffic to demonstrate pixel tracking

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Real-Time Traffic Simulator" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$pixelTrackerUrl = "http://localhost:7071/api/pixel"
$s2sTrackerUrl = "http://localhost:7071/api/s2s/track"
$impressionsPerMinute = 60
$durationMinutes = 10
$pixelRatio = 0.7  # 70% pixel, 30% S2S

# Load demo data
$campaignsFile = "demo/SampleData/campaigns.json"
$panelistsFile = "demo/SampleData/panelists.json"

Write-Host "Loading campaigns and panelists..." -ForegroundColor Yellow
$campaigns = Get-Content $campaignsFile | ConvertFrom-Json
$panelists = Get-Content $panelistsFile | ConvertFrom-Json
$activeCampaigns = $campaigns | Where-Object { $_.status -eq "Active" }

Write-Host "  Active campaigns: $($activeCampaigns.Count)" -ForegroundColor Gray
Write-Host "  Available panelists: $($panelists.Count)" -ForegroundColor Gray
Write-Host ""

Write-Host "Simulation Configuration:" -ForegroundColor Cyan
Write-Host "  - Rate: $impressionsPerMinute impressions/minute" -ForegroundColor White
Write-Host "  - Duration: $durationMinutes minutes" -ForegroundColor White
Write-Host "  - Total impressions: $($impressionsPerMinute * $durationMinutes)" -ForegroundColor White
Write-Host "  - Pixel/S2S split: $([math]::Round($pixelRatio * 100))%/$([math]::Round((1-$pixelRatio) * 100))%" -ForegroundColor White
Write-Host ""

$userAgents = @(
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
    "Mozilla/5.0 (iPhone; CPU iPhone OS 17_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Mobile/15E148 Safari/604.1",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0"
)

function Send-PixelImpression {
    param($campaign, $creative, $panelist)
    
    try {
        $url = "$pixelTrackerUrl?cid=$($campaign.campaignId)&crid=$($creative.creativeId)&uid=encrypted_$($panelist.panelistId)"
        
        $headers = @{
            "User-Agent" = $userAgents[(Get-Random -Minimum 0 -Maximum $userAgents.Length)]
            "Referer" = "https://example.com/page$(Get-Random -Minimum 1 -Maximum 100)"
        }
        
        Invoke-WebRequest -Uri $url -Method GET -Headers $headers -TimeoutSec 2 -ErrorAction SilentlyContinue | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

function Send-S2SImpression {
    param($campaign, $creative, $panelist)
    
    try {
        $body = @{
            campaign_id = $campaign.campaignId
            creative_id = $creative.creativeId
            panelist_token = "encrypted_$($panelist.panelistId)"
            ad_server = "demo_simulator"
            timestamp = (Get-Date).ToUniversalTime().ToString("o")
        } | ConvertTo-Json
        
        $headers = @{
            "Content-Type" = "application/json"
            "User-Agent" = "DemoSimulator/1.0"
        }
        
        Invoke-RestMethod -Uri $s2sTrackerUrl -Method POST -Body $body -Headers $headers -TimeoutSec 2 -ErrorAction SilentlyContinue | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

Write-Host "Starting traffic simulation..." -ForegroundColor Green
Write-Host "Press Ctrl+C to stop" -ForegroundColor Gray
Write-Host ""

$startTime = Get-Date
$totalSent = 0
$totalErrors = 0
$pixelCount = 0
$s2sCount = 0

try {
    for ($minute = 1; $minute -le $durationMinutes; $minute++) {
        $minuteStart = Get-Date
        $sentThisMinute = 0
        
        for ($i = 0; $i -lt $impressionsPerMinute; $i++) {
            # Select random campaign, creative, and panelist
            $campaign = $activeCampaigns[(Get-Random -Minimum 0 -Maximum $activeCampaigns.Count)]
            $creative = $campaign.creatives[(Get-Random -Minimum 0 -Maximum $campaign.creatives.Count)]
            $panelist = $panelists[(Get-Random -Minimum 0 -Maximum $panelists.Count)]
            
            # Decide pixel vs S2S
            $usePixel = (Get-Random -Minimum 0.0 -Maximum 1.0) -lt $pixelRatio
            
            $success = if ($usePixel) {
                $pixelCount++
                Send-PixelImpression -campaign $campaign -creative $creative -panelist $panelist
            } else {
                $s2sCount++
                Send-S2SImpression -campaign $campaign -creative $creative -panelist $panelist
            }
            
            if ($success) {
                $totalSent++
                $sentThisMinute++
            } else {
                $totalErrors++
            }
            
            # Sleep to maintain rate (spread evenly across minute)
            $sleepMs = [math]::Max(1, 60000 / $impressionsPerMinute)
            Start-Sleep -Milliseconds $sleepMs
        }
        
        $minuteElapsed = ((Get-Date) - $minuteStart).TotalSeconds
        Write-Host "Minute $minute/$durationMinutes : Sent $sentThisMinute impressions in $([math]::Round($minuteElapsed, 1))s (Errors: $totalErrors)" -ForegroundColor White
    }
}
catch {
    Write-Host ""
    Write-Host "Simulation interrupted" -ForegroundColor Yellow
}

$totalElapsed = ((Get-Date) - $startTime).TotalSeconds

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Simulation Complete" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  - Total impressions sent: $totalSent" -ForegroundColor White
Write-Host "  - Pixel impressions: $pixelCount" -ForegroundColor White
Write-Host "  - S2S impressions: $s2sCount" -ForegroundColor White
Write-Host "  - Errors: $totalErrors" -ForegroundColor White
Write-Host "  - Duration: $([math]::Round($totalElapsed, 1)) seconds" -ForegroundColor White
Write-Host "  - Average rate: $([math]::Round($totalSent / ($totalElapsed / 60), 1)) impressions/minute" -ForegroundColor White
Write-Host ""
Write-Host "Check your dashboards for updated metrics!" -ForegroundColor Yellow
Write-Host ""
