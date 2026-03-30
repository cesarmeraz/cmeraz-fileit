@description('The function app name')
param functionAppName string

@description('The function app resource group name')
param functionRGName string

@description('The service bus name')
param serviceBusName string

@description('The principal ID of the user assigned managed identity to assign RBAC roles to')
param principalId string


// Reference the existing Function App (scoped to its own RG)
resource functionApp 'Microsoft.Web/sites@2022-09-01' existing = {
  name: functionAppName
  scope: resourceGroup(functionRGName)
}

// Reference the existing Service Bus in its OWN scope
resource serviceBus 'Microsoft.ServiceBus/namespaces@2021-11-01' existing = {
  name: serviceBusName
}

resource dataSenderAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBus.id, functionApp.name, 'Azure Service Bus Data Sender')
  properties: {
    principalId: principalId
    roleDefinitionId: '/subscriptions/${subscription().subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
    principalType: 'ServicePrincipal'
  }
}

resource dataReceiverAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBus.id, functionApp.name, 'Azure Service Bus Data Receiver')
  properties: {
    principalId: principalId
    roleDefinitionId: '/subscriptions/${subscription().subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'
    principalType: 'ServicePrincipal'
  }
}
