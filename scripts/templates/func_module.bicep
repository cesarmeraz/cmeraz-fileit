param location string = resourceGroup().location
param resourceName string = 'fn-flex-${uniqueString(resourceGroup().id)}'
param storageAccountName string = 'stflex${uniqueString(resourceGroup().id)}'

@description('Tags applied to the resource')
param tagsByResource object

// 1. Storage Account (Required for hosting and deployment)
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    defaultToOAuthAuthentication: true
  }
}

// 2. App Service Plan (Flex Consumption SKU)
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

// 3. Function App (Flex Consumption Configuration)
resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: resourceName
  location: location
  kind: 'functionapp,linux'
  identity: { type: 'SystemAssigned' }
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
          value: '${storageAccount.properties.primaryEndpoints.blob}app-package'
          authentication: {
            type: 'SystemAssignedIdentity'
          }
        }
      }
    }
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storageAccount.name
        }
        // Use Identity-based connections instead of connection strings
        {
          name: 'AzureWebJobsStorage__credential'
          value: 'managedidentity'
        }
      ]
    }
  }
}
