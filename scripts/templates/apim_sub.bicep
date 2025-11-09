@description('The Azure region for the deployment')
param location string = 'eastus2'

@description('A unique value, like domain')
param stem string

@description('The name of the parent resource group')
param resourceGroupName string

@description('The resource name')
param resourceName string

@description('Tag value for deployment')
param deploymentName string

targetScope = 'subscription'

resource rg_apim 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  location: location
  name: resourceGroupName
  properties: {}
  tags: {
    stem: stem
    module: 'apim_module'
    deployment: deploymentName
  }
}

module apim_module 'apim_module.bicep' = {
  scope: rg_apim
  params: {
    resourceName: resourceName
    location: location
    tagsByResource: {
      stem: stem
      module: 'apim_module'
      deployment: deploymentName
    }
    zones: []
    tier: 'Consumption'
    capacity: 1
    adminEmail: 'cesar.meraz@gmail.com'
    organizationName: 'Cesar Meraz'
    customProperties: {}
    identity: {
      type: 'SystemAssigned'
    }
  }
}
