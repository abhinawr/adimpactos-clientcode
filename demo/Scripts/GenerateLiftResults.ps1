# Generate Lift Analysis Results
# Pre-compute brand lift metrics with statistical significance

$ErrorActionPreference = "Stop"

Write-Host "=== Lift Analysis Results Generator ===" -ForegroundColor Cyan
Write-Host ""

# Load data
$surveysFile = "demo/SampleData/surveys.json"
$responsesFile = "demo/SampleData/survey-responses.json"
$outputFile = "demo/SampleData/lift-results.json"

Write-Host "Loading surveys from $surveysFile..." -ForegroundColor Yellow
$surveys = Get-Content $surveysFile | ConvertFrom-Json

Write-Host "Loading responses from $responsesFile..." -ForegroundColor Yellow
$responses = Get-Content $responsesFile | ConvertFrom-Json

Write-Host ""

function Calculate-TTest {
    param($exposedValues, $controlValues)
    
    $nExposed = $exposedValues.Count
    $nControl = $controlValues.Count
    
    if ($nExposed -lt 2 -or $nControl -lt 2) {
        return @{
            pValue = 1.0
            tStatistic = 0
            significant = $false
        }
    }
    
    $meanExposed = ($exposedValues | Measure-Object -Average).Average
    $meanControl = ($controlValues | Measure-Object -Average).Average
    
    $varExposed = ($exposedValues | ForEach-Object { [math]::Pow($_ - $meanExposed, 2) } | Measure-Object -Sum).Sum / ($nExposed - 1)
    $varControl = ($controlValues | ForEach-Object { [math]::Pow($_ - $meanControl, 2) } | Measure-Object -Sum).Sum / ($nControl - 1)
    
    $pooledStdDev = [math]::Sqrt((($nExposed - 1) * $varExposed + ($nControl - 1) * $varControl) / ($nExposed + $nControl - 2))
    $standardError = $pooledStdDev * [math]::Sqrt(1.0 / $nExposed + 1.0 / $nControl)
    
    if ($standardError -eq 0) {
        $tStat = 0
    } else {
        $tStat = ($meanExposed - $meanControl) / $standardError
    }
    
    $df = $nExposed + $nControl - 2
    
    # Simplified p-value estimation (actual t-distribution would be more accurate)
    $pValue = if ([math]::Abs($tStat) -gt 2.576) { 0.001 }
              elseif ([math]::Abs($tStat) -gt 1.96) { 0.01 }
              elseif ([math]::Abs($tStat) -gt 1.645) { 0.05 }
              else { 0.10 }
    
    return @{
        pValue = $pValue
        tStatistic = [math]::Round($tStat, 3)
        significant = $pValue -lt 0.05
        degreesOfFreedom = $df
    }
}

function Calculate-ConfidenceInterval {
    param($mean, $stdDev, $n, $confidence = 0.95)
    
    if ($n -lt 2) {
        return @{
            lower = $mean
            upper = $mean
        }
    }
    
    $zScore = if ($confidence -eq 0.95) { 1.96 } else { 2.576 }
    $marginOfError = $zScore * ($stdDev / [math]::Sqrt($n))
    
    return @{
        lower = [math]::Round($mean - $marginOfError, 2)
        upper = [math]::Round($mean + $marginOfError, 2)
    }
}

Write-Host "Calculating lift metrics..." -ForegroundColor Yellow
Write-Host ""

$liftResults = @()

foreach ($survey in $surveys) {
    Write-Host "Processing survey: $($survey.surveyName)" -ForegroundColor Cyan
    
    $surveyResponses = $responses | Where-Object { $_.surveyId -eq $survey.surveyId }
    
    if ($surveyResponses.Count -eq 0) {
        Write-Host "  No responses found, skipping..." -ForegroundColor Yellow
        continue
    }
    
    $exposedResponses = $surveyResponses | Where-Object { $_.cohortType -eq "exposed" }
    $controlResponses = $surveyResponses | Where-Object { $_.cohortType -eq "control" }
    
    Write-Host "  Exposed: $($exposedResponses.Count), Control: $($controlResponses.Count)" -ForegroundColor Gray
    
    foreach ($question in $survey.questions) {
        $metric = $question.metric
        
        # Extract numeric values for this question
        $exposedValues = $exposedResponses | ForEach-Object {
            $answer = $_.answers | Where-Object { $_.questionId -eq $question.questionId }
            if ($answer -and $answer.numericValue -ne $null) {
                [double]$answer.numericValue
            }
        } | Where-Object { $_ -ne $null }
        
        $controlValues = $controlResponses | ForEach-Object {
            $answer = $_.answers | Where-Object { $_.questionId -eq $question.questionId }
            if ($answer -and $answer.numericValue -ne $null) {
                [double]$answer.numericValue
            }
        } | Where-Object { $_ -ne $null }
        
        if ($exposedValues.Count -eq 0 -or $controlValues.Count -eq 0) {
            continue
        }
        
        # Calculate statistics
        $exposedStats = $exposedValues | Measure-Object -Average -Sum
        $controlStats = $controlValues | Measure-Object -Average -Sum
        
        $exposedMean = [math]::Round($exposedStats.Average, 2)
        $controlMean = [math]::Round($controlStats.Average, 2)
        
        $liftPercent = if ($controlMean -gt 0) {
            [math]::Round((($exposedMean - $controlMean) / $controlMean) * 100, 2)
        } else {
            0
        }
        
        # Standard deviation
        $exposedStdDev = [math]::Round([math]::Sqrt(($exposedValues | ForEach-Object { [math]::Pow($_ - $exposedMean, 2) } | Measure-Object -Sum).Sum / $exposedValues.Count), 2)
        $controlStdDev = [math]::Round([math]::Sqrt(($controlValues | ForEach-Object { [math]::Pow($_ - $controlMean, 2) } | Measure-Object -Sum).Sum / $controlValues.Count), 2)
        
        # Statistical test
        $tTest = Calculate-TTest -exposedValues $exposedValues -controlValues $controlValues
        
        # Confidence intervals
        $exposedCI = Calculate-ConfidenceInterval -mean $exposedMean -stdDev $exposedStdDev -n $exposedValues.Count
        $controlCI = Calculate-ConfidenceInterval -mean $controlMean -stdDev $controlStdDev -n $controlValues.Count
        
        # Determine significance level
        $significanceLevel = if ($tTest.pValue -lt 0.001) { "***" }
                            elseif ($tTest.pValue -lt 0.01) { "**" }
                            elseif ($tTest.pValue -lt 0.05) { "*" }
                            else { "ns" }
        
        $result = [PSCustomObject]@{
            resultId = "result_$(Get-Random -Minimum 100000 -Maximum 999999)"
            surveyId = $survey.surveyId
            campaignId = $survey.campaignId
            questionId = $question.questionId
            metric = $metric
            metricDisplayName = $metric -replace "_", " " | ForEach-Object { (Get-Culture).TextInfo.ToTitleCase($_) }
            exposedMean = $exposedMean
            exposedStdDev = $exposedStdDev
            exposedCI95Lower = $exposedCI.lower
            exposedCI95Upper = $exposedCI.upper
            exposedN = $exposedValues.Count
            controlMean = $controlMean
            controlStdDev = $controlStdDev
            controlCI95Lower = $controlCI.lower
            controlCI95Upper = $controlCI.upper
            controlN = $controlValues.Count
            absoluteLift = [math]::Round($exposedMean - $controlMean, 2)
            liftPercent = $liftPercent
            pValue = $tTest.pValue
            tStatistic = $tTest.tStatistic
            degreesOfFreedom = $tTest.degreesOfFreedom
            isSignificant = $tTest.significant
            significanceLevel = $significanceLevel
            calculatedAt = (Get-Date).ToString("o")
            questionText = $question.questionText
            questionType = $question.questionType
        }
        
        $liftResults += $result
        
        Write-Host "    $($metric): $liftPercent% lift (p=$($tTest.pValue)) $significanceLevel" -ForegroundColor $(if ($tTest.significant) { "Green" } else { "Gray" })
    }
    
    Write-Host ""
}

Write-Host "Converting to JSON..." -ForegroundColor Yellow
$json = $liftResults | ConvertTo-Json -Depth 10

Write-Host "Writing to file: $outputFile" -ForegroundColor Yellow
$json | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host ""
Write-Host "? Successfully generated lift analysis results!" -ForegroundColor Green
Write-Host ""
Write-Host "Statistics:" -ForegroundColor Cyan
Write-Host "  - Total lift calculations: $(($liftResults.Count).ToString('N0'))"
Write-Host "  - Significant results (p<0.05): $(($liftResults | Where-Object { $_.isSignificant }).Count)"
Write-Host "  - Surveys analyzed: $($surveys.Count)"
Write-Host "  - Average lift: $([math]::Round(($liftResults | Measure-Object -Property liftPercent -Average).Average, 1))%"
Write-Host ""

# Summary by metric
Write-Host "Lift by Metric:" -ForegroundColor Cyan
$liftResults | Group-Object -Property metric | ForEach-Object {
    $avgLift = [math]::Round(($_.Group | Measure-Object -Property liftPercent -Average).Average, 1)
    $significant = ($_.Group | Where-Object { $_.isSignificant }).Count
    Write-Host "  $($_.Name): $avgLift% (significant: $significant/$($_.Count))" -ForegroundColor Gray
}

Write-Host ""
Write-Host "File saved to: $outputFile" -ForegroundColor Green
