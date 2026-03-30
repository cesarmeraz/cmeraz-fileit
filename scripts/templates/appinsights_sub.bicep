@description('The Azure region for the deployment')
param location string

@description('A unique value, like domain')
param stem string

@description('The name of the parent resource group')
param resourceGroupName string

@description('The resource name')
param resourceName string

@description('The workspace name')
param workspaceName string

@description('Tag value for deployment')
param deploymentName string


targetScope = 'subscription'

resource rg_appinsights 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  location: location
  name: resourceGroupName
  tags: {
    stem: stem
    module: 'appinsights_module'
    deployment: deploymentName
  }
}

module appinsights_module 'appinsights_module.bicep' = {
  name: resourceName
  scope: resourceGroup(rg_appinsights.name)
  params: {
    appInsightsName: resourceName
    workspaceName: workspaceName
    tagsByResource: {
      stem: stem
      module: 'appinsights_module'
      deployment: deploymentName
    }
  }
}
