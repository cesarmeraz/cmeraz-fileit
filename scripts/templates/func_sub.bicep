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

@description('The name of the container')
param containerName string

@description('The storage account resource name for the function app')
param storageAccountName string

targetScope = 'subscription'

resource rg_function 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  location: location
  name: resourceGroupName
  properties: {}
  tags: {
    stem: stem
    module: 'function_module'
    deployment: deploymentName
  }
}

module function_module 'func_module.bicep' = {
  name: 'function_module'
  scope: resourceGroup(rg_function.name)
  params: {
    resourceName: resourceName
    storageAccountName: storageAccountName
    containerName: containerName
    location: location
    tagsByResource: {
      stem: stem
      module: 'bus_module' 
      deployment: deploymentName
    }
  }
}
