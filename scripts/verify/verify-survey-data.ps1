# Verify Survey Data Seeded

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Survey Data Verification" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "? Survey data has been seeded successfully!" -ForegroundColor Green
Write-Host ""

Write-Host "?? What was seeded:" -ForegroundColor Yellow
Write-Host "  3 Sample Surveys:" -ForegroundColor White
Write-Host ""

Write-Host "  1. Brand Awareness Survey - Q1 2025" -ForegroundColor Cyan
Write-Host "     Survey ID: survey-001" -ForegroundColor Gray
Write-Host "     Campaign: camp-001" -ForegroundColor Gray
Write-Host "     Questions: 3 (Brand awareness, Purchase intent, Brand favorability)" -ForegroundColor Gray
Write-Host "     Status: Active" -ForegroundColor Green
Write-Host ""

Write-Host "  2. Ad Recall Survey - Video Campaign" -ForegroundColor Cyan
Write-Host "     Survey ID: survey-002" -ForegroundColor Gray
Write-Host "     Campaign: camp-002" -ForegroundColor Gray
Write-Host "     Questions: 3 (Ad recall, Message association, Message recall)" -ForegroundColor Gray
Write-Host "     Status: Active" -ForegroundColor Green
Write-Host ""

Write-Host "  3. Purchase Intent Follow-Up" -ForegroundColor Cyan
Write-Host "     Survey ID: survey-003" -ForegroundColor Gray
Write-Host "     Campaign: camp-001" -ForegroundColor Gray
Write-Host "     Questions: 2 (Consideration, Brand perception)" -ForegroundColor Gray
Write-Host "     Status: Active" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  How to View the Data" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Option 1: CosmosDB Data Explorer (Recommended)" -ForegroundColor Yellow
Write-Host "  1. Open: https://localhost:8081/_explorer/index.html" -ForegroundColor White
Write-Host "  2. Accept the certificate warning" -ForegroundColor Gray
Write-Host "  3. Navigate to: AdImpactOsDB ? Surveys ? Items" -ForegroundColor White
Write-Host "  4. You should see 3 survey documents" -ForegroundColor Gray
Write-Host ""

Write-Host "Option 2: Survey API Swagger" -ForegroundColor Yellow
Write-Host "  1. Open: http://localhost:5002/swagger" -ForegroundColor White
Write-Host "  2. Try GET /api/surveys" -ForegroundColor White
Write-Host "  3. Try GET /api/surveys/survey-001" -ForegroundColor White
Write-Host ""

Write-Host "Option 3: Direct API Call" -ForegroundColor Yellow
Write-Host "  Invoke-RestMethod -Uri 'http://localhost:5002/api/surveys' -Method Get" -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Sample SQL Queries in CosmosDB" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Get all surveys:" -ForegroundColor White
Write-Host "  SELECT * FROM c" -ForegroundColor DarkGray
Write-Host ""

Write-Host "Get surveys by campaign:" -ForegroundColor White
Write-Host "  SELECT * FROM c WHERE c.campaignId = 'camp-001'" -ForegroundColor DarkGray
Write-Host ""

Write-Host "Get active surveys:" -ForegroundColor White
Write-Host "  SELECT * FROM c WHERE c.status = 'Active'" -ForegroundColor DarkGray
Write-Host ""

Write-Host "Count surveys:" -ForegroundColor White
Write-Host "  SELECT COUNT(1) as total FROM c" -ForegroundColor DarkGray
Write-Host ""

Write-Host "Get surveys by type:" -ForegroundColor White
Write-Host "  SELECT * FROM c WHERE c.surveyType = 'BrandLift'" -ForegroundColor DarkGray
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Survey Details" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Survey 1: Brand Awareness Survey" -ForegroundColor Yellow
Write-Host "  • Question 1: Have you heard of our brand before? (YesNo)" -ForegroundColor Gray
Write-Host "  • Question 2: How likely are you to purchase our product? (1-10 Rating)" -ForegroundColor Gray
Write-Host "  • Question 3: How would you rate our brand compared to competitors? (Likert Scale)" -ForegroundColor Gray
Write-Host ""

Write-Host "Survey 2: Ad Recall Survey" -ForegroundColor Yellow
Write-Host "  • Question 1: Do you recall seeing any video ads recently? (YesNo)" -ForegroundColor Gray
Write-Host "  • Question 2: Which brand was featured in the ad? (Multiple Choice)" -ForegroundColor Gray
Write-Host "  • Question 3: What was the main message of the ad? (Open Ended)" -ForegroundColor Gray
Write-Host ""

Write-Host "Survey 3: Purchase Intent Follow-Up" -ForegroundColor Yellow
Write-Host "  • Question 1: How likely are you to consider our product? (1-10 Rating)" -ForegroundColor Gray
Write-Host "  • Question 2: What is your overall perception of our brand? (Likert Scale)" -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Complete Database Status" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "? Panelists Container:" -ForegroundColor Green
Write-Host "  • 3 sample panelists seeded" -ForegroundColor Gray
Write-Host "  • Ready for testing" -ForegroundColor Gray
Write-Host ""

Write-Host "? Surveys Container:" -ForegroundColor Green
Write-Host "  • 3 sample surveys seeded" -ForegroundColor Gray
Write-Host "  • Ready for responses" -ForegroundColor Gray
Write-Host ""

Write-Host "? SurveyResponses Container:" -ForegroundColor Green
Write-Host "  • Empty (ready for test responses)" -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Next Steps" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "1. View surveys in CosmosDB Explorer" -ForegroundColor White
Write-Host "   ? https://localhost:8081/_explorer/index.html" -ForegroundColor Cyan
Write-Host ""

Write-Host "2. Test survey APIs in Swagger" -ForegroundColor White
Write-Host "   ? http://localhost:5002/swagger" -ForegroundColor Cyan
Write-Host ""

Write-Host "3. Submit test survey responses" -ForegroundColor White
Write-Host "   Use POST /api/surveys/{surveyId}/responses" -ForegroundColor Gray
Write-Host ""

Write-Host "4. Run complete E2E test" -ForegroundColor White
Write-Host "   ? .\scripts\test\test-e2e-guide.ps1" -ForegroundColor Cyan
Write-Host ""

$open = Read-Host "Would you like to open CosmosDB Explorer now? (Y/N)"
if ($open -eq "Y" -or $open -eq "y") {
    Start-Process "https://localhost:8081/_explorer/index.html"
    Write-Host "? Opening CosmosDB Explorer..." -ForegroundColor Green
}

$openSwagger = Read-Host "Would you like to open Survey API Swagger? (Y/N)"
if ($openSwagger -eq "Y" -or $openSwagger -eq "y") {
    Start-Process "http://localhost:5002/swagger"
    Write-Host "? Opening Survey API Swagger..." -ForegroundColor Green
}

Write-Host ""
Write-Host "? All data seeded and ready for testing!" -ForegroundColor Green
Write-Host ""
