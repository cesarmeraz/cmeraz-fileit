@description('The Azure region for the deployment')
param location string = 'eastus2'

@description('A unique value, like domain')
param stem string

@description('The name of the resource group for the storage account')
param storageAccountResourceGroupName string

@description('The name of the storage account')
param storageAccountName string

@description('The name of the container')
param containerName string

targetScope = 'subscription'

resource rg_function 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  location: location
  name: storageAccountResourceGroupName
  properties: {}
  tags: {
    stem: stem
    module: 'function_module'
  }
}

module function_module 'func_module.bicep' = {
  name: 'function_module'
  scope: resourceGroup(rg_function.name)
  params: {
    storageAccountName: storageAccountName
    containerName: containerName
  }
}
