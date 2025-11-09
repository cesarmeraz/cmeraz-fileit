@description('The Azure region for the deployment')
param location string

@description('A unique value, like domain')
param stem string

@description('The name of the parent resource group')
param resourceGroupName string

@description('The resource name')
param resourceName string

@description('Tag value for deployment')
param deploymentName string

targetScope = 'subscription'

resource rg_bus 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  location: location
  name: resourceGroupName
  properties: {}
  tags: {
    stem: stem
    module: 'bus_module'
    deployment: deploymentName
  }
}

module bus_module 'bus_module.bicep' = {
  name: resourceName
  scope: resourceGroup(rg_bus.name)
  params: {
    resourceName: resourceName
    location: location
    tagsByResource: {
      stem: stem
      module: 'bus_module' 
      deployment: deploymentName
    }
  }
}
