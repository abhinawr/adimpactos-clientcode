# Terraform Configuration for Azure Ad Tracking Infrastructure
# Provider: Azure (azurerm)
# Target: Microsoft Azure resources

terraform {
  required_version = ">= 1.0"
  
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
  
  backend "azurerm" {
    resource_group_name  = "tfstate-rg"
    storage_account_name = "tfstatestorage"
    container_name       = "tfstate"
    key                  = "adtracking.terraform.tfstate"
  }
}

provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy = false
    }
  }
}

# Variables
variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "prod"
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "eastus"
}

variable "project_prefix" {
  description = "Project prefix for resource naming"
  type        = string
  default     = "adtrack"
}

variable "alert_email" {
  description = "Email address for monitoring alerts"
  type        = string
  default     = "ops@example.com"
}

variable "monthly_budget" {
  description = "Monthly budget in USD for cost alerts"
  type        = number
  default     = 1000
}

# Locals for naming conventions
locals {
  resource_name = "${var.project_prefix}-${var.environment}"
  tags = {
    Project     = "AdTracking"
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "${local.resource_name}-rg"
  location = var.location
  tags     = local.tags
}

# Azure Front Door
resource "azurerm_cdn_frontdoor_profile" "main" {
  name                = "${local.resource_name}-afd"
  resource_group_name = azurerm_resource_group.main.name
  sku_name            = "Standard_AzureFrontDoor"
  tags                = local.tags
}

resource "azurerm_cdn_frontdoor_endpoint" "main" {
  name                     = "${local.resource_name}-endpoint"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.main.id
  tags                     = local.tags
}

# Storage Account (Azure Data Lake Gen2)
resource "azurerm_storage_account" "datalake" {
  name                     = replace("${var.project_prefix}${var.environment}dl", "-", "")
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "GRS"
  account_kind             = "StorageV2"
  is_hns_enabled           = true  # Hierarchical namespace for Data Lake Gen2
  
  tags = local.tags
}

# Data Lake containers
resource "azurerm_storage_data_lake_gen2_filesystem" "staging" {
  name               = "staging"
  storage_account_id = azurerm_storage_account.datalake.id
}

resource "azurerm_storage_data_lake_gen2_filesystem" "raw" {
  name               = "raw"
  storage_account_id = azurerm_storage_account.datalake.id
}

# Event Hubs Namespace
resource "azurerm_eventhub_namespace" "main" {
  name                = "${local.resource_name}-ehns"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "Standard"
  capacity            = 2
  
  tags = local.tags
}

# Event Hub for ad impressions
resource "azurerm_eventhub" "ad_impressions" {
  name                = "ad-impressions"
  namespace_name      = azurerm_eventhub_namespace.main.name
  resource_group_name = azurerm_resource_group.main.name
  partition_count     = 4
  message_retention   = 7
}

# Event Hub Authorization Rule
resource "azurerm_eventhub_authorization_rule" "send_listen" {
  name                = "SendListenRule"
  namespace_name      = azurerm_eventhub_namespace.main.name
  eventhub_name       = azurerm_eventhub.ad_impressions.name
  resource_group_name = azurerm_resource_group.main.name
  listen              = true
  send                = true
  manage              = false
}

# Cosmos DB Account
resource "azurerm_cosmosdb_account" "main" {
  name                = "${local.resource_name}-cosmos"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"
  
  consistency_policy {
    consistency_level = "Session"
  }
  
  geo_location {
    location          = azurerm_resource_group.main.location
    failover_priority = 0
  }
  
  tags = local.tags
}

# Cosmos DB SQL Database
resource "azurerm_cosmosdb_sql_database" "panelists" {
  name                = "AdTrackingDB"
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  throughput          = 400
}

# Cosmos DB Container
resource "azurerm_cosmosdb_sql_container" "panelists" {
  name                  = "Panelists"
  resource_group_name   = azurerm_resource_group.main.name
  account_name          = azurerm_cosmosdb_account.main.name
  database_name         = azurerm_cosmosdb_sql_database.panelists.name
  partition_key_path    = "/id"
  partition_key_version = 1
  throughput            = 400
  
  indexing_policy {
    indexing_mode = "consistent"
    
    included_path {
      path = "/*"
    }
    
    excluded_path {
      path = "/\"_etag\"/?"
    }
  }
}

# Key Vault
resource "azurerm_key_vault" "main" {
  name                = "${var.project_prefix}${var.environment}kv"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"
  
  enable_rbac_authorization = true
  purge_protection_enabled  = true
  
  tags = local.tags
}

# Key Vault Secrets
resource "azurerm_key_vault_secret" "cosmos_connection_string" {
  name         = "CosmosDbConnectionString"
  value        = azurerm_cosmosdb_account.main.primary_sql_connection_string
  key_vault_id = azurerm_key_vault.main.id
  
  depends_on = [azurerm_role_assignment.terraform_kv_admin]
}

resource "azurerm_key_vault_secret" "eventhub_connection_string" {
  name         = "EventHubConnectionString"
  value        = azurerm_eventhub_authorization_rule.send_listen.primary_connection_string
  key_vault_id = azurerm_key_vault.main.id
  
  depends_on = [azurerm_role_assignment.terraform_kv_admin]
}

# App Service Plan for Functions
resource "azurerm_service_plan" "functions" {
  name                = "${local.resource_name}-plan"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  os_type             = "Linux"
  sku_name            = "EP1"
  
  tags = local.tags
}

# Function App Storage
resource "azurerm_storage_account" "functions" {
  name                     = replace("${var.project_prefix}${var.environment}func", "-", "")
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  
  tags = local.tags
}

# Function App
resource "azurerm_linux_function_app" "main" {
  name                = "${local.resource_name}-func"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  service_plan_id     = azurerm_service_plan.functions.id
  
  storage_account_name       = azurerm_storage_account.functions.name
  storage_account_access_key = azurerm_storage_account.functions.primary_access_key
  
  site_config {
    application_stack {
      dotnet_version              = "8.0"
      use_dotnet_isolated_runtime = true
    }
    
    application_insights_connection_string = azurerm_application_insights.main.connection_string
  }
  
  app_settings = {
    "EventHubConnection"        = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.eventhub_connection_string.id})"
    "CosmosDbConnectionString"  = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.cosmos_connection_string.id})"
    "FUNCTIONS_WORKER_RUNTIME"  = "dotnet-isolated"
  }
  
  identity {
    type = "SystemAssigned"
  }
  
  tags = local.tags
}

# Azure Synapse Workspace
resource "azurerm_synapse_workspace" "main" {
  name                                 = "${local.resource_name}-synapse"
  resource_group_name                  = azurerm_resource_group.main.name
  location                             = azurerm_resource_group.main.location
  storage_data_lake_gen2_filesystem_id = azurerm_storage_data_lake_gen2_filesystem.staging.id
  sql_administrator_login              = "sqladmin"
  sql_administrator_login_password     = random_password.synapse_admin.result
  
  identity {
    type = "SystemAssigned"
  }
  
  tags = local.tags
}

# Synapse Spark Pool
resource "azurerm_synapse_spark_pool" "main" {
  name                 = "${local.resource_name}spark"
  synapse_workspace_id = azurerm_synapse_workspace.main.id
  node_size_family     = "MemoryOptimized"
  node_size            = "Small"
  
  auto_scale {
    max_node_count = 10
    min_node_count = 3
  }
  
  auto_pause {
    delay_in_minutes = 15
  }
  
  tags = local.tags
}

# Application Insights
resource "azurerm_log_analytics_workspace" "main" {
  name                = "${local.resource_name}-law"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  
  tags = local.tags
}

resource "azurerm_application_insights" "main" {
  name                = "${local.resource_name}-ai"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"
  
  tags = local.tags
}

# RBAC Assignments
data "azurerm_client_config" "current" {}

resource "azurerm_role_assignment" "terraform_kv_admin" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Administrator"
  principal_id         = data.azurerm_client_config.current.object_id
}

resource "azurerm_role_assignment" "function_kv_secrets_user" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_linux_function_app.main.identity[0].principal_id
}

resource "azurerm_role_assignment" "synapse_storage_contributor" {
  scope                = azurerm_storage_account.datalake.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_synapse_workspace.main.identity[0].principal_id
}

# Random password for Synapse
resource "random_password" "synapse_admin" {
  length  = 16
  special = true
}

# Action Group for Alerts
resource "azurerm_monitor_action_group" "main" {
  name                = "${local.resource_name}-action-group"
  resource_group_name = azurerm_resource_group.main.name
  short_name          = "adtrack"
  
  email_receiver {
    name          = "ops-team"
    email_address = var.alert_email
  }
  
  tags = local.tags
}

# Alert: High Error Rate in Function App
resource "azurerm_monitor_metric_alert" "function_errors" {
  name                = "${local.resource_name}-func-errors"
  resource_group_name = azurerm_resource_group.main.name
  scopes              = [azurerm_linux_function_app.main.id]
  description         = "Alert when function error rate exceeds threshold"
  severity            = 1
  frequency           = "PT5M"
  window_size         = "PT15M"
  
  criteria {
    metric_namespace = "Microsoft.Web/sites"
    metric_name      = "Http5xx"
    aggregation      = "Total"
    operator         = "GreaterThan"
    threshold        = 10
  }
  
  action {
    action_group_id = azurerm_monitor_action_group.main.id
  }
  
  tags = local.tags
}

# Alert: Function App CPU Usage
resource "azurerm_monitor_metric_alert" "function_cpu" {
  name                = "${local.resource_name}-func-cpu"
  resource_group_name = azurerm_resource_group.main.name
  scopes              = [azurerm_service_plan.functions.id]
  description         = "Alert when CPU usage is consistently high"
  severity            = 2
  frequency           = "PT5M"
  window_size         = "PT15M"
  
  criteria {
    metric_namespace = "Microsoft.Web/serverfarms"
    metric_name      = "CpuPercentage"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 80
  }
  
  action {
    action_group_id = azurerm_monitor_action_group.main.id
  }
  
  tags = local.tags
}

# Alert: Cosmos DB High RU Consumption
resource "azurerm_monitor_metric_alert" "cosmos_ru" {
  name                = "${local.resource_name}-cosmos-ru"
  resource_group_name = azurerm_resource_group.main.name
  scopes              = [azurerm_cosmosdb_account.main.id]
  description         = "Alert when Cosmos DB RU consumption is high"
  severity            = 2
  frequency           = "PT5M"
  window_size         = "PT15M"
  
  criteria {
    metric_namespace = "Microsoft.DocumentDB/databaseAccounts"
    metric_name      = "TotalRequestUnits"
    aggregation      = "Total"
    operator         = "GreaterThan"
    threshold        = 10000
  }
  
  action {
    action_group_id = azurerm_monitor_action_group.main.id
  }
  
  tags = local.tags
}

# Alert: Event Hub Throttling
resource "azurerm_monitor_metric_alert" "eventhub_throttling" {
  name                = "${local.resource_name}-eh-throttle"
  resource_group_name = azurerm_resource_group.main.name
  scopes              = [azurerm_eventhub_namespace.main.id]
  description         = "Alert when Event Hub throttling occurs"
  severity            = 1
  frequency           = "PT5M"
  window_size         = "PT15M"
  
  criteria {
    metric_namespace = "Microsoft.EventHub/namespaces"
    metric_name      = "ThrottledRequests"
    aggregation      = "Total"
    operator         = "GreaterThan"
    threshold        = 10
  }
  
  action {
    action_group_id = azurerm_monitor_action_group.main.id
  }
  
  tags = local.tags
}

# Alert: Data Lake Storage Capacity
resource "azurerm_monitor_metric_alert" "storage_capacity" {
  name                = "${local.resource_name}-storage-capacity"
  resource_group_name = azurerm_resource_group.main.name
  scopes              = [azurerm_storage_account.datalake.id]
  description         = "Alert when storage capacity exceeds 80%"
  severity            = 2
  frequency           = "PT1H"
  window_size         = "PT6H"
  
  criteria {
    metric_namespace = "Microsoft.Storage/storageAccounts"
    metric_name      = "UsedCapacity"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 4398046511104  # 4TB
  }
  
  action {
    action_group_id = azurerm_monitor_action_group.main.id
  }
  
  tags = local.tags
}

# Budget Alert
resource "azurerm_consumption_budget_resource_group" "main" {
  name              = "${local.resource_name}-budget"
  resource_group_id = azurerm_resource_group.main.id
  
  amount     = var.monthly_budget
  time_grain = "Monthly"
  
  time_period {
    start_date = formatdate("YYYY-MM-01'T'00:00:00'Z'", timestamp())
  }
  
  notification {
    enabled   = true
    threshold = 80
    operator  = "GreaterThan"
    
    contact_emails = [var.alert_email]
  }
  
  notification {
    enabled   = true
    threshold = 100
    operator  = "GreaterThan"
    
    contact_emails = [var.alert_email]
  }
}

# Data Lake Lifecycle Management
resource "azurerm_storage_management_policy" "datalake" {
  storage_account_id = azurerm_storage_account.datalake.id
  
  rule {
    name    = "delete-old-raw-data"
    enabled = true
    
    filters {
      prefix_match = ["raw/"]
      blob_types   = ["blockBlob"]
    }
    
    actions {
      base_blob {
        delete_after_days_since_modification_greater_than = 365
      }
    }
  }
  
  rule {
    name    = "tier-to-cool"
    enabled = true
    
    filters {
      prefix_match = ["staging/"]
      blob_types   = ["blockBlob"]
    }
    
    actions {
      base_blob {
        tier_to_cool_after_days_since_modification_greater_than = 90
        delete_after_days_since_modification_greater_than       = 730
      }
    }
  }
}

# Outputs
output "function_app_name" {
  value = azurerm_linux_function_app.main.name
}

output "function_app_url" {
  value = azurerm_linux_function_app.main.default_hostname
}

output "cosmos_db_endpoint" {
  value = azurerm_cosmosdb_account.main.endpoint
}

output "synapse_workspace_url" {
  value = azurerm_synapse_workspace.main.connectivity_endpoints["dev"]
}

output "key_vault_uri" {
  value = azurerm_key_vault.main.vault_uri
}

output "eventhub_namespace" {
  value = azurerm_eventhub_namespace.main.name
}

output "action_group_id" {
  value       = azurerm_monitor_action_group.main.id
  description = "Action Group ID for monitoring alerts"
}
