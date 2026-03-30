@description('The function app name')
param functionAppName string

@description('The function app resource group name')
param functionRGName string

@description('The event grid system topic name')
param eventGridTopicName string

@description('Principal ID of the identity to assign RBAC to')
param principalId string


// EventGridContributor
var role='2414bbcf-6497-4faf-8c65-045460748405'
// 428e0ff0-5e57-4d9c-a221-2c70d0e0a443	EventGrid EventSubscription Contributor
// 2414bbcf-6497-4faf-8c65-045460748405	EventGrid EventSubscription Reader

// Reference the existing Function App (scoped to its own RG)
resource functionApp 'Microsoft.Web/sites@2022-09-01' existing = {
  name: functionAppName
  scope: resourceGroup(functionRGName)
}

// Reference the existing System Topic (scoped to the Storage Account)
resource systemTopic 'Microsoft.EventGrid/systemTopics@2022-06-15' existing = {
  name: eventGridTopicName
  scope: resourceGroup() // the CURRENT scope is the Storage Account resource group, so we can reference it directly
}

// 2. Assign EventGrid EventSubscription Contributor
resource eventGridRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(systemTopic.name, functionApp.name, role)
  scope: systemTopic
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', role)
    principalType: 'ServicePrincipal'
  }
}


