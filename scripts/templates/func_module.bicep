param location string = resourceGroup().location
param resourceName string
@minLength(3)
@maxLength(24)
@description('The resource storage account name')
param resourceStorageAccountName string

@description('The shared storage account name')
param storageAccountName string

@description('The shared storage account resource group name')
param storageAccountGroupName string

@description('Application Insights instance name')
param appInsightsName string

@description('Application Insights resource group name')
param appInsightsGroupName string

@description('Service Bus instance name')
param busName string

@description('Service Bus resource group name')
param busGroupName string

@description('SQL server name')
param sqlServerName string

@description('Database instance name')
param databaseName string

@description('Database resource group name')
param databaseGroupName string

@description('Tags applied to the resource')
param tagsByResource object

@description('User assigned managed identity id')
param userIdentityId string 

@description('The client ID of the user assigned managed identity')
param userClientId string 

// Existing Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
  scope: resourceGroup(appInsightsGroupName)
}

// Existing Service Bus
resource serviceBus 'Microsoft.ServiceBus/namespaces@2021-11-01' existing = {
  name: busName
  scope: resourceGroup(busGroupName)
}


// Get reference to existing SQL Server
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' existing = {
  name: sqlServerName
  scope: resourceGroup(databaseGroupName)
}

resource database 'Microsoft.Sql/servers@2021-02-01-preview' existing = {
  name: databaseName
  scope: resourceGroup(databaseGroupName)
}

// Shared Storage Account
resource sharedStorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
  scope: resourceGroup(storageAccountGroupName)
}

// Construct the connection strings

var sqlConnectionString = 'Server=${sqlServer.properties.fullyQualifiedDomainName},1433; Authentication=Active Directory Managed Identity; Database=${database.name};User Id=${userClientId}'
// Storage Account (Required for hosting and deployment)
resource functionStorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: resourceStorageAccountName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    defaultToOAuthAuthentication: true
  }
}

// Create blob service and container used for function app package deployments
resource functionBlobService 'Microsoft.Storage/storageAccounts/blobServices@2021-09-01' = {
  parent: functionStorageAccount
  name: 'default'
  properties: {}
}

resource functionStorageContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-09-01' = {
  parent: functionBlobService
  name: 'app-package'
  properties: {}
}

// App Service Plan (Flex Consumption SKU)
resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${resourceName}-plan'
  location: location
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  kind: 'functionapp'
  properties: {
    reserved: true // Required for Linux
  }
}

// Function App (Flex Consumption Configuration)
resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: resourceName
  location: location
  kind: 'functionapp,linux'
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${userIdentityId}': {} } }
  tags: tagsByResource
  properties: {
    serverFarmId: hostingPlan.id
    
    functionAppConfig: {
      runtime: {
        name: 'dotnet-isolated'
        version: '8.0' // Use '9.0' if targeting .NET 9
      }
      scaleAndConcurrency: {
        maximumInstanceCount: 100
        instanceMemoryMB: 2048
      }
      deployment: {
        storage: {
          type: 'blobContainer'
          // Flex apps can deploy from a specific blob container
          value: '${functionStorageAccount.properties.primaryEndpoints.blob}app-package'
          authentication: {
            type: 'UserAssignedIdentity'
            userAssignedIdentityResourceId: userIdentityId
          }
        }
      }
    }
    siteConfig: {
      appSettings: [
        {
          name: 'FileItDbConnection'
          value: sqlConnectionString
        }
        {
          name: 'AzureWebJobsStorage__accountName'
          value: functionStorageAccount.name
        }
        // Use Identity-based connections instead of connection strings
        {
          name: 'AzureWebJobsStorage__credential'
          value: 'managedidentity'
        }
        {
          name:'AzureWebJobsStorage__clientId'
          value: userClientId
        }
        // Application Insights configuration
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~1'
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: userClientId
        }
        {
          name: 'FileItServiceBus__fullyQualifiedNamespace'
          value: '${serviceBus.name}.servicebus.windows.net'
        }
        {
          name: 'FileItServiceBus__credential'
          value: 'managedidentity'
        }
        {
          name: 'FileItServiceBus__clientId'
          value: userClientId
        }
        {
          name: 'FileItStorage__serviceUri'
          value: sharedStorageAccount.properties.primaryEndpoints.blob
        }
        {
          name: 'FileItStorage__clientId'
          value: userClientId
        }
        {
          name: 'FileItStorage__credential'
          value: 'managedidentity'
        }
        {
          name: 'EveryMinuteSchedule'
          value: '0 */1 * * * *'
        }
      ]
    }
  }
}
