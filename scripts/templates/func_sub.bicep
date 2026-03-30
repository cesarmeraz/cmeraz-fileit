@description('The Azure region for the deployment')
param location string

@description('A unique value, like domain')
param stem string

@description('The name of the parent resource group')
param resourceGroupName string

@description('The resource name')
param resourceName string

@description('The resource storage account name')
param resourceStorageAccountName string

@description('The shared storage account name')
param storageAccountName string

@description('The shared storage account resource group name')
param storageAccountGroupName string

@description('Tag value for deployment')
param deploymentName string


@description('Application Insights instance name')
param appInsightsName string

@description('Application Insights resource group name')
param appInsightsGroupName string

@description('Service Bus instance name')
param busName string

@description('Service Bus resource group name')
param busGroupName string

@description('SQL Server name')
param sqlServerName string

@description('Database instance name')
param databaseName string

@description('Database resource group name')
param databaseGroupName string

@description('User defined managed identity name')
param userName string

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

module function_user 'user_module.bicep' = {
  name: 'function_user'
  scope: resourceGroup(rg_function.name)
  params: {
    identityName: userName
  }
}

var userIdentityPrincipalId = function_user.outputs.principalId
var userIdentityId = function_user.outputs.identityId
var userClientId = function_user.outputs.clientId

module function_module 'func_module.bicep' = {
  name: 'function_module'
  scope: resourceGroup(rg_function.name)
  params: {
    resourceName: resourceName
    resourceStorageAccountName: resourceStorageAccountName
    appInsightsName: appInsightsName
    appInsightsGroupName: appInsightsGroupName
    busName: busName
    busGroupName: busGroupName
    databaseName: databaseName
    databaseGroupName: databaseGroupName
    storageAccountName: storageAccountName
    storageAccountGroupName: storageAccountGroupName
    sqlServerName: sqlServerName
    userIdentityId: userIdentityId
    userClientId: userClientId
    tagsByResource: {
      stem: stem
      module: 'function_module' 
      deployment: deploymentName
    }
  }
}

// Ensure RBAC modules run after function_module completes
module deploy_rbac 'func_storage_rbacs.bicep' = {
  name: 'deploy_rbac'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    function_module
  ]
  params: {
    functionAppName: resourceName
    functionRGName: resourceGroupName
    storageAccountName: resourceStorageAccountName
    userIdentityPrincipalId: userIdentityPrincipalId
  }
}

// Ensure RBAC modules run after function_module completes
module blob_rbac 'func_storage_rbacs.bicep' = {
  name: 'blob_rbac'
  scope: resourceGroup(storageAccountGroupName)
  dependsOn: [
    function_module
  ]
  params: {
    functionAppName: resourceName
    functionRGName: resourceGroupName
    storageAccountName: storageAccountName
    userIdentityPrincipalId: userIdentityPrincipalId
  }
}

module bus_rbac 'func_bus_rbacs.bicep' = {
  name: 'bus_rbac'
  scope: resourceGroup(busGroupName)
  dependsOn: [
    function_module
  ]
  params: {
    functionAppName: resourceName
    serviceBusName: busName
    principalId: userIdentityPrincipalId
    functionRGName: resourceGroupName
  }
}
