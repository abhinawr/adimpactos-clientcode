#!/usr/bin/env bash
# ============================================================================
# AdImpactOs – Azure Infrastructure Provisioning Script
# Provisions all Azure resources (Phases 4–15 from the deployment guide).
# Idempotent — safe to re-run.
#
# Prerequisites: Azure CLI installed, logged in (az login), subscription set.
#
# Usage:
#   ./scripts/provision-azure.sh
#   ./scripts/provision-azure.sh --prefix myapp --env staging --location westus2
# ============================================================================

set -euo pipefail

# ── Defaults (override with flags) ──
PREFIX="${PREFIX:-adimpact}"
ENV="${ENV:-dev}"
LOCATION="${LOCATION:-eastus}"
ALERT_EMAIL="${ALERT_EMAIL:-tech@theeditorialinstitute.com}"

# Parse arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --prefix)  PREFIX="$2";  shift 2 ;;
    --env)     ENV="$2";     shift 2 ;;
    --location) LOCATION="$2"; shift 2 ;;
    --email)   ALERT_EMAIL="$2"; shift 2 ;;
    *) echo "Unknown argument: $1"; exit 1 ;;
  esac
done

# ── Derived names ──
RG="${PREFIX}-${ENV}-rg"
ACR_NAME="${PREFIX}${ENV}acr"
COSMOS_NAME="${PREFIX}-cosmos-${ENV}"
EH_NAMESPACE="${PREFIX}-eh-${ENV}"
STORAGE_NAME="${PREFIX}${ENV}sa"
KV_NAME="${PREFIX}-kv-${ENV}"
LAW_NAME="${PREFIX}-law-${ENV}"
AI_NAME="${PREFIX}-ai-${ENV}"
APP_PLAN="${PREFIX}-plan-${ENV}"
FUNC_APP="${PREFIX}-fn-${ENV}"

echo "==========================================="
echo "AdImpactOs – Azure Provisioning"
echo "==========================================="
echo "Prefix:     $PREFIX"
echo "Environment: $ENV"
echo "Location:    $LOCATION"
echo "RG:          $RG"
echo "==========================================="

# ── Phase 4: Resource Group ──
echo ""
echo "▸ Creating Resource Group: $RG"
az group create \
  --name "$RG" \
  --location "$LOCATION" \
  --tags project=AdImpactOs environment="$ENV" \
  --output none

# ── Phase 5: Container Registry ──
echo "▸ Creating Container Registry: $ACR_NAME"
az acr create \
  --resource-group "$RG" \
  --name "$ACR_NAME" \
  --sku Basic \
  --location "$LOCATION" \
  --output none
az acr update --name "$ACR_NAME" --admin-enabled true --output none

ACR_LOGIN_SERVER=$(az acr show --name "$ACR_NAME" --query loginServer --output tsv)
echo "  ACR Login Server: $ACR_LOGIN_SERVER"

# ── Phase 6: Cosmos DB (Serverless) ──
echo "▸ Creating Cosmos DB Account: $COSMOS_NAME (serverless)"
az cosmosdb create \
  --resource-group "$RG" \
  --name "$COSMOS_NAME" \
  --kind GlobalDocumentDB \
  --default-consistency-level Session \
  --locations regionName="$LOCATION" failoverPriority=0 isZoneRedundant=false \
  --capabilities EnableServerless \
  --enable-automatic-failover false \
  --backup-policy-type Continuous \
  --output none

echo "  Creating database: AdImpactOsDB"
az cosmosdb sql database create \
  --resource-group "$RG" \
  --account-name "$COSMOS_NAME" \
  --name "AdImpactOsDB" \
  --output none

echo "  Creating containers..."
for CONTAINER_SPEC in "Panelists:/panelistId" "Campaigns:/campaignId" "Impressions:/campaignId" "Surveys:/surveyId" "SurveyResponses:/surveyId"; do
  NAME="${CONTAINER_SPEC%%:*}"
  PK="${CONTAINER_SPEC#*:}"
  az cosmosdb sql container create \
    --resource-group "$RG" \
    --account-name "$COSMOS_NAME" \
    --database-name "AdImpactOsDB" \
    --name "$NAME" \
    --partition-key-path "$PK" \
    --output none
  echo "    ✓ $NAME ($PK)"
done

COSMOS_ENDPOINT=$(az cosmosdb show --resource-group "$RG" --name "$COSMOS_NAME" --query documentEndpoint --output tsv)
COSMOS_KEY=$(az cosmosdb keys list --resource-group "$RG" --name "$COSMOS_NAME" --query primaryMasterKey --output tsv)

# ── Phase 7: Event Hubs ──
echo "▸ Creating Event Hubs Namespace: $EH_NAMESPACE"
az eventhubs namespace create \
  --resource-group "$RG" \
  --name "$EH_NAMESPACE" \
  --location "$LOCATION" \
  --sku Standard \
  --capacity 1 \
  --enable-auto-inflate true \
  --maximum-throughput-units 10 \
  --enable-kafka true \
  --output none

az eventhubs eventhub create \
  --resource-group "$RG" \
  --namespace-name "$EH_NAMESPACE" \
  --name "ad-impressions" \
  --partition-count 8 \
  --message-retention 7 \
  --output none

for CG in "event-consumer" "monitoring" "archive"; do
  az eventhubs eventhub consumer-group create \
    --resource-group "$RG" \
    --namespace-name "$EH_NAMESPACE" \
    --eventhub-name "ad-impressions" \
    --name "$CG" \
    --output none
done

az eventhubs namespace authorization-rule create \
  --resource-group "$RG" \
  --namespace-name "$EH_NAMESPACE" \
  --name "adimpactos-rule" \
  --rights Send Listen \
  --output none

EH_CONNECTION_STRING=$(az eventhubs namespace authorization-rule keys list \
  --resource-group "$RG" --namespace-name "$EH_NAMESPACE" --name "adimpactos-rule" \
  --query primaryConnectionString --output tsv)

# ── Phase 8: Storage Account ──
echo "▸ Creating Storage Account: $STORAGE_NAME"
az storage account create \
  --resource-group "$RG" \
  --name "$STORAGE_NAME" \
  --location "$LOCATION" \
  --sku Standard_LRS \
  --kind StorageV2 \
  --min-tls-version TLS1_2 \
  --allow-blob-public-access false \
  --output none

STORAGE_CONNECTION_STRING=$(az storage account show-connection-string \
  --resource-group "$RG" --name "$STORAGE_NAME" --query connectionString --output tsv)

az storage container create --name "event-consumer-checkpoints" \
  --account-name "$STORAGE_NAME" --auth-mode login --output none
az storage container create --name "dead-letter-queue" \
  --account-name "$STORAGE_NAME" --auth-mode login --output none

# ── Phase 9: Key Vault ──
echo "▸ Creating Key Vault: $KV_NAME"
az keyvault create \
  --resource-group "$RG" \
  --name "$KV_NAME" \
  --location "$LOCATION" \
  --sku standard \
  --enable-soft-delete true \
  --retention-days 90 \
  --enable-purge-protection true \
  --output none

KV_URI=$(az keyvault show --name "$KV_NAME" --query properties.vaultUri --output tsv)

az keyvault key create \
  --vault-name "$KV_NAME" \
  --name "panelist-token-key" \
  --kty RSA \
  --size 2048 \
  --protection software \
  --output none

# ── Phase 10: Application Insights ──
echo "▸ Creating Log Analytics + Application Insights"
az monitor log-analytics workspace create \
  --resource-group "$RG" \
  --workspace-name "$LAW_NAME" \
  --location "$LOCATION" \
  --output none

LAW_ID=$(az monitor log-analytics workspace show \
  --resource-group "$RG" --workspace-name "$LAW_NAME" --query id --output tsv)

az monitor app-insights component create \
  --resource-group "$RG" \
  --app "$AI_NAME" \
  --location "$LOCATION" \
  --kind web \
  --workspace "$LAW_ID" \
  --output none

AI_CONNECTION_STRING=$(az monitor app-insights component show \
  --resource-group "$RG" --app "$AI_NAME" --query connectionString --output tsv)

# ── Phase 11: App Service Plan ──
echo "▸ Creating App Service Plan: $APP_PLAN (B1)"
az appservice plan create \
  --resource-group "$RG" \
  --name "$APP_PLAN" \
  --location "$LOCATION" \
  --is-linux \
  --sku B1 \
  --number-of-workers 1 \
  --output none

# ── Phase 12: Function App ──
echo "▸ Creating Function App: $FUNC_APP (Consumption)"
az functionapp create \
  --resource-group "$RG" \
  --name "$FUNC_APP" \
  --storage-account "$STORAGE_NAME" \
  --consumption-plan-location "$LOCATION" \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4 \
  --os-type Linux \
  --output none

# ── Phase 13: Web Apps ──
echo "▸ Creating Web Apps"
APPS=("panelist" "campaign" "survey" "dashboard" "demoui" "consumer")
for APP in "${APPS[@]}"; do
  APP_NAME="${PREFIX}-${APP}-${ENV}"
  az webapp create \
    --resource-group "$RG" \
    --plan "$APP_PLAN" \
    --name "$APP_NAME" \
    --deployment-container-image-name "mcr.microsoft.com/dotnet/aspnet:8.0" \
    --output none
  echo "  ✓ $APP_NAME"
done

# ── Phase 14: Managed Identities ──
echo "▸ Enabling Managed Identities and granting access"
ACR_ID=$(az acr show --name "$ACR_NAME" --query id --output tsv)

for APP in "${APPS[@]}"; do
  APP_NAME="${PREFIX}-${APP}-${ENV}"
  PRINCIPAL_ID=$(az webapp identity assign \
    --resource-group "$RG" --name "$APP_NAME" --query principalId --output tsv)

  az keyvault set-policy \
    --name "$KV_NAME" --object-id "$PRINCIPAL_ID" \
    --secret-permissions get list --output none

  az role assignment create \
    --assignee "$PRINCIPAL_ID" --scope "$ACR_ID" --role AcrPull --output none
done

FN_PRINCIPAL_ID=$(az functionapp identity assign \
  --resource-group "$RG" --name "$FUNC_APP" --query principalId --output tsv)
az keyvault set-policy \
  --name "$KV_NAME" --object-id "$FN_PRINCIPAL_ID" \
  --secret-permissions get list --output none

# ── Phase 15: Store Secrets ──
echo "▸ Storing secrets in Key Vault"
SURVEY_TOKEN_SECRET=$(openssl rand -base64 32)

az keyvault secret set --vault-name "$KV_NAME" --name "CosmosDbEndpoint" --value "$COSMOS_ENDPOINT" --output none
az keyvault secret set --vault-name "$KV_NAME" --name "CosmosDbKey" --value "$COSMOS_KEY" --output none
az keyvault secret set --vault-name "$KV_NAME" --name "EventHubConnectionString" --value "$EH_CONNECTION_STRING" --output none
az keyvault secret set --vault-name "$KV_NAME" --name "BlobStorageConnectionString" --value "$STORAGE_CONNECTION_STRING" --output none
az keyvault secret set --vault-name "$KV_NAME" --name "AppInsightsConnectionString" --value "$AI_CONNECTION_STRING" --output none
az keyvault secret set --vault-name "$KV_NAME" --name "SurveyTokenSecret" --value "$SURVEY_TOKEN_SECRET" --output none

# ── Done ──
echo ""
echo "==========================================="
echo "✓ All Azure resources provisioned"
echo "==========================================="
echo "Resource Group:    $RG"
echo "ACR:               $ACR_LOGIN_SERVER"
echo "Cosmos DB:         $COSMOS_ENDPOINT"
echo "Event Hubs:        $EH_NAMESPACE"
echo "Storage:           $STORAGE_NAME"
echo "Key Vault:         $KV_URI"
echo "App Service Plan:  $APP_PLAN"
echo "Function App:      $FUNC_APP"
echo "Web Apps:          ${APPS[*]}"
echo "App Insights:      $AI_CONNECTION_STRING"
echo "==========================================="
echo ""
echo "Next steps:"
echo "  1. Create a service principal for GitHub Actions:"
echo "     az ad sp create-for-rbac --name 'adimpactos-github-sp' \\"
echo "       --role Contributor \\"
echo "       --scopes /subscriptions/\$(az account show --query id -o tsv)/resourceGroups/$RG \\"
echo "       --sdk-auth"
echo "  2. Add the JSON output as AZURE_CREDENTIALS in GitHub repo secrets"
echo "  3. Run: ./scripts/deploy.sh  (or use the CD workflow)"
