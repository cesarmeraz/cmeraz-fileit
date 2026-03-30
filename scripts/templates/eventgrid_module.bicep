param storageAccountName string
param functionAppName string
param functionRGName string // Needed to build the full ID
@description('Principal ID of the identity to assign RBAC to')
param principalId string
param functionName string
param containerName string
param subscriptionName string
param subscriptionId string

@description('The event grid system topic name')
param eventGridTopicName string

// EventGridContributor
var role='1e241071-0855-49ea-94dc-649edcd759de'

// Reference existing Storage Account in the CURRENT scope
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
}

// Reference the existing Function App (scoped to its own RG)
resource functionApp 'Microsoft.Web/sites@2022-09-01' existing = {
  name: functionAppName
  scope: resourceGroup(functionRGName)
}


// Create the System Topic in the Storage RG
resource systemTopic 'Microsoft.EventGrid/systemTopics@2022-06-15' = {
  name: eventGridTopicName
  location: resourceGroup().location
  properties: {
    source: storageAccount.id
    topicType: 'Microsoft.Storage.StorageAccounts'
  }
}

resource eventGridRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(systemTopic.id, functionApp.name, role)
  scope: systemTopic
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', role)
    principalType: 'ServicePrincipal'
  }
}


// Create the Subscription
resource eventSubscription 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2022-06-15' = {
  parent: systemTopic
  name: subscriptionName
  properties: {
    destination: {
      endpointType: 'AzureFunction'
      properties: {
        // Construct full resource ID for the function
        resourceId: '/subscriptions/${subscriptionId}/resourceGroups/${functionRGName}/providers/Microsoft.Web/sites/${functionAppName}/functions/${functionName}'
      }
    }
    filter: {
      includedEventTypes: ['Microsoft.Storage.BlobCreated']
      subjectBeginsWith: '/blobServices/default/containers/${containerName}/'
      isSubjectCaseSensitive: false
    }
  }
}
