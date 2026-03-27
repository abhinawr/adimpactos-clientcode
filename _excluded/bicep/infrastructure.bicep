@description('Location for all resources')
param location string = resourceGroup().location

@description('Project prefix for resource naming')
param projectPrefix string = 'adimpact'

@description('Environment name (dev, staging, prod)')
param environment string = 'prod'

@description('Event Hub namespace name')
param eventHubNamespace string = '${projectPrefix}-${environment}-eh-ns'

@description('Event Hub name for ad impressions')
param eventHubName string = 'ad-impressions'

@description('Function App name')
param functionAppName string = '${projectPrefix}-${environment}-func'

@description('Key Vault name')
param keyVaultName string = '${projectPrefix}${environment}kv'

@description('Storage account name')
param storageAccountName string = '${replace(projectPrefix, '-', '')}${environment}st'

// Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
  }
}

// Event Hubs Namespace
resource eventHubNamespaceResource 'Microsoft.EventHub/namespaces@2023-01-01-preview' = {
  name: eventHubNamespace
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 1
  }
  properties: {
    minimumTlsVersion: '1.2'
  }
}

// Event Hub
resource eventHub 'Microsoft.EventHub/namespaces/eventhubs@2023-01-01-preview' = {
  parent: eventHubNamespaceResource
  name: eventHubName
  properties: {
    messageRetentionInDays: 7
    partitionCount: 4 // Supports high throughput
  }
}

// Event Hub Shared Access Policy
resource eventHubPolicy 'Microsoft.EventHub/namespaces/authorizationRules@2023-01-01-preview' = {
  parent: eventHubNamespaceResource
  name: 'RootManageSharedAccessKey'
  properties: {
    rights: [
      'Listen'
      'Manage'
      'Send'
    ]
  }
}

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: []
    enableRbacAuthorization: true
  }
}

// Key Vault Reader Role Definition
resource keyVaultSecretsUser 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: '4633458b-17de-408a-b874-0445c86b69e6' // Key Vault Secrets User
}

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: '${projectPrefix}-${environment}-plan'
  location: location
  kind: 'elastic'
  sku: {
    name: 'EP1'
    tier: 'ElasticPremium'
    capacity: 1
  }
  properties: {
    reserved: true // For Linux
  }
}

// Function App
resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'EventHubConnection'
          value: eventHubPolicy.listKeys().primaryConnectionString
        }
        {
          name: 'KeyVaultUrl'
          value: keyVault.properties.vaultUri
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
      ]
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      http20Enabled: true
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
    }
    httpsOnly: true
  }
}

// Role Assignment: Function App -> Key Vault Secrets User
resource functionAppKeyVaultAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, functionApp.name, keyVaultSecretsUser.id)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUser.id)
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Application Insights
resource appInsightsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${projectPrefix}-${environment}-law'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${projectPrefix}-${environment}-ai'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    RetentionInDays: 30
    WorkspaceResourceId: appInsightsWorkspace.id
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Outputs
output functionAppName string = functionApp.name
output functionAppUrl string = 'https://${functionApp.properties.defaultHostName}'
output eventHubNamespace string = eventHubNamespaceResource.name
output keyVaultName string = keyVault.name
output applicationInsightsName string = applicationInsights.name
