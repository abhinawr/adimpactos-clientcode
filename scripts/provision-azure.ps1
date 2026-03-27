<#
.SYNOPSIS
    AdImpactOs - Azure Infrastructure Provisioning Script (PowerShell)
.DESCRIPTION
    Provisions all Azure resources (Phases 4-15 from the deployment guide).
    Idempotent - safe to re-run.
.PARAMETER Prefix
    Project prefix for resource naming (default: adimpact)
.PARAMETER Env
    Environment name: dev, staging (default: dev)
.PARAMETER Location
    Azure region (default: eastus)
.PARAMETER AlertEmail
    Email for budget and monitoring alerts
.EXAMPLE
    .\scripts\provision-azure.ps1
    .\scripts\provision-azure.ps1 -Prefix myapp -Env staging -Location westus2
#>

param(
    [string]$Prefix = "adimpact",
    [string]$Env = "dev",
    [string]$Location = "eastus",
    [string]$AlertEmail = "tech@theeditorialinstitute.com"
)

$ErrorActionPreference = "Stop"

# Derived names
$RG = "$Prefix-$Env-rg"
$ACR_NAME = "${Prefix}${Env}acr"
$COSMOS_NAME = "$Prefix-cosmos-$Env"
$EH_NAMESPACE = "$Prefix-eh-$Env"
$STORAGE_NAME = "${Prefix}${Env}sa"
$KV_NAME = "$Prefix-kv-$Env"
$LAW_NAME = "$Prefix-law-$Env"
$AI_NAME = "$Prefix-ai-$Env"
$APP_PLAN = "$Prefix-plan-$Env"
$FUNC_APP = "$Prefix-fn-$Env"

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "AdImpactOs - Azure Provisioning" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "Prefix:      $Prefix"
Write-Host "Environment: $Env"
Write-Host "Location:    $Location"
Write-Host "RG:          $RG"
Write-Host "===========================================" -ForegroundColor Cyan

# Phase 4: Resource Group
Write-Host "`n> Creating Resource Group: $RG" -ForegroundColor Yellow
az group create --name $RG --location $Location --tags project=AdImpactOs environment=$Env --output none

# Phase 5: Container Registry
Write-Host "> Creating Container Registry: $ACR_NAME" -ForegroundColor Yellow
az acr create --resource-group $RG --name $ACR_NAME --sku Basic --location $Location --output none
az acr update --name $ACR_NAME --admin-enabled true --output none
$ACR_LOGIN_SERVER = az acr show --name $ACR_NAME --query loginServer --output tsv
Write-Host "  ACR Login Server: $ACR_LOGIN_SERVER"

# Phase 6: Cosmos DB (Serverless)
Write-Host "> Creating Cosmos DB Account: $COSMOS_NAME (serverless)" -ForegroundColor Yellow
az cosmosdb create `
    --resource-group $RG `
    --name $COSMOS_NAME `
    --kind GlobalDocumentDB `
    --default-consistency-level Session `
    --locations regionName=$Location failoverPriority=0 isZoneRedundant=false `
    --capabilities EnableServerless `
    --enable-automatic-failover false `
    --backup-policy-type Continuous `
    --output none

Write-Host "  Creating database: AdImpactOsDB"
az cosmosdb sql database create --resource-group $RG --account-name $COSMOS_NAME --name "AdImpactOsDB" --output none

$containers = @(
    @{ Name = "Panelists"; PK = "/panelistId" },
    @{ Name = "Campaigns"; PK = "/campaignId" },
    @{ Name = "Impressions"; PK = "/campaignId" },
    @{ Name = "Surveys"; PK = "/surveyId" },
    @{ Name = "SurveyResponses"; PK = "/surveyId" }
)

Write-Host "  Creating containers..."
foreach ($c in $containers) {
    az cosmosdb sql container create `
        --resource-group $RG `
        --account-name $COSMOS_NAME `
        --database-name "AdImpactOsDB" `
        --name $c.Name `
        --partition-key-path $c.PK `
        --output none
    Write-Host "    + $($c.Name) ($($c.PK))"
}

$COSMOS_ENDPOINT = az cosmosdb show --resource-group $RG --name $COSMOS_NAME --query documentEndpoint --output tsv
$COSMOS_KEY = az cosmosdb keys list --resource-group $RG --name $COSMOS_NAME --query primaryMasterKey --output tsv

# Phase 7: Event Hubs
Write-Host "> Creating Event Hubs Namespace: $EH_NAMESPACE" -ForegroundColor Yellow
az eventhubs namespace create `
    --resource-group $RG --name $EH_NAMESPACE --location $Location `
    --sku Standard --capacity 1 `
    --enable-auto-inflate true --maximum-throughput-units 10 `
    --enable-kafka true --output none

az eventhubs eventhub create `
    --resource-group $RG --namespace-name $EH_NAMESPACE `
    --name "ad-impressions" --partition-count 8 --message-retention 7 --output none

foreach ($cg in @("event-consumer", "monitoring", "archive")) {
    az eventhubs eventhub consumer-group create `
        --resource-group $RG --namespace-name $EH_NAMESPACE `
        --eventhub-name "ad-impressions" --name $cg --output none
}

az eventhubs namespace authorization-rule create `
    --resource-group $RG --namespace-name $EH_NAMESPACE `
    --name "adimpactos-rule" --rights Send Listen --output none

$EH_CONNECTION_STRING = az eventhubs namespace authorization-rule keys list `
    --resource-group $RG --namespace-name $EH_NAMESPACE --name "adimpactos-rule" `
    --query primaryConnectionString --output tsv

# Phase 8: Storage Account
Write-Host "> Creating Storage Account: $STORAGE_NAME" -ForegroundColor Yellow
az storage account create `
    --resource-group $RG --name $STORAGE_NAME --location $Location `
    --sku Standard_LRS --kind StorageV2 `
    --min-tls-version TLS1_2 --allow-blob-public-access false --output none

$STORAGE_CONNECTION_STRING = az storage account show-connection-string `
    --resource-group $RG --name $STORAGE_NAME --query connectionString --output tsv

az storage container create --name "event-consumer-checkpoints" --account-name $STORAGE_NAME --auth-mode login --output none
az storage container create --name "dead-letter-queue" --account-name $STORAGE_NAME --auth-mode login --output none

# Phase 9: Key Vault
Write-Host "> Creating Key Vault: $KV_NAME" -ForegroundColor Yellow
az keyvault create `
    --resource-group $RG --name $KV_NAME --location $Location `
    --sku standard --enable-soft-delete true --retention-days 90 `
    --enable-purge-protection true --output none

$KV_URI = az keyvault show --name $KV_NAME --query properties.vaultUri --output tsv

az keyvault key create `
    --vault-name $KV_NAME --name "panelist-token-key" `
    --kty RSA --size 2048 --protection software --output none

# Phase 10: Application Insights
Write-Host "> Creating Log Analytics + Application Insights" -ForegroundColor Yellow
az monitor log-analytics workspace create `
    --resource-group $RG --workspace-name $LAW_NAME --location $Location --output none

$LAW_ID = az monitor log-analytics workspace show `
    --resource-group $RG --workspace-name $LAW_NAME --query id --output tsv

az monitor app-insights component create `
    --resource-group $RG --app $AI_NAME --location $Location `
    --kind web --workspace $LAW_ID --output none

$AI_CONNECTION_STRING = az monitor app-insights component show `
    --resource-group $RG --app $AI_NAME --query connectionString --output tsv

# Phase 11: App Service Plan
Write-Host "> Creating App Service Plan: $APP_PLAN (B1)" -ForegroundColor Yellow
az appservice plan create `
    --resource-group $RG --name $APP_PLAN --location $Location `
    --is-linux --sku B1 --number-of-workers 1 --output none

# Phase 12: Function App
Write-Host "> Creating Function App: $FUNC_APP (Consumption)" -ForegroundColor Yellow
az functionapp create `
    --resource-group $RG --name $FUNC_APP `
    --storage-account $STORAGE_NAME `
    --consumption-plan-location $Location `
    --runtime dotnet-isolated --runtime-version 10 `
    --functions-version 4 --os-type Linux --output none

# Phase 13: Web Apps
Write-Host "> Creating Web Apps" -ForegroundColor Yellow
$webApps = @("panelist", "campaign", "survey", "dashboard", "demoui", "consumer")
foreach ($app in $webApps) {
    $appName = "$Prefix-$app-$Env"
    az webapp create `
        --resource-group $RG --plan $APP_PLAN --name $appName `
        --deployment-container-image-name "mcr.microsoft.com/dotnet/aspnet:10.0" --output none
    Write-Host "  + $appName"
}

# Phase 14: Managed Identities
Write-Host "> Enabling Managed Identities and granting access" -ForegroundColor Yellow
$ACR_ID = az acr show --name $ACR_NAME --query id --output tsv

foreach ($app in $webApps) {
    $appName = "$Prefix-$app-$Env"
    $principalId = az webapp identity assign --resource-group $RG --name $appName --query principalId --output tsv

    az keyvault set-policy --name $KV_NAME --object-id $principalId --secret-permissions get list --output none
    az role assignment create --assignee $principalId --scope $ACR_ID --role AcrPull --output none
}

$fnPrincipalId = az functionapp identity assign --resource-group $RG --name $FUNC_APP --query principalId --output tsv
az keyvault set-policy --name $KV_NAME --object-id $fnPrincipalId --secret-permissions get list --output none

# Phase 15: Store Secrets
Write-Host "> Storing secrets in Key Vault" -ForegroundColor Yellow
$surveyTokenBytes = [byte[]]::new(32)
[System.Security.Cryptography.RandomNumberGenerator]::Fill($surveyTokenBytes)
$SURVEY_TOKEN_SECRET = [Convert]::ToBase64String($surveyTokenBytes)

az keyvault secret set --vault-name $KV_NAME --name "CosmosDbEndpoint" --value $COSMOS_ENDPOINT --output none
az keyvault secret set --vault-name $KV_NAME --name "CosmosDbKey" --value $COSMOS_KEY --output none
az keyvault secret set --vault-name $KV_NAME --name "EventHubConnectionString" --value $EH_CONNECTION_STRING --output none
az keyvault secret set --vault-name $KV_NAME --name "BlobStorageConnectionString" --value $STORAGE_CONNECTION_STRING --output none
az keyvault secret set --vault-name $KV_NAME --name "AppInsightsConnectionString" --value $AI_CONNECTION_STRING --output none
az keyvault secret set --vault-name $KV_NAME --name "SurveyTokenSecret" --value $SURVEY_TOKEN_SECRET --output none

# Done
Write-Host ""
Write-Host "===========================================" -ForegroundColor Green
Write-Host "All Azure resources provisioned" -ForegroundColor Green
Write-Host "===========================================" -ForegroundColor Green
Write-Host "Resource Group:    $RG"
Write-Host "ACR:               $ACR_LOGIN_SERVER"
Write-Host "Cosmos DB:         $COSMOS_ENDPOINT"
Write-Host "Event Hubs:        $EH_NAMESPACE"
Write-Host "Storage:           $STORAGE_NAME"
Write-Host "Key Vault:         $KV_URI"
Write-Host "App Service Plan:  $APP_PLAN"
Write-Host "Function App:      $FUNC_APP"
Write-Host "===========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Create a service principal for GitHub Actions:"
Write-Host "     az ad sp create-for-rbac --name 'adimpactos-github-sp' ``"
Write-Host "       --role Contributor ``"
Write-Host "       --scopes /subscriptions/`$(az account show --query id -o tsv)/resourceGroups/$RG ``"
Write-Host "       --sdk-auth"
Write-Host "  2. Add the JSON output as AZURE_CREDENTIALS in GitHub repo secrets"
Write-Host "  3. Run: .\scripts\deploy.ps1  (or use the CD workflow)"
