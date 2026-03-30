@description('Function App name')
param functionAppName string

@description('Function App resource group name')
param functionRGName string

@description('Storage account name')
param storageAccountName string

param userIdentityPrincipalId string

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
  scope: resourceGroup()
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' existing = {
  name: functionAppName
  scope: resourceGroup(functionRGName)
}

var blobDataContributor = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
var tableDataContributor = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
var queueDataContributor = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')

resource blobRoleAssignment 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(storageAccount.id, functionApp.id, blobDataContributor)
  scope: storageAccount
  properties: {
    principalId: userIdentityPrincipalId
    roleDefinitionId: blobDataContributor
    principalType: 'ServicePrincipal'
  }
}
resource tableRoleAssignment 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(storageAccount.id, functionApp.id, tableDataContributor)
  scope: storageAccount
  properties: {
    principalId: userIdentityPrincipalId
    roleDefinitionId: tableDataContributor
    principalType: 'ServicePrincipal'
  }
}
resource queueRoleAssignment 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(storageAccount.id, functionApp.id, queueDataContributor)
  scope: storageAccount
  properties: {
    principalId: userIdentityPrincipalId
    roleDefinitionId: queueDataContributor
    principalType: 'ServicePrincipal'
  }
}
