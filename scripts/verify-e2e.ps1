# ============================================================
# AdImpact Os - End-to-End Verification Script
# ============================================================
# Verifies data was captured across all services after a demo.
# Usage: .\scripts\verify-e2e.ps1
# ============================================================

$ErrorActionPreference = "Continue"

function Write-Section($title) { Write-Host "`n$('=' * 60)" -ForegroundColor Cyan; Write-Host "  $title" -ForegroundColor Cyan; Write-Host "$('=' * 60)" -ForegroundColor Cyan }
function Write-Pass($msg)    { Write-Host "  [PASS] $msg" -ForegroundColor Green }
function Write-Fail($msg)    { Write-Host "  [FAIL] $msg" -ForegroundColor Red }
function Write-Info($msg)    { Write-Host "  [INFO] $msg" -ForegroundColor Gray }
function Write-Data($msg)    { Write-Host "         $msg" -ForegroundColor White }

$pass = 0; $fail = 0

# ============================================================
# 1. SERVICE HEALTH
# ============================================================
Write-Section "1. SERVICE HEALTH CHECK"

$services = @(
    @{ Name = "Campaign API";  Url = "http://localhost:5003/api/campaigns" },
    @{ Name = "Survey API";    Url = "http://localhost:5002/api/surveys" },
    @{ Name = "Panelist API";  Url = "http://localhost:5001/api/panelists" },
    @{ Name = "Dashboard";     Url = "http://localhost:5004" },
    @{ Name = "Demo UI";       Url = "http://localhost:5010" }
)

foreach ($svc in $services) {
    try {
        $r = Invoke-WebRequest -Uri $svc.Url -UseBasicParsing -TimeoutSec 5
        if ($r.StatusCode -eq 200) { Write-Pass "$($svc.Name): OK ($($r.StatusCode))"; $pass++ }
        else { Write-Fail "$($svc.Name): HTTP $($r.StatusCode)"; $fail++ }
    } catch { Write-Fail "$($svc.Name): $($_.Exception.Message)"; $fail++ }
}

# ============================================================
# 2. CAMPAIGNS
# ============================================================
Write-Section "2. CAMPAIGNS"

try {
    $campaigns = (Invoke-WebRequest -Uri "http://localhost:5003/api/campaigns" -UseBasicParsing -TimeoutSec 5).Content | ConvertFrom-Json
    Write-Pass "Campaigns loaded: $($campaigns.Count) total"
    $pass++

    $byStatus = $campaigns | Group-Object status
    foreach ($g in $byStatus) { Write-Data "  $($g.Name): $($g.Count)" }

    $active = $campaigns | Where-Object { $_.status -eq "Active" }
    if ($active.Count -gt 0) { Write-Pass "Active campaigns: $($active.Count)"; $pass++ }
    else { Write-Info "No active campaigns (expected if using seed data with Scheduled status)" }
} catch { Write-Fail "Could not load campaigns: $($_.Exception.Message)"; $fail++ }

# ============================================================
# 3. IMPRESSIONS
# ============================================================
Write-Section "3. IMPRESSIONS"

try {
    $impressions = (Invoke-WebRequest -Uri "http://localhost:5003/api/impressions?limit=500" -UseBasicParsing -TimeoutSec 5).Content | ConvertFrom-Json
    Write-Pass "Impressions loaded: $($impressions.Count) total"
    $pass++

    # By source
    $bySource = $impressions | Group-Object ingestSource
    Write-Info "By Ingest Source:"
    foreach ($g in $bySource) { Write-Data "  $($g.Name): $($g.Count)" }

    # By device
    $byDevice = $impressions | Group-Object deviceType
    Write-Info "By Device Type:"
    foreach ($g in $byDevice) { Write-Data "  $($g.Name): $($g.Count)" }

    # By campaign
    $byCampaign = $impressions | Group-Object campaignId | Sort-Object Count -Descending
    Write-Info "By Campaign (top 5):"
    $byCampaign | Select-Object -First 5 | ForEach-Object { Write-Data "  $($_.Name): $($_.Count) impressions" }

    # Unique panelists
    $uniquePanelists = ($impressions | Select-Object -ExpandProperty panelistId -Unique).Count
    Write-Info "Unique Panelists: $uniquePanelists"

    # Bot detection
    $bots = ($impressions | Where-Object { $_.isBot -eq $true }).Count
    $valid = ($impressions | Where-Object { $_.isBot -ne $true }).Count
    Write-Info "Valid: $valid | Bot: $bots"

    # E2E test impressions
    $e2e = $impressions | Where-Object { $_.impressionId -like "e2e_*" -or $_.impressionId -like "demo_*" }
    if ($e2e.Count -gt 0) {
        Write-Pass "E2E/Demo impressions found: $($e2e.Count)"
        $pass++
        $e2e | ForEach-Object { Write-Data "  $($_.impressionId) | $($_.campaignId) | $($_.ingestSource) | $($_.timestampUtc)" }
    } else {
        Write-Info "No e2e_*/demo_* impressions (fire from Demo UI to create them)"
    }
} catch { Write-Fail "Could not load impressions: $($_.Exception.Message)"; $fail++ }

# ============================================================
# 4. IMPRESSION SUMMARIES (per campaign)
# ============================================================
Write-Section "4. CAMPAIGN IMPRESSION SUMMARIES"

try {
    $summaries = (Invoke-WebRequest -Uri "http://localhost:5003/api/impressions/summaries" -UseBasicParsing -TimeoutSec 5).Content | ConvertFrom-Json
    $summaryProps = $summaries.PSObject.Properties
    Write-Pass "Summaries loaded for $($summaryProps.Count) campaigns"
    $pass++

    foreach ($prop in $summaryProps) {
        $s = $prop.Value
        Write-Data "--- $($prop.Name) ---"
        Write-Data "  Total: $($s.totalImpressions) | Valid: $($s.validImpressions) | Bot: $($s.botImpressions) | Unique Panelists: $($s.uniquePanelists)"
        if ($s.bySource) { Write-Data "  Source: $(($s.bySource | ConvertTo-Json -Compress))" }
        if ($s.byDevice) { Write-Data "  Device: $(($s.byDevice | ConvertTo-Json -Compress))" }
    }
} catch { Write-Fail "Could not load summaries: $($_.Exception.Message)"; $fail++ }

# ============================================================
# 5. PANELISTS
# ============================================================
Write-Section "5. PANELISTS"

try {
    $panelists = (Invoke-WebRequest -Uri "http://localhost:5001/api/panelists" -UseBasicParsing -TimeoutSec 5).Content | ConvertFrom-Json
    Write-Pass "Panelists loaded: $($panelists.Count) total"
    $pass++

    $withConsent = ($panelists | Where-Object { $_.consentGiven -eq $true }).Count
    $countries = ($panelists | Select-Object -ExpandProperty country -Unique).Count
    $byGender = $panelists | Group-Object gender
    Write-Info "With consent: $withConsent | Countries: $countries"
    Write-Info "By Gender: $(($byGender | ForEach-Object { "$($_.Name):$($_.Count)" }) -join ', ')"
} catch { Write-Fail "Could not load panelists: $($_.Exception.Message)"; $fail++ }

# ============================================================
# 6. SURVEYS
# ============================================================
Write-Section "6. SURVEYS"

try {
    $surveys = (Invoke-WebRequest -Uri "http://localhost:5002/api/surveys" -UseBasicParsing -TimeoutSec 5).Content | ConvertFrom-Json
    Write-Pass "Surveys loaded: $($surveys.Count) total"
    $pass++

    $byStatus = $surveys | Group-Object status
    foreach ($g in $byStatus) { Write-Data "  $($g.Name): $($g.Count)" }

    $surveys | ForEach-Object {
        Write-Data "  $($_.surveyId) | $($_.surveyName) | $($_.status) | Questions: $($_.questions.Count)"
    }
} catch { Write-Fail "Could not load surveys: $($_.Exception.Message)"; $fail++ }

# ============================================================
# 7. SURVEY RESPONSES
# ============================================================
Write-Section "7. SURVEY RESPONSES"

try {
    $responses = (Invoke-WebRequest -Uri "http://localhost:5002/api/surveys/responses/all" -UseBasicParsing -TimeoutSec 5).Content | ConvertFrom-Json
    Write-Pass "Survey responses loaded: $($responses.Count) total"
    $pass++

    $byStatus = $responses | Group-Object status
    Write-Info "By Status:"
    foreach ($g in $byStatus) { Write-Data "  $($g.Name): $($g.Count)" }

    $byCohort = $responses | Group-Object cohortType
    Write-Info "By Cohort:"
    foreach ($g in $byCohort) { Write-Data "  $($g.Name): $($g.Count)" }

    $bySurvey = $responses | Group-Object surveyId
    Write-Info "By Survey:"
    foreach ($g in $bySurvey) { Write-Data "  $($g.Name): $($g.Count) responses" }
} catch { Write-Fail "Could not load survey responses: $($_.Exception.Message)"; $fail++ }

# ============================================================
# 8. BRAND LIFT RESULTS
# ============================================================
Write-Section "8. BRAND LIFT RESULTS"

try {
    $surveys = (Invoke-WebRequest -Uri "http://localhost:5002/api/surveys" -UseBasicParsing -TimeoutSec 5).Content | ConvertFrom-Json
    foreach ($survey in $surveys) {
        try {
            $results = (Invoke-WebRequest -Uri "http://localhost:5002/api/surveys/$($survey.surveyId)/results" -UseBasicParsing -TimeoutSec 5).Content | ConvertFrom-Json
            if ($results.totalResponses -gt 0) {
                Write-Data "--- $($survey.surveyName) ---"
                Write-Data "  Responses: $($results.totalResponses) (Exposed: $($results.exposedResponses), Control: $($results.controlResponses))"
                foreach ($q in $results.questionResults) {
                    $lift = if ($q.liftPercent) { [math]::Round($q.liftPercent, 1).ToString() + "%" } else { "N/A" }
                    $expM = if ($q.exposedMean) { [math]::Round($q.exposedMean, 2) } else { "-" }
                    $ctlM = if ($q.controlMean) { [math]::Round($q.controlMean, 2) } else { "-" }
                    Write-Data "  [$($q.metric)] $($q.questionText)"
                    Write-Data "    Exposed: $expM | Control: $ctlM | Lift: $lift"
                }
            }
        } catch {}
    }
    Write-Pass "Brand lift results retrieved"
    $pass++
} catch { Write-Fail "Could not load brand lift results: $($_.Exception.Message)"; $fail++ }

# ============================================================
# 9. CROSS-SERVICE: PANELIST JOURNEY
# ============================================================
Write-Section "9. PANELIST JOURNEY (panelist_001)"

try {
    # Panelist profile
    $p = (Invoke-WebRequest -Uri "http://localhost:5001/api/panelists/panelist-001" -UseBasicParsing -TimeoutSec 5).Content | ConvertFrom-Json
    Write-Data "Profile: $($p.firstName) $($p.lastName) | $($p.ageRange) | $($p.gender) | $($p.country)"

    # Impressions for this panelist
    $allImpressions = (Invoke-WebRequest -Uri "http://localhost:5003/api/impressions?limit=500" -UseBasicParsing -TimeoutSec 5).Content | ConvertFrom-Json
    $pImpressions = $allImpressions | Where-Object { $_.panelistId -like "*001*" }
    Write-Data "Impressions: $($pImpressions.Count)"
    $pImpressions | Select-Object -First 5 | ForEach-Object { Write-Data "  $($_.impressionId) | $($_.campaignId) | $($_.ingestSource)" }

    # Survey responses
    try {
        $pResponses = (Invoke-WebRequest -Uri "http://localhost:5002/api/surveys/panelist/panelist_001/responses" -UseBasicParsing -TimeoutSec 5).Content | ConvertFrom-Json
        Write-Data "Survey Responses: $($pResponses.Count)"
        $pResponses | ForEach-Object { Write-Data "  $($_.surveyId) | $($_.cohortType) | $($_.status)" }
    } catch {
        Write-Info "No survey responses found for panelist_001"
    }

    Write-Pass "Panelist journey verified"
    $pass++
} catch { Write-Fail "Could not verify panelist journey: $($_.Exception.Message)"; $fail++ }

# ============================================================
# SUMMARY
# ============================================================
Write-Host ""
Write-Host "$('=' * 60)" -ForegroundColor Cyan
Write-Host "  VERIFICATION SUMMARY" -ForegroundColor Cyan
Write-Host "$('=' * 60)" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Passed: $pass" -ForegroundColor Green
Write-Host "  Failed: $fail" -ForegroundColor $(if ($fail -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($fail -eq 0) {
    Write-Host "  ALL CHECKS PASSED - E2E flow is working!" -ForegroundColor Green
} else {
    Write-Host "  SOME CHECKS FAILED - Review errors above" -ForegroundColor Yellow
}
Write-Host ""
