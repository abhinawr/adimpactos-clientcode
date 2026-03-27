# Generate Demo Panelist Profiles
# Creates 1000 realistic panelist records with diverse demographics

$ErrorActionPreference = "Stop"

Write-Host "=== Demo Panelist Profile Generator ===" -ForegroundColor Cyan
Write-Host ""

# Configuration
$outputFile = "demo/SampleData/panelists.json"
$panelistCount = 1000

# Data arrays for realistic generation
$firstNamesMale = @("James", "John", "Robert", "Michael", "William", "David", "Richard", "Joseph", "Thomas", "Christopher", "Daniel", "Matthew", "Anthony", "Mark", "Donald", "Steven", "Andrew", "Kenneth", "Joshua", "Kevin")
$firstNamesFemale = @("Mary", "Patricia", "Jennifer", "Linda", "Barbara", "Elizabeth", "Susan", "Jessica", "Sarah", "Karen", "Nancy", "Lisa", "Betty", "Margaret", "Sandra", "Ashley", "Kimberly", "Emily", "Donna", "Michelle")
$lastNames = @("Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Thompson", "White", "Harris", "Sanchez", "Clark", "Lewis", "Robinson", "Walker", "Young")

$countries = @(
    @{Code="US"; Weight=45},
    @{Code="CA"; Weight=20},
    @{Code="UK"; Weight=15},
    @{Code="AU"; Weight=10},
    @{Code="DE"; Weight=5},
    @{Code="FR"; Weight=5}
)

$postalCodesByCountry = @{
    "US" = @("10001", "90210", "60601", "02108", "30301", "33101", "94102", "98101", "75201", "19101")
    "CA" = @("M5H 2N2", "V6B 1A1", "T2P 1J9", "K1A 0A9", "H2Y 1C6")
    "UK" = @("SW1A 1AA", "E1 6AN", "M1 1AE", "B1 1AA", "EH1 1YZ")
    "AU" = @("2000", "3000", "4000", "6000", "5000")
    "DE" = @("10115", "80331", "20095", "60311", "50667")
    "FR" = @("75001", "13001", "69001", "31000", "33000")
}

$ageRanges = @("18-24", "25-34", "35-44", "45-54", "55-64", "65+")
$genders = @("M", "F")
$deviceTypes = @("Desktop", "Mobile", "Tablet")
$browsers = @("Chrome", "Safari", "Firefox", "Edge", "Opera")
$hhIncomeBuckets = @("< 25k", "25k-50k", "50k-75k", "75k-100k", "100k-150k", "> 150k")
$interestsList = @("sports", "fitness", "technology", "travel", "food", "music", "movies", "gaming", "fashion", "health", "outdoor", "shopping", "finance", "automotive", "home")

function Get-WeightedCountry {
    $random = Get-Random -Minimum 0 -Maximum 100
    $cumulative = 0
    foreach ($country in $countries) {
        $cumulative += $country.Weight
        if ($random -lt $cumulative) {
            return $country.Code
        }
    }
    return "US"
}

function Get-RandomEmail {
    param($firstName, $lastName)
    $domains = @("gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "icloud.com")
    $domain = $domains[(Get-Random -Minimum 0 -Maximum $domains.Length)]
    $emailPrefix = "$($firstName.ToLower()).$($lastName.ToLower())$(Get-Random -Minimum 1 -Maximum 999)"
    return "$emailPrefix@$domain"
}

function Get-RandomInterests {
    $count = Get-Random -Minimum 2 -Maximum 6
    $selected = $interestsList | Get-Random -Count $count
    return ($selected -join ",")
}

Write-Host "Generating $panelistCount panelist profiles..." -ForegroundColor Yellow

$panelists = @()

for ($i = 0; $i -lt $panelistCount; $i++) {
    # Random demographics
    $gender = $genders[(Get-Random -Minimum 0 -Maximum $genders.Length)]
    $firstName = if ($gender -eq "M") { 
        $firstNamesMale[(Get-Random -Minimum 0 -Maximum $firstNamesMale.Length)] 
    } else { 
        $firstNamesFemale[(Get-Random -Minimum 0 -Maximum $firstNamesFemale.Length)] 
    }
    $lastName = $lastNames[(Get-Random -Minimum 0 -Maximum $lastNames.Length)]
    
    $country = Get-WeightedCountry
    $postalCode = $postalCodesByCountry[$country][(Get-Random -Minimum 0 -Maximum $postalCodesByCountry[$country].Length)]
    
    $ageRange = $ageRanges[(Get-Random -Minimum 0 -Maximum $ageRanges.Length)]
    $age = switch ($ageRange) {
        "18-24" { Get-Random -Minimum 18 -Maximum 25 }
        "25-34" { Get-Random -Minimum 25 -Maximum 35 }
        "35-44" { Get-Random -Minimum 35 -Maximum 45 }
        "45-54" { Get-Random -Minimum 45 -Maximum 55 }
        "55-64" { Get-Random -Minimum 55 -Maximum 65 }
        "65+" { Get-Random -Minimum 65 -Maximum 80 }
    }
    
    # 80% of panelists have given consent
    $hasConsent = (Get-Random -Minimum 0 -Maximum 100) -lt 80
    
    # 70% assigned to exposed, 30% to control (for those with consent)
    $cohortType = if ($hasConsent) {
        if ((Get-Random -Minimum 0 -Maximum 100) -lt 70) { "exposed" } else { "control" }
    } else {
        $null
    }
    
    $panelistId = "panelist_$($i.ToString("D5"))"
    
    $panelist = [PSCustomObject]@{
        id = $panelistId
        panelistId = $panelistId
        email = Get-RandomEmail -firstName $firstName -lastName $lastName
        phone = "+1$(Get-Random -Minimum 2000000000 -Maximum 9999999999)"
        firstName = $firstName
        lastName = $lastName
        age = $age
        ageRange = $ageRange
        gender = $gender
        hhIncomeBucket = $hhIncomeBuckets[(Get-Random -Minimum 0 -Maximum $hhIncomeBuckets.Length)]
        interests = Get-RandomInterests
        country = $country
        postalCode = $postalCode
        deviceType = $deviceTypes[(Get-Random -Minimum 0 -Maximum $deviceTypes.Length)]
        browser = $browsers[(Get-Random -Minimum 0 -Maximum $browsers.Length)]
        consentGdpr = $hasConsent
        consentCcpa = $hasConsent
        consentGiven = $hasConsent
        consentTimestamp = if ($hasConsent) { (Get-Date).AddDays(-(Get-Random -Minimum 1 -Maximum 365)).ToString("o") } else { $null }
        cohortType = $cohortType
        isActive = $true
        lastActive = (Get-Date).AddDays(-(Get-Random -Minimum 0 -Maximum 30)).ToString("o")
        createdAt = (Get-Date).AddDays(-(Get-Random -Minimum 30 -Maximum 365)).ToString("o")
        updatedAt = (Get-Date).AddDays(-(Get-Random -Minimum 0 -Maximum 30)).ToString("o")
    }
    
    $panelists += $panelist
    
    if (($i + 1) % 100 -eq 0) {
        Write-Host "  Generated $($i + 1) panelists..." -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Converting to JSON..." -ForegroundColor Yellow
$json = $panelists | ConvertTo-Json -Depth 10

Write-Host "Writing to file: $outputFile" -ForegroundColor Yellow
$json | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host ""
Write-Host "? Successfully generated $panelistCount panelist profiles!" -ForegroundColor Green
Write-Host ""
Write-Host "Statistics:" -ForegroundColor Cyan
Write-Host "  - Total panelists: $panelistCount"
Write-Host "  - With consent: $(($panelists | Where-Object { $_.consentGiven }).Count)"
Write-Host "  - Exposed cohort: $(($panelists | Where-Object { $_.cohortType -eq 'exposed' }).Count)"
Write-Host "  - Control cohort: $(($panelists | Where-Object { $_.cohortType -eq 'control' }).Count)"
Write-Host "  - Male: $(($panelists | Where-Object { $_.gender -eq 'M' }).Count)"
Write-Host "  - Female: $(($panelists | Where-Object { $_.gender -eq 'F' }).Count)"
Write-Host ""
Write-Host "File saved to: $outputFile" -ForegroundColor Green
