# AdImpactOs – Complete Azure Deployment Guide

A step-by-step walkthrough that takes you from **zero** (no Azure account) to a fully operational
production deployment of the AdImpactOs platform.

---

## Table of Contents

1. [Information You Will Need – Master Checklist](#information-you-will-need--master-checklist)
2. [Phase 1 – Create an Azure Account](#phase-1--create-an-azure-account)
3. [Phase 2 – Install Required Tools](#phase-2--install-required-tools)
4. [Phase 3 – Authenticate the Azure CLI](#phase-3--authenticate-the-azure-cli)
5. [Phase 4 – Create a Resource Group](#phase-4--create-a-resource-group)
6. [Phase 5 – Create Azure Container Registry](#phase-5--create-azure-container-registry)
7. [Phase 6 – Create Azure Cosmos DB](#phase-6--create-azure-cosmos-db)
8. [Phase 7 – Create Azure Event Hubs](#phase-7--create-azure-event-hubs)
9. [Phase 8 – Create Azure Storage Account](#phase-8--create-azure-storage-account)
10. [Phase 9 – Create Azure Key Vault](#phase-9--create-azure-key-vault)
11. [Phase 10 – Create Application Insights](#phase-10--create-application-insights)
12. [Phase 11 – Create Azure App Service Plan](#phase-11--create-azure-app-service-plan)
13. [Phase 12 – Create Azure Function App](#phase-12--create-azure-function-app)
14. [Phase 13 – Create Web Apps for the APIs and UI](#phase-13--create-web-apps-for-the-apis-and-ui)
15. [Phase 14 – Enable Managed Identities](#phase-14--enable-managed-identities)
16. [Phase 15 – Store Secrets in Key Vault](#phase-15--store-secrets-in-key-vault)
17. [Phase 16 – Build and Push Docker Images](#phase-16--build-and-push-docker-images)
18. [Phase 17 – Configure Application Settings](#phase-17--configure-application-settings)
19. [Phase 18 – Deploy the Azure Function App](#phase-18--deploy-the-azure-function-app)
20. [Phase 19 – Initialize the Cosmos DB Database](#phase-19--initialize-the-cosmos-db-database)
21. [Phase 20 – Verify the Deployment](#phase-20--verify-the-deployment)
22. [Phase 21 – Configure Monitoring and Alerts](#phase-21--configure-monitoring-and-alerts)
23. [Phase 22 – Cost Management](#phase-22--cost-management)
24. [Phase 23 – Optional Extras](#phase-23--optional-extras)
25. [Quick-Reference: All Resource Names](#quick-reference-all-resource-names)
26. [Troubleshooting](#troubleshooting)

---

## Information You Will Need – Master Checklist

Collect all of the items below **before** you start.  
Everything marked **[YOU CHOOSE]** is a name or value you invent; the rest must be obtained from
the listed source.

### Identity & Billing

| #   | Item                                                           | Where to get it                  |
| --- | -------------------------------------------------------------- | -------------------------------- |
| 1   | Personal or company **email address**                          | Your own mailbox                 |
| 2   | **Phone number** (SMS or call) for account verification        | Your mobile                      |
| 3   | **Credit or debit card** (or bank account for invoice billing) | Your finance team                |
| 4   | Company name, address, country, VAT/tax ID (if applicable)     | Your finance team                |
| 5   | Desired **Azure subscription name** [YOU CHOOSE]               | e.g. `AdImpactOs-Dev`            |
| 6   | Monthly **budget limit** in USD [YOU CHOOSE]                   | e.g. `$200` (free trial credit)  |
| 7   | **Alert email** for budget and monitoring alerts [YOU CHOOSE]  | `tech@theeditorialinstitute.com` |

### Azure Region

| #   | Item                                                    | Where to get it                                                                                                                                                         |
| --- | ------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 8   | **Primary Azure region** [YOU CHOOSE]                   | Pick from [Azure regions](https://azure.microsoft.com/en-us/explore/global-infrastructure/geographies/). Recommendation: `eastus` (low cost, wide service availability) |
| 9   | **Secondary region** for disaster recovery [YOU CHOOSE] | e.g. `westeurope`                                                                                                                                                       |

### Naming Conventions

| #   | Item                                 | Recommended value                               |
| --- | ------------------------------------ | ----------------------------------------------- |
| 10  | **Resource group name** [YOU CHOOSE] | `adimpact-dev-rg`                               |
| 11  | **Project prefix** [YOU CHOOSE]      | `adimpact` (max 10 chars, lowercase, no spaces) |
| 12  | **Environment tag** [YOU CHOOSE]     | `dev`                                           |

> **Tip** – Use the prefix everywhere so every resource name is globally unique (Azure enforces
> unique names for storage accounts, Key Vaults, etc.).  
> Example pattern: `<prefix>-<service>-<env>` → `adimpact-cosmos-dev`

### Source Code

| #   | Item                          | Where to get it                                    |
| --- | ----------------------------- | -------------------------------------------------- |
| 13  | Git repository URL            | `https://github.com/<org>/AdImpactOs` or your fork |
| 14  | Branch to deploy [YOU CHOOSE] | e.g. `main`                                        |

---

## Phase 1 – Create an Azure Account

**Time required:** ~10 minutes  
**Cost:** Free (Azure offers a free trial with $200 credit for 30 days)

### Information needed for this phase

| Item              | Details                                                                                                                                           |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| Email address     | Must be a valid, accessible mailbox                                                                                                               |
| Phone number      | For identity verification (SMS or automated call)                                                                                                 |
| Credit/debit card | Card is charged $1 temporarily to verify identity; it is refunded immediately. You will not be charged until you upgrade or exceed the free tier. |
| Country/region    | Your billing country                                                                                                                              |

### Steps

1. Open a browser and navigate to **https://azure.microsoft.com/en-us/free/**.

2. Click **Start free**.

3. Sign in with an existing Microsoft account, or click **Create one** to create a new account:
   - Enter your email address.
   - Create a password (minimum 8 characters, mix of letters, numbers, symbols).
   - Complete email and phone verification.

4. On the **About you** page fill in:
   - Country/region
   - First name, last name
   - Phone number
   - Company name (if applicable)

5. On the **Identity verification by phone** page:
   - Choose **Text me** or **Call me**.
   - Enter the verification code.

6. On the **Identity verification by card** page:
   - Enter your credit or debit card number, expiry date, CVV, cardholder name, and billing address.
   - Click **Next** (a temporary $1 hold is placed and then released).

7. Check **I agree to the subscription agreement, offer details, and privacy statement** and click
   **Sign up**.

8. Wait for the account to be provisioned (~30 seconds).  
   You will be taken to the **Azure Portal** at **https://portal.azure.com**.

> **Note – Free trial**  
> The free trial gives you $200 credit for 30 days plus 12 months of selected free services.  
> This is sufficient for a dev/test deployment. Stay on the free trial and monitor your credit  
> usage in Portal → **Cost Management + Billing** → **Azure credits**. You can upgrade to  
> **Pay-As-You-Go** later when you move to production.

---

## Phase 2 – Install Required Tools

**Time required:** ~20 minutes  
**Prerequisites:** Windows 10/11, macOS, or Ubuntu 20.04+

Install every tool below on the **developer/ops workstation** that will run deployments.

### 2.1 Azure CLI

The Azure CLI is the primary tool used in every subsequent phase.

| OS            | Command                                                                  |
| ------------- | ------------------------------------------------------------------------ |
| Windows       | Download installer from https://aka.ms/installazurecliwindows and run it |
| macOS         | `brew install azure-cli`                                                 |
| Ubuntu/Debian | `curl -sL https://aka.ms/InstallAzureCLIDeb \| sudo bash`                |

Verify:

```bash
az version
# Expected: "azure-cli": "2.50.0" or higher
```

### 2.2 .NET 10 SDK

Required to build and publish the Azure Functions project and all APIs.

| OS      | Command / link                                                                       |
| ------- | ------------------------------------------------------------------------------------ |
| Windows | https://dotnet.microsoft.com/en-us/download/dotnet/10.0 – download the SDK installer |
| macOS   | `brew install dotnet@10`                                                             |
| Ubuntu  | `sudo apt install dotnet-sdk-10.0`                                                   |

Verify:

```bash
dotnet --version
# Expected: 8.x.x
```

### 2.3 Docker Desktop

Required to build the container images for all seven services.

Download from **https://www.docker.com/products/docker-desktop/** and follow the installer.

Verify:

```bash
docker --version
# Expected: Docker version 24.x or higher
docker compose version
# Expected: Docker Compose version v2.x
```

### 2.4 Azure Functions Core Tools

Required to publish the tracking functions directly from the CLI.

```bash
npm install -g azure-functions-core-tools@4 --unsafe-perm true
```

Verify:

```bash
func --version
# Expected: 4.x.x
```

### 2.5 Git

| OS      | Command                          |
| ------- | -------------------------------- |
| Windows | https://git-scm.com/download/win |
| macOS   | `brew install git`               |
| Ubuntu  | `sudo apt install git`           |

Verify:

```bash
git --version
```

---

## Phase 3 – Authenticate the Azure CLI

**Information needed:**

| Item                   | Details                                        |
| ---------------------- | ---------------------------------------------- |
| Azure account email    | The email used when creating the Azure account |
| Azure account password | Set during account creation                    |
| Tenant (Root Group) ID | `84d2a2d3-8846-412a-af03-932ff0b721c8`         |
| Subscription name      | `AdImpactOs-Dev`                               |
| Subscription ID        | `91a6db8a-cc0c-4ae7-93e1-bd9b02291dd9`         |

### Steps

```bash
# 1. Login (opens a browser window)
az login

# 2. List subscriptions to find your Subscription ID
az account list --output table

# 3. Set the active subscription
az account set --subscription "91a6db8a-cc0c-4ae7-93e1-bd9b02291dd9"

# 4. Confirm the active subscription
az account show --query "{Name:name, ID:id, State:state}" --output table
```

Record the **Subscription ID** – you will need it in later commands.

```bash
# Save to a shell variable for convenience (all phases below reference $SUBSCRIPTION_ID)
export SUBSCRIPTION_ID="91a6db8a-cc0c-4ae7-93e1-bd9b02291dd9"
export LOCATION="eastus"           # Change to your chosen region
export PREFIX="adimpact"           # Your chosen prefix (lowercase, no spaces)
export ENV="dev"
export RG="${PREFIX}-${ENV}-rg"    # adimpact-dev-rg
```

> **Windows PowerShell alternative:**
>
> ```powershell
> $SUBSCRIPTION_ID = "91a6db8a-cc0c-4ae7-93e1-bd9b02291dd9"
> $LOCATION        = "eastus"
> $PREFIX          = "adimpact"
> $ENV             = "dev"
> $RG              = "$PREFIX-$ENV-rg"
> ```

---

## Phase 4 – Create a Resource Group

A resource group is a logical container for all Azure resources in this project.

**Information needed:**

| Item                | Example value     | Notes                                     |
| ------------------- | ----------------- | ----------------------------------------- |
| Resource group name | `adimpact-dev-rg` | Unique within the subscription            |
| Location            | `eastus`          | All resources will default to this region |

```bash
az group create \
  --name "$RG" \
  --location "$LOCATION" \
  --tags project=AdImpactOs environment=$ENV

# Confirm
az group show --name "$RG" --query "{Name:name, Location:location, State:properties.provisioningState}"
```

---

## Phase 5 – Create Azure Container Registry

All service Docker images are stored here. The App Services and Azure Functions pull images
from this registry.

**Information needed:**

| Item          | Example value    | Notes                                                               |
| ------------- | ---------------- | ------------------------------------------------------------------- |
| Registry name | `adimpactdevacr` | **Globally unique**, 5–50 chars, lowercase alphanumeric only        |
| SKU           | `Basic`          | `Basic` (dev), `Standard` (production), `Premium` (geo-replication) |

```bash
export ACR_NAME="${PREFIX}${ENV}acr"    # adimpactdevacr

az acr create \
  --resource-group "$RG" \
  --name "$ACR_NAME" \
  --sku Basic \
  --location "$LOCATION"

# Enable admin access so App Service can pull images
az acr update --name "$ACR_NAME" --admin-enabled true

# Retrieve login server URL
export ACR_LOGIN_SERVER=$(az acr show --name "$ACR_NAME" --query loginServer --output tsv)
echo "ACR Login Server: $ACR_LOGIN_SERVER"
# e.g. adimpactdevacr.azurecr.io

# Log Docker in to the registry
az acr login --name "$ACR_NAME"
```

---

## Phase 6 – Create Azure Cosmos DB

Cosmos DB is the primary operational database holding Panelists, Campaigns, Impressions, Surveys,
and SurveyResponses.

**Information needed:**

| Item              | Example value                       | Notes                                                    |
| ----------------- | ----------------------------------- | -------------------------------------------------------- |
| Account name      | `adimpact-cosmos-dev`               | Globally unique, 3–44 chars, lowercase                   |
| API kind          | `GlobalDocumentDB` (Core / SQL API) | Used by the .NET SDK                                     |
| Consistency level | `Session`                           | Balance between performance and consistency              |
| Database name     | `AdImpactOsDB`                      | Must match `appsettings.json` → `CosmosDb__DatabaseName` |

### 6.1 Create the Cosmos DB Account

```bash
export COSMOS_NAME="${PREFIX}-cosmos-${ENV}"   # adimpact-cosmos-dev

az cosmosdb create \
  --resource-group "$RG" \
  --name "$COSMOS_NAME" \
  --kind GlobalDocumentDB \
  --default-consistency-level Session \
  --locations regionName="$LOCATION" failoverPriority=0 isZoneRedundant=false \
  --capabilities EnableServerless \
  --enable-automatic-failover false \
  --backup-policy-type Continuous

echo "Cosmos DB account created: $COSMOS_NAME"
```

> **Continuous backup** is enabled so you can restore to any point in time within 30 days.

### 6.2 Create the Database

```bash
az cosmosdb sql database create \
  --resource-group "$RG" \
  --account-name "$COSMOS_NAME" \
  --name "AdImpactOsDB"
```

### 6.3 Create the Five Containers

Each container requires a **partition key** (the field Cosmos DB uses to shard data).

> **Note:** Serverless mode does not use provisioned throughput — omit `--throughput` from container creation.

```bash
# Panelists – partition key: /panelistId
az cosmosdb sql container create \
  --resource-group "$RG" \
  --account-name "$COSMOS_NAME" \
  --database-name "AdImpactOsDB" \
  --name "Panelists" \
  --partition-key-path "/panelistId"

# Campaigns – partition key: /campaignId
az cosmosdb sql container create \
  --resource-group "$RG" \
  --account-name "$COSMOS_NAME" \
  --database-name "AdImpactOsDB" \
  --name "Campaigns" \
  --partition-key-path "/campaignId"

# Impressions – partition key: /campaignId
az cosmosdb sql container create \
  --resource-group "$RG" \
  --account-name "$COSMOS_NAME" \
  --database-name "AdImpactOsDB" \
  --name "Impressions" \
  --partition-key-path "/campaignId"

# Surveys – partition key: /surveyId
az cosmosdb sql container create \
  --resource-group "$RG" \
  --account-name "$COSMOS_NAME" \
  --database-name "AdImpactOsDB" \
  --name "Surveys" \
  --partition-key-path "/surveyId"

# SurveyResponses – partition key: /surveyId
az cosmosdb sql container create \
  --resource-group "$RG" \
  --account-name "$COSMOS_NAME" \
  --database-name "AdImpactOsDB" \
  --name "SurveyResponses" \
  --partition-key-path "/surveyId"
```

### 6.4 Retrieve Connection Details

```bash
# Endpoint URL
export COSMOS_ENDPOINT=$(az cosmosdb show \
  --resource-group "$RG" \
  --name "$COSMOS_NAME" \
  --query documentEndpoint --output tsv)

# Primary master key (treat as a password – store it in Key Vault in Phase 15)
export COSMOS_KEY=$(az cosmosdb keys list \
  --resource-group "$RG" \
  --name "$COSMOS_NAME" \
  --query primaryMasterKey --output tsv)

echo "Cosmos Endpoint : $COSMOS_ENDPOINT"
echo "Cosmos Key      : $COSMOS_KEY"
```

---

## Phase 7 – Create Azure Event Hubs

Event Hubs receives ad impression events from the pixel tracker and S2S function and delivers
them to the Event Consumer worker service.

**Information needed:**

| Item              | Example value     | Notes                                                        |
| ----------------- | ----------------- | ------------------------------------------------------------ |
| Namespace name    | `adimpact-eh-dev` | Globally unique, 6–50 chars                                  |
| SKU               | `Standard`        | Standard tier required for consumer groups and Kafka surface |
| Throughput units  | `2`               | Start with 2; auto-inflate can grow to 10                    |
| Event Hub name    | `ad-impressions`  | Must match `EventHub__Name` config                           |
| Partition count   | `8`               | Parallelism level for the Event Consumer                     |
| Message retention | `7`               | Days to retain messages                                      |

### 7.1 Create the Namespace

```bash
export EH_NAMESPACE="${PREFIX}-eh-${ENV}"   # adimpact-eh-dev

az eventhubs namespace create \
  --resource-group "$RG" \
  --name "$EH_NAMESPACE" \
  --location "$LOCATION" \
  --sku Standard \
  --capacity 2 \
  --enable-auto-inflate true \
  --maximum-throughput-units 10 \
  --enable-kafka true
```

### 7.2 Create the Event Hub

```bash
az eventhubs eventhub create \
  --resource-group "$RG" \
  --namespace-name "$EH_NAMESPACE" \
  --name "ad-impressions" \
  --partition-count 8 \
  --message-retention 7
```

### 7.3 Create Consumer Groups

```bash
# Used by the Event Consumer service
az eventhubs eventhub consumer-group create \
  --resource-group "$RG" \
  --namespace-name "$EH_NAMESPACE" \
  --eventhub-name "ad-impressions" \
  --name "event-consumer"

# Used by monitoring
az eventhubs eventhub consumer-group create \
  --resource-group "$RG" \
  --namespace-name "$EH_NAMESPACE" \
  --eventhub-name "ad-impressions" \
  --name "monitoring"

# Used for archiving
az eventhubs eventhub consumer-group create \
  --resource-group "$RG" \
  --namespace-name "$EH_NAMESPACE" \
  --eventhub-name "ad-impressions" \
  --name "archive"
```

### 7.4 Create an Authorization Rule and Retrieve the Connection String

```bash
# Create a rule with Send + Listen permissions
az eventhubs namespace authorization-rule create \
  --resource-group "$RG" \
  --namespace-name "$EH_NAMESPACE" \
  --name "adimpactos-rule" \
  --rights Send Listen

# Retrieve the primary connection string
export EH_CONNECTION_STRING=$(az eventhubs namespace authorization-rule keys list \
  --resource-group "$RG" \
  --namespace-name "$EH_NAMESPACE" \
  --name "adimpactos-rule" \
  --query primaryConnectionString --output tsv)

echo "Event Hub Connection String: $EH_CONNECTION_STRING"
```

---

## Phase 8 – Create Azure Storage Account

The storage account provides:

- **Blob storage** – checkpoint store for the Event Consumer and dead-letter-queue (DLQ) blobs.
- **Azure Functions host** – Functions requires a storage account for internal operations.

**Information needed:**

| Item                 | Example value   | Notes                                                        |
| -------------------- | --------------- | ------------------------------------------------------------ |
| Storage account name | `adimpactdevsa` | Globally unique, 3–24 chars, lowercase alphanumeric          |
| SKU                  | `Standard_LRS`  | Locally redundant (use `Standard_GRS` for higher durability) |
| Kind                 | `StorageV2`     | General-purpose v2                                           |

```bash
export STORAGE_NAME="${PREFIX}${ENV}sa"   # adimpactdevsa

az storage account create \
  --resource-group "$RG" \
  --name "$STORAGE_NAME" \
  --location "$LOCATION" \
  --sku Standard_LRS \
  --kind StorageV2 \
  --min-tls-version TLS1_2 \
  --allow-blob-public-access false

# Retrieve the connection string
export STORAGE_CONNECTION_STRING=$(az storage account show-connection-string \
  --resource-group "$RG" \
  --name "$STORAGE_NAME" \
  --query connectionString --output tsv)

echo "Storage Connection String: $STORAGE_CONNECTION_STRING"

# Create the checkpoint and DLQ containers
az storage container create --name "event-consumer-checkpoints" \
  --account-name "$STORAGE_NAME" --auth-mode login
az storage container create --name "dead-letter-queue" \
  --account-name "$STORAGE_NAME" --auth-mode login
```

---

## Phase 9 – Create Azure Key Vault

Key Vault stores all secrets (connection strings, API keys, encryption keys) so they never
appear in application configuration files or Docker images.

**Information needed:**

| Item                  | Example value     | Notes                                  |
| --------------------- | ----------------- | -------------------------------------- |
| Key Vault name        | `adimpact-kv-dev` | Globally unique, 3–24 chars            |
| SKU                   | `standard`        | `premium` for HSM-backed keys          |
| Soft-delete retention | `90` days         | Prevents accidental permanent deletion |

```bash
export KV_NAME="${PREFIX}-kv-${ENV}"   # adimpact-kv-dev

az keyvault create \
  --resource-group "$RG" \
  --name "$KV_NAME" \
  --location "$LOCATION" \
  --sku standard \
  --enable-soft-delete true \
  --retention-days 90 \
  --enable-purge-protection true

export KV_URI=$(az keyvault show --name "$KV_NAME" --query properties.vaultUri --output tsv)
echo "Key Vault URI: $KV_URI"
```

### Create the Panelist Token Encryption Key

```bash
az keyvault key create \
  --vault-name "$KV_NAME" \
  --name "panelist-token-key" \
  --kty RSA \
  --size 2048 \
  --protection software
```

---

## Phase 10 – Create Application Insights

Application Insights provides real-time telemetry, request tracing, error logging, and custom
dashboards for all services.

**Information needed:**

| Item              | Example value      | Notes                                               |
| ----------------- | ------------------ | --------------------------------------------------- |
| Workspace name    | `adimpact-law-dev` | Log Analytics Workspace for Application Insights v2 |
| App Insights name | `adimpact-ai-dev`  |                                                     |

```bash
export LAW_NAME="${PREFIX}-law-${ENV}"
export AI_NAME="${PREFIX}-ai-${ENV}"

# Create Log Analytics Workspace (required for workspace-based App Insights)
az monitor log-analytics workspace create \
  --resource-group "$RG" \
  --workspace-name "$LAW_NAME" \
  --location "$LOCATION"

export LAW_ID=$(az monitor log-analytics workspace show \
  --resource-group "$RG" \
  --workspace-name "$LAW_NAME" \
  --query id --output tsv)

# Create Application Insights
az monitor app-insights component create \
  --resource-group "$RG" \
  --app "$AI_NAME" \
  --location "$LOCATION" \
  --kind web \
  --workspace "$LAW_ID"

# Retrieve the Instrumentation Key and Connection String
export AI_CONNECTION_STRING=$(az monitor app-insights component show \
  --resource-group "$RG" \
  --app "$AI_NAME" \
  --query connectionString --output tsv)

echo "App Insights Connection String: $AI_CONNECTION_STRING"
```

---

## Phase 11 – Create Azure App Service Plan

The App Service Plan defines the compute tier for all seven web applications (APIs, Dashboard,
Demo UI).

**Information needed:**

| Item         | Example value       | Notes                                                                                 |
| ------------ | ------------------- | ------------------------------------------------------------------------------------- |
| Plan name    | `adimpact-plan-dev` |                                                                                       |
| SKU          | `B1`                | B1 = 1 vCPU, 1.75 GB RAM per instance. Ideal for dev/test. Use `P1v3` for production. |
| OS           | `Linux`             | Required for Docker container deployment                                              |
| Worker count | `1`                 | Scale up as load increases                                                            |

```bash
export APP_PLAN="${PREFIX}-plan-${ENV}"

az appservice plan create \
  --resource-group "$RG" \
  --name "$APP_PLAN" \
  --location "$LOCATION" \
  --is-linux \
  --sku B1 \
  --number-of-workers 1
```

---

## Phase 12 – Create Azure Function App

The Function App hosts the **Pixel Tracker** (returns a 1×1 GIF) and the **S2S Ingest API**
(accepts JSON payloads from ad servers).

**Information needed:**

| Item              | Example value     | Notes                                             |
| ----------------- | ----------------- | ------------------------------------------------- |
| Function App name | `adimpact-fn-dev` | Globally unique, lowercase, alphanumeric, hyphens |
| Runtime           | `dotnet-isolated` | Must match `FUNCTIONS_WORKER_RUNTIME`             |
| .NET version      | `10.0`            |                                                   |
| Functions version | `4`               | Azure Functions v4                                |
| Storage account   | `adimpactdevsa`   | Same storage account created in Phase 8           |

```bash
export FUNC_APP="${PREFIX}-fn-${ENV}"

az functionapp create \
  --resource-group "$RG" \
  --name "$FUNC_APP" \
  --storage-account "$STORAGE_NAME" \
  --consumption-plan-location "$LOCATION" \
  --runtime dotnet-isolated \
  --runtime-version 10 \
  --functions-version 4 \
  --os-type Linux

echo "Function App created: $FUNC_APP"
```

> **Consumption vs. Premium plan**  
> The Consumption plan above is pay-per-execution (cheapest for dev/test).  
> For production at high scale, use `--plan $APP_PLAN` with a Premium plan instead of
> `--consumption-plan-location`, so you get dedicated instances and VNet integration.

---

## Phase 13 – Create Web Apps for the APIs and UI

Create one Azure Web App (App Service) for each of the five containerised services:

| Service        | App name                 | Internal port |
| -------------- | ------------------------ | ------------- |
| Panelist API   | `adimpact-panelist-dev`  | 8080          |
| Campaign API   | `adimpact-campaign-dev`  | 8080          |
| Survey API     | `adimpact-survey-dev`    | 8080          |
| Dashboard      | `adimpact-dashboard-dev` | 8080          |
| Demo UI        | `adimpact-demoui-dev`    | 8080          |
| Event Consumer | `adimpact-consumer-dev`  | N/A (worker)  |

**Information needed per app:**

| Item             | Details                                               |
| ---------------- | ----------------------------------------------------- |
| App name         | Globally unique (Azure adds `.azurewebsites.net`)     |
| Container image  | `<acr-login-server>/<image>:latest` (set in Phase 16) |
| App Service Plan | `adimpact-plan-dev` (created in Phase 11)             |

```bash
# Helper function
create_webapp() {
  local APP_NAME=$1
  az webapp create \
    --resource-group "$RG" \
    --plan "$APP_PLAN" \
    --name "$APP_NAME" \
    --deployment-container-image-name "mcr.microsoft.com/dotnet/aspnet:10.0"
  # Container image is updated in Phase 16 after Docker build
}

create_webapp "${PREFIX}-panelist-${ENV}"
create_webapp "${PREFIX}-campaign-${ENV}"
create_webapp "${PREFIX}-survey-${ENV}"
create_webapp "${PREFIX}-dashboard-${ENV}"
create_webapp "${PREFIX}-demoui-${ENV}"
create_webapp "${PREFIX}-consumer-${ENV}"

echo "All web apps created."
```

---

## Phase 14 – Enable Managed Identities

Managed Identities let each Azure service authenticate to Key Vault and other resources
**without storing credentials**. This is the recommended secure approach.

```bash
# Enable system-assigned identity for each app
APPS=(
  "${PREFIX}-panelist-${ENV}"
  "${PREFIX}-campaign-${ENV}"
  "${PREFIX}-survey-${ENV}"
  "${PREFIX}-dashboard-${ENV}"
  "${PREFIX}-demoui-${ENV}"
  "${PREFIX}-consumer-${ENV}"
)

for APP in "${APPS[@]}"; do
  PRINCIPAL_ID=$(az webapp identity assign \
    --resource-group "$RG" \
    --name "$APP" \
    --query principalId --output tsv)

  echo "Assigned identity to $APP → principal: $PRINCIPAL_ID"

  # Grant Key Vault access (read secrets)
  az keyvault set-policy \
    --name "$KV_NAME" \
    --object-id "$PRINCIPAL_ID" \
    --secret-permissions get list
done

# Enable identity for the Function App
FN_PRINCIPAL_ID=$(az functionapp identity assign \
  --resource-group "$RG" \
  --name "$FUNC_APP" \
  --query principalId --output tsv)

az keyvault set-policy \
  --name "$KV_NAME" \
  --object-id "$FN_PRINCIPAL_ID" \
  --secret-permissions get list
```

Also grant the ACR pull permission so App Services can download container images:

```bash
ACR_ID=$(az acr show --name "$ACR_NAME" --query id --output tsv)

for APP in "${APPS[@]}"; do
  PRINCIPAL_ID=$(az webapp identity show \
    --resource-group "$RG" \
    --name "$APP" \
    --query principalId --output tsv)

  az role assignment create \
    --assignee "$PRINCIPAL_ID" \
    --scope "$ACR_ID" \
    --role AcrPull
done
```

---

## Phase 15 – Store Secrets in Key Vault

All sensitive configuration values are written to Key Vault **once** here.
Application settings reference them using the `@Microsoft.KeyVault(...)` syntax.

**Information needed:**

| Secret name                   | Value                                 | Source       |
| ----------------------------- | ------------------------------------- | ------------ |
| `CosmosDbEndpoint`            | `$COSMOS_ENDPOINT`                    | Phase 6.4    |
| `CosmosDbKey`                 | `$COSMOS_KEY`                         | Phase 6.4    |
| `EventHubConnectionString`    | `$EH_CONNECTION_STRING`               | Phase 7.4    |
| `BlobStorageConnectionString` | `$STORAGE_CONNECTION_STRING`          | Phase 8      |
| `AppInsightsConnectionString` | `$AI_CONNECTION_STRING`               | Phase 10     |
| `SurveyTokenSecret`           | Any strong random string you generate | You generate |

```bash
# Generate a strong survey token secret (32 random bytes, base64)
export SURVEY_TOKEN_SECRET=$(openssl rand -base64 32)

# Store all secrets
az keyvault secret set --vault-name "$KV_NAME" \
  --name "CosmosDbEndpoint" --value "$COSMOS_ENDPOINT"

az keyvault secret set --vault-name "$KV_NAME" \
  --name "CosmosDbKey" --value "$COSMOS_KEY"

az keyvault secret set --vault-name "$KV_NAME" \
  --name "EventHubConnectionString" --value "$EH_CONNECTION_STRING"

az keyvault secret set --vault-name "$KV_NAME" \
  --name "BlobStorageConnectionString" --value "$STORAGE_CONNECTION_STRING"

az keyvault secret set --vault-name "$KV_NAME" \
  --name "AppInsightsConnectionString" --value "$AI_CONNECTION_STRING"

az keyvault secret set --vault-name "$KV_NAME" \
  --name "SurveyTokenSecret" --value "$SURVEY_TOKEN_SECRET"

echo "All secrets stored in Key Vault."
```

> **Security note** – Never commit any of the exported shell variables (keys, connection strings)
> to source control. Clear your terminal history when done if on a shared workstation.

---

## Phase 16 – Build and Push Docker Images

Build each service image and push it to the Azure Container Registry created in Phase 5.

**Information needed:**

| Item             | Details                                      |
| ---------------- | -------------------------------------------- |
| Repository root  | `/path/to/AdImpactOs` (local clone)          |
| ACR login server | `$ACR_LOGIN_SERVER` (set in Phase 5)         |
| Image tag        | `latest` or a semantic version, e.g. `1.0.0` |

```bash
# Navigate to repository root
cd /path/to/AdImpactOs

# Log in to ACR (if session expired)
az acr login --name "$ACR_NAME"

# Panelist API
docker build -t "${ACR_LOGIN_SERVER}/panelistapi:latest" \
  -f src/AdImpactOs.PanelistAPI/Dockerfile .
docker push "${ACR_LOGIN_SERVER}/panelistapi:latest"

# Campaign API
docker build -t "${ACR_LOGIN_SERVER}/campaignapi:latest" \
  -f src/AdImpactOs.Campaign/Dockerfile .
docker push "${ACR_LOGIN_SERVER}/campaignapi:latest"

# Survey API
docker build -t "${ACR_LOGIN_SERVER}/surveyapi:latest" \
  -f src/AdImpactOs.Survey/Dockerfile .
docker push "${ACR_LOGIN_SERVER}/surveyapi:latest"

# Dashboard
docker build -t "${ACR_LOGIN_SERVER}/dashboard:latest" \
  -f src/AdImpactOs.Dashboard/Dockerfile .
docker push "${ACR_LOGIN_SERVER}/dashboard:latest"

# Demo UI
docker build -t "${ACR_LOGIN_SERVER}/demoui:latest" \
  -f src/AdImpactOs.DemoUI/Dockerfile .
docker push "${ACR_LOGIN_SERVER}/demoui:latest"

# Event Consumer
docker build -t "${ACR_LOGIN_SERVER}/eventconsumer:latest" \
  -f src/AdImpactOs.EventConsumer/Dockerfile .
docker push "${ACR_LOGIN_SERVER}/eventconsumer:latest"

echo "All images pushed to $ACR_LOGIN_SERVER"
```

### Update App Services to Use the ACR Images

```bash
# Retrieve ACR admin credentials (used by App Service to pull images)
ACR_USERNAME=$(az acr credential show --name "$ACR_NAME" --query username --output tsv)
ACR_PASSWORD=$(az acr credential show --name "$ACR_NAME" --query passwords[0].value --output tsv)

declare -A IMAGES=(
  ["${PREFIX}-panelist-${ENV}"]="panelistapi:latest"
  ["${PREFIX}-campaign-${ENV}"]="campaignapi:latest"
  ["${PREFIX}-survey-${ENV}"]="surveyapi:latest"
  ["${PREFIX}-dashboard-${ENV}"]="dashboard:latest"
  ["${PREFIX}-demoui-${ENV}"]="demoui:latest"
  ["${PREFIX}-consumer-${ENV}"]="eventconsumer:latest"
)

for APP in "${!IMAGES[@]}"; do
  IMAGE="${ACR_LOGIN_SERVER}/${IMAGES[$APP]}"
  az webapp config container set \
    --resource-group "$RG" \
    --name "$APP" \
    --docker-custom-image-name "$IMAGE" \
    --docker-registry-server-url "https://${ACR_LOGIN_SERVER}" \
    --docker-registry-server-user "$ACR_USERNAME" \
    --docker-registry-server-password "$ACR_PASSWORD"
  echo "Updated $APP → $IMAGE"
done
```

---

## Phase 17 – Configure Application Settings

Each service needs environment variables that point to the Azure resources created above.
Key Vault references (`@Microsoft.KeyVault(...)`) are used for every secret so the actual
values never appear in App Service configuration.

### Build the Key Vault reference strings

```bash
# Helper: builds a Key Vault secret reference URI
kv_ref() {
  local SECRET_NAME=$1
  local SECRET_URI="${KV_URI}secrets/${SECRET_NAME}"
  echo "@Microsoft.KeyVault(SecretUri=${SECRET_URI})"
}

COSMOS_ENDPOINT_REF=$(kv_ref "CosmosDbEndpoint")
COSMOS_KEY_REF=$(kv_ref "CosmosDbKey")
EH_REF=$(kv_ref "EventHubConnectionString")
STORAGE_REF=$(kv_ref "BlobStorageConnectionString")
AI_REF=$(kv_ref "AppInsightsConnectionString")
SURVEY_TOKEN_REF=$(kv_ref "SurveyTokenSecret")
```

### 17.1 Panelist API

```bash
az webapp config appsettings set \
  --resource-group "$RG" \
  --name "${PREFIX}-panelist-${ENV}" \
  --settings \
    ASPNETCORE_ENVIRONMENT="Development" \
    CosmosDb__Endpoint="$COSMOS_ENDPOINT_REF" \
    CosmosDb__Key="$COSMOS_KEY_REF" \
    CosmosDb__DatabaseName="AdImpactOsDB" \
    CosmosDb__ContainerName="Panelists" \
    APPLICATIONINSIGHTS_CONNECTION_STRING="$AI_REF" \
    WEBSITES_PORT=8080
```

### 17.2 Campaign API

```bash
az webapp config appsettings set \
  --resource-group "$RG" \
  --name "${PREFIX}-campaign-${ENV}" \
  --settings \
    ASPNETCORE_ENVIRONMENT="Development" \
    CosmosDb__Endpoint="$COSMOS_ENDPOINT_REF" \
    CosmosDb__Key="$COSMOS_KEY_REF" \
    CosmosDb__DatabaseName="AdImpactOsDB" \
    CosmosDb__ContainerName="Campaigns" \
    CosmosDb__ImpressionContainerName="Impressions" \
    APPLICATIONINSIGHTS_CONNECTION_STRING="$AI_REF" \
    WEBSITES_PORT=8080
```

### 17.3 Survey API

```bash
az webapp config appsettings set \
  --resource-group "$RG" \
  --name "${PREFIX}-survey-${ENV}" \
  --settings \
    ASPNETCORE_ENVIRONMENT="Development" \
    CosmosDb__Endpoint="$COSMOS_ENDPOINT_REF" \
    CosmosDb__Key="$COSMOS_KEY_REF" \
    CosmosDb__DatabaseName="AdImpactOsDB" \
    CosmosDb__SurveyContainerName="Surveys" \
    CosmosDb__SurveyResponseContainerName="SurveyResponses" \
    SurveyToken__Secret="$SURVEY_TOKEN_REF" \
    SurveyToken__BaseUrl="https://${PREFIX}-survey-${ENV}.azurewebsites.net" \
    SurveyToken__ExpiryHours="168" \
    APPLICATIONINSIGHTS_CONNECTION_STRING="$AI_REF" \
    WEBSITES_PORT=8080
```

### 17.4 Dashboard

```bash
az webapp config appsettings set \
  --resource-group "$RG" \
  --name "${PREFIX}-dashboard-${ENV}" \
  --settings \
    ASPNETCORE_ENVIRONMENT="Development" \
    PanelistApiBaseUrl="https://${PREFIX}-panelist-${ENV}.azurewebsites.net" \
    CampaignApiBaseUrl="https://${PREFIX}-campaign-${ENV}.azurewebsites.net" \
    SurveyApiBaseUrl="https://${PREFIX}-survey-${ENV}.azurewebsites.net" \
    AdImpactOsFunctionsBaseUrl="https://${PREFIX}-fn-${ENV}.azurewebsites.net" \
    APPLICATIONINSIGHTS_CONNECTION_STRING="$AI_REF" \
    WEBSITES_PORT=8080
```

### 17.5 Demo UI

```bash
az webapp config appsettings set \
  --resource-group "$RG" \
  --name "${PREFIX}-demoui-${ENV}" \
  --settings \
    ASPNETCORE_ENVIRONMENT="Development" \
    AdImpactOsFunctionsBaseUrl="https://${PREFIX}-fn-${ENV}.azurewebsites.net" \
    APPLICATIONINSIGHTS_CONNECTION_STRING="$AI_REF" \
    WEBSITES_PORT=8080
```

### 17.6 Event Consumer

```bash
az webapp config appsettings set \
  --resource-group "$RG" \
  --name "${PREFIX}-consumer-${ENV}" \
  --settings \
    DOTNET_ENVIRONMENT="Development" \
    EventHub__ConnectionString="$EH_REF" \
    EventHub__Name="ad-impressions" \
    EventHub__ConsumerGroup="event-consumer" \
    BlobStorage__ConnectionString="$STORAGE_REF" \
    BlobStorage__CheckpointContainerName="event-consumer-checkpoints" \
    CosmosDb__Endpoint="$COSMOS_ENDPOINT_REF" \
    CosmosDb__Key="$COSMOS_KEY_REF" \
    CosmosDb__DatabaseName="AdImpactOsDB" \
    CosmosDb__ImpressionContainerName="Impressions" \
    KeyVault__Url="$KV_URI" \
    KeyVault__EncryptionKeyName="panelist-token-key" \
    APPLICATIONINSIGHTS_CONNECTION_STRING="$AI_REF"
```

### 17.7 Azure Function App

```bash
az functionapp config appsettings set \
  --resource-group "$RG" \
  --name "$FUNC_APP" \
  --settings \
    FUNCTIONS_WORKER_RUNTIME="dotnet-isolated" \
    AzureWebJobsStorage="$STORAGE_REF" \
    EventHubConnection="$EH_REF" \
    EventHub__Name="ad-impressions" \
    KeyVault__Url="$KV_URI" \
    KeyVault__EncryptionKeyName="panelist-token-key" \
    APPLICATIONINSIGHTS_CONNECTION_STRING="$AI_REF"
```

---

## Phase 18 – Deploy the Azure Function App

The Functions project is deployed directly using the Azure Functions Core Tools (not Docker).

**Information needed:**

| Item              | Details                            |
| ----------------- | ---------------------------------- |
| Function App name | `$FUNC_APP`                        |
| Source directory  | `src/AdImpactOs` in the repository |

```bash
cd /path/to/AdImpactOs/src/AdImpactOs

# Build in Release mode
dotnet publish -c Release -o ./publish

# Deploy to Azure
func azure functionapp publish "$FUNC_APP" --dotnet-isolated

echo "Function App deployed."
```

Verify the deployment in the Azure Portal:

1. Navigate to **Function Apps** → `$FUNC_APP` → **Functions**.
2. You should see `PixelTracker` and `S2SIngest` listed as Active.

---

## Phase 19 – Initialize the Cosmos DB Database

The application uses migrations/seed scripts to create indexes, unique key constraints, and initial
seed data in Cosmos DB.

**Option A – Run migrations via the API (recommended)**

Each API has a `/migrate` or `/health` endpoint that triggers migration on first startup.
The easiest approach is to call the migration endpoint right after deployment:

```bash
PANELIST_URL="https://${PREFIX}-panelist-${ENV}.azurewebsites.net"
CAMPAIGN_URL="https://${PREFIX}-campaign-${ENV}.azurewebsites.net"
SURVEY_URL="https://${PREFIX}-survey-${ENV}.azurewebsites.net"

# Trigger migrations (the APIs run EnsureCreated / migration scripts on startup)
curl -X POST "${PANELIST_URL}/migrate" -H "Content-Type: application/json" || true
curl -X POST "${CAMPAIGN_URL}/migrate" -H "Content-Type: application/json" || true
curl -X POST "${SURVEY_URL}/migrate" -H "Content-Type: application/json" || true

echo "Migration endpoints called."
```

**Option B – Restart the App Services** (migrations run automatically on startup)

```bash
az webapp restart --resource-group "$RG" --name "${PREFIX}-panelist-${ENV}"
az webapp restart --resource-group "$RG" --name "${PREFIX}-campaign-${ENV}"
az webapp restart --resource-group "$RG" --name "${PREFIX}-survey-${ENV}"
```

**Option C – Load demo data (optional)**

```bash
cd /path/to/AdImpactOs

# Adjust the BaseUrl variables inside the script to point to your Azure endpoints
# then run:
pwsh ./demo/Scripts/LoadDemoData.ps1 \
  -PanelistApiUrl "$PANELIST_URL" \
  -CampaignApiUrl "$CAMPAIGN_URL" \
  -SurveyApiUrl   "$SURVEY_URL"
```

---

## Phase 20 – Verify the Deployment

### 20.1 Health Checks

```bash
# Replace with your actual URLs
PANELIST_URL="https://${PREFIX}-panelist-${ENV}.azurewebsites.net"
CAMPAIGN_URL="https://${PREFIX}-campaign-${ENV}.azurewebsites.net"
SURVEY_URL="https://${PREFIX}-survey-${ENV}.azurewebsites.net"
DASHBOARD_URL="https://${PREFIX}-dashboard-${ENV}.azurewebsites.net"
DEMOUI_URL="https://${PREFIX}-demoui-${ENV}.azurewebsites.net"
FUNCTIONS_URL="https://${PREFIX}-fn-${ENV}.azurewebsites.net"

echo "--- Health Checks ---"
curl -sf "${PANELIST_URL}/health"  && echo " Panelist API  OK" || echo " Panelist API  FAIL"
curl -sf "${CAMPAIGN_URL}/health"  && echo " Campaign API  OK" || echo " Campaign API  FAIL"
curl -sf "${SURVEY_URL}/health"    && echo " Survey API    OK" || echo " Survey API    FAIL"
curl -sf "${DASHBOARD_URL}/"       && echo " Dashboard     OK" || echo " Dashboard     FAIL"
curl -sf "${DEMOUI_URL}/"          && echo " Demo UI       OK" || echo " Demo UI       FAIL"
```

### 20.2 Test Pixel Tracking

```bash
# Pixel endpoint should return a 1×1 GIF (HTTP 200)
curl -I "${FUNCTIONS_URL}/api/pixel?campaignId=test001&panelistToken=testtoken"
# Expected: HTTP/1.1 200 OK, Content-Type: image/gif
```

### 20.3 Test S2S Tracking

```bash
curl -X POST "${FUNCTIONS_URL}/api/s2s" \
  -H "Content-Type: application/json" \
  -d '{"campaignId":"test001","creativeId":"creative01","panelistToken":"testtoken","referrer":"https://example.com"}'
# Expected: HTTP 200 { "status": "accepted" }
```

### 20.4 Open the Swagger UIs

| Service      | URL                                                           |
| ------------ | ------------------------------------------------------------- |
| Panelist API | `https://${PREFIX}-panelist-${ENV}.azurewebsites.net/swagger` |
| Campaign API | `https://${PREFIX}-campaign-${ENV}.azurewebsites.net/swagger` |
| Survey API   | `https://${PREFIX}-survey-${ENV}.azurewebsites.net/swagger`   |
| Dashboard    | `https://${PREFIX}-dashboard-${ENV}.azurewebsites.net`        |
| Demo UI      | `https://${PREFIX}-demoui-${ENV}.azurewebsites.net`           |

---

## Phase 21 – Configure Monitoring and Alerts

### 21.1 Create an Action Group (notification target)

**Information needed:**

| Item              | Example value                    |
| ----------------- | -------------------------------- |
| Alert email       | `tech@theeditorialinstitute.com` |
| Action group name | `adimpact-dev-alerts`            |

```bash
export ALERT_EMAIL="tech@theeditorialinstitute.com"   # Replace with client's ops email

az monitor action-group create \
  --resource-group "$RG" \
  --name "adimpact-dev-alerts" \
  --short-name "adimpact" \
  --action email ops-team "$ALERT_EMAIL"
```

### 21.2 Create Metric Alerts

```bash
ACTION_GROUP_ID=$(az monitor action-group show \
  --resource-group "$RG" \
  --name "adimpact-dev-alerts" \
  --query id --output tsv)

FUNC_APP_ID=$(az functionapp show \
  --resource-group "$RG" \
  --name "$FUNC_APP" \
  --query id --output tsv)

# Alert: Function 5xx errors > 10 in 15 minutes
az monitor metrics alert create \
  --resource-group "$RG" \
  --name "fn-5xx-errors" \
  --scopes "$FUNC_APP_ID" \
  --condition "count Http5xx > 10" \
  --window-size 15m \
  --evaluation-frequency 5m \
  --severity 1 \
  --action "$ACTION_GROUP_ID" \
  --description "Function App returning too many 5xx errors"

# Alert: Function CPU > 80% for 15 minutes
az monitor metrics alert create \
  --resource-group "$RG" \
  --name "fn-high-cpu" \
  --scopes "$FUNC_APP_ID" \
  --condition "avg CpuPercentage > 80" \
  --window-size 15m \
  --evaluation-frequency 5m \
  --severity 2 \
  --action "$ACTION_GROUP_ID" \
  --description "Function App CPU above 80%"
```

### 21.3 Create a Budget Alert

**Information needed:**

| Item                 | Example value | Notes                   |
| -------------------- | ------------- | ----------------------- |
| Monthly budget (USD) | `200`         | Free trial credit limit |
| Alert thresholds     | `50`, `80`    | Percent of budget       |

```bash
az consumption budget create \
  --budget-name "adimpactos-monthly-budget" \
  --amount 200 \
  --time-grain Monthly \
  --start-date "$(date +%Y-%m-01)" \
  --end-date "$(date -d '+3 years' +%Y-%m-01)" \
  --resource-group "$RG" \
  --notification-threshold-percentage 50 \
  --contact-emails "$ALERT_EMAIL"
```

---

## Phase 22 – Cost Management

Estimated **monthly costs** for the dev environment (East US, serverless Cosmos DB):

| Service                       | Configuration               | Est. Monthly Cost  |
| ----------------------------- | --------------------------- | ------------------ |
| Azure Cosmos DB               | **Serverless** (pay-per-RU) | $1–$10             |
| Azure Event Hubs              | Standard, 1 TU              | $12                |
| Azure Storage                 | Standard LRS, < 10 GB       | $1                 |
| Azure Functions               | Consumption plan            | $0–$5              |
| App Service Plan              | **B1** × 1 instance         | $13                |
| Azure Container Registry      | **Basic**                   | $5                 |
| Application Insights          | Up to 5 GB/month free       | $0–$5              |
| Key Vault                     | Standard                    | $1                 |
| Azure Monitor (Log Analytics) | 5 GB/month free             | $0–$5              |
| **Total estimate**            |                             | **~$35–$55/month** |

> With the $200 free Azure credit, this configuration covers approximately **3.5–5.7 months** of dev usage.

### Cost-Saving Tips

- Use `az consumption usage list` to see a breakdown by service.
- Cosmos DB Serverless charges per RU consumed — no idle cost; ideal for dev/test workloads.
- Enable **lifecycle management** on the storage account to move old blobs to cool/archive tiers.
- Use **Azure Advisor** (Portal → Advisor) for automated cost recommendations.
- When ready for production, consider migrating Cosmos DB to provisioned throughput with autoscale.

---

## Phase 23 – Optional Extras

### 23.1 Custom Domain and TLS Certificate

**Information needed:**

| Item                | Example value                                            |
| ------------------- | -------------------------------------------------------- |
| Custom domain       | `adimpactos.example.com`                                 |
| DNS provider access | Access to DNS records for `example.com`                  |
| TLS certificate     | Managed (free) via Azure App Service, or upload your own |

```bash
# Add custom domain to the Dashboard web app
az webapp config hostname add \
  --resource-group "$RG" \
  --webapp-name "${PREFIX}-dashboard-${ENV}" \
  --hostname "adimpactos.example.com"

# Create a free managed certificate
az webapp config ssl create \
  --resource-group "$RG" \
  --name "${PREFIX}-dashboard-${ENV}" \
  --hostname "adimpactos.example.com"

# Bind the certificate
CERT_THUMBPRINT=$(az webapp config ssl show \
  --resource-group "$RG" \
  --name "${PREFIX}-dashboard-${ENV}" \
  --query thumbprint --output tsv)

az webapp config ssl bind \
  --resource-group "$RG" \
  --name "${PREFIX}-dashboard-${ENV}" \
  --certificate-thumbprint "$CERT_THUMBPRINT" \
  --ssl-type SNI
```

### 23.2 Azure AD B2C Authentication

Required before enabling authentication on the APIs and Dashboard.

**Information needed:**

| Item                     | Details                              |
| ------------------------ | ------------------------------------ |
| Azure AD B2C tenant name | e.g. `adimpactosb2c.onmicrosoft.com` |
| Application (client) ID  | Created in the B2C app registration  |
| Sign-up/sign-in policy   | e.g. `B2C_1_susi`                    |

Follow the [official B2C quickstart](https://learn.microsoft.com/azure/active-directory-b2c/tutorial-create-tenant)
then update `appsettings.json` / App Service settings with `AzureAdB2C__*` values.

### 23.3 Azure Front Door (WAF + CDN)

Azure Front Door puts a globally distributed WAF and CDN in front of the pixel tracker endpoint
to absorb traffic spikes and protect against DDoS attacks.

```bash
az afd profile create \
  --profile-name "${PREFIX}-fd-${ENV}" \
  --resource-group "$RG" \
  --sku Standard_AzureFrontDoor
```

Refer to the [Azure Front Door documentation](https://learn.microsoft.com/azure/frontdoor/) for
endpoint and origin group configuration.

### 23.4 CI/CD Pipeline

Set up a GitHub Actions workflow (or Azure DevOps pipeline) to automatically build, push, and
deploy on every push to `main`.

**Information needed:**

| Secret name         | Value                                                          |
| ------------------- | -------------------------------------------------------------- |
| `AZURE_CREDENTIALS` | Service principal JSON (`az ad sp create-for-rbac --sdk-auth`) |
| `ACR_LOGIN_SERVER`  | `$ACR_LOGIN_SERVER`                                            |
| `ACR_USERNAME`      | ACR admin username                                             |
| `ACR_PASSWORD`      | ACR admin password                                             |
| `FUNC_APP_NAME`     | `$FUNC_APP`                                                    |

A starter workflow file lives at `.github/workflows/deploy.yml` (create it if not present).

---

## Quick-Reference: All Resource Names

After following this guide you will have created the following Azure resources (using example
prefix `adimpact`, environment `dev`, region `eastus`):

| Resource                | Name                                                      | Type                     |
| ----------------------- | --------------------------------------------------------- | ------------------------ |
| Tenant (Root Group)     | `84d2a2d3-8846-412a-af03-932ff0b721c8`                    | Management Group         |
| Subscription            | `AdImpactOs-Dev` (`91a6db8a-cc0c-4ae7-93e1-bd9b02291dd9`) | Subscription             |
| Resource Group          | `adimpact-dev-rg`                                         | Resource Group           |
| Container Registry      | `adimpactdevacr`                                          | Azure Container Registry |
| Cosmos DB Account       | `adimpact-cosmos-dev`                                     | Cosmos DB                |
| Cosmos DB Database      | `AdImpactOsDB`                                            | Database                 |
| Event Hubs Namespace    | `adimpact-eh-dev`                                         | Event Hubs               |
| Event Hub               | `ad-impressions`                                          | Event Hub                |
| Storage Account         | `adimpactdevsa`                                           | Storage Account          |
| Key Vault               | `adimpact-kv-dev`                                         | Key Vault                |
| Log Analytics Workspace | `adimpact-law-dev`                                        | Log Analytics            |
| Application Insights    | `adimpact-ai-dev`                                         | Application Insights     |
| App Service Plan        | `adimpact-plan-dev`                                       | App Service Plan         |
| Function App            | `adimpact-fn-dev`                                         | Azure Functions          |
| Panelist API            | `adimpact-panelist-dev`                                   | App Service              |
| Campaign API            | `adimpact-campaign-dev`                                   | App Service              |
| Survey API              | `adimpact-survey-dev`                                     | App Service              |
| Dashboard               | `adimpact-dashboard-dev`                                  | App Service              |
| Demo UI                 | `adimpact-demoui-dev`                                     | App Service              |
| Event Consumer          | `adimpact-consumer-dev`                                   | App Service              |

---

## Troubleshooting

### App Service returns HTTP 503 immediately after deployment

- The container may still be pulling. Wait 2–5 minutes and try again.
- Check logs: `az webapp log tail --name <app-name> --resource-group "$RG"`

### Function App fails to start (Key Vault reference errors)

- Verify the Managed Identity has been granted Key Vault access (Phase 14).
- Check the secret name matches exactly (`CosmosDbKey`, not `CosmosDbKey1`).

### Cosmos DB returning 403 Forbidden

- Verify `CosmosDb__Endpoint` and `CosmosDb__Key` are set correctly.
- Check that the IP of your App Service is not blocked by a Cosmos DB firewall rule.

### Event Consumer not processing events

- Check App Service logs for Event Hub connection errors.
- Verify the consumer group `event-consumer` exists (Phase 7.3).
- Confirm `BlobStorage__ConnectionString` is set and the checkpoint container exists.

### Docker build fails

- Run `az acr login --name "$ACR_NAME"` to refresh credentials.
- Ensure Docker Desktop is running locally.

### "Name already exists" errors during resource creation

- A resource with that name already exists globally. Use a more unique prefix (e.g. add a
  3-digit number: `adimpact42`).

---

_Last updated: 2026-03-07 | Guide version: 1.0 | Maintained by: Operations Team_
