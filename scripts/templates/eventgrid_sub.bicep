// This template can be deployed to either RG or the Subscription
param subscriptionId string
param storageRGName string
param storageAccountName string
param functionRGName string
param functionAppName string
param functionName string
param containerName string
param subscriptionName string
param eventGridTopicName string
@description('Principal ID of the identity to assign RBAC to')
param principalId string

targetScope = 'subscription'

// Call the module and target the Storage Resource Group
module eventGridDeploy 'eventgrid_module.bicep' = {
  name: 'eventGridDeployment'
  scope: resourceGroup(storageRGName) 
  params: {
    subscriptionId: subscriptionId
    storageAccountName: storageAccountName
    functionRGName: functionRGName
    functionAppName: functionAppName
    functionName: functionName
    containerName: containerName
    subscriptionName: subscriptionName
    eventGridTopicName: eventGridTopicName
    principalId: principalId
  }
}

module eventgrid_rbac 'eventgrid_rbacs.bicep' = {
  name: 'eventgrid_rbac'
  scope: resourceGroup(storageRGName)
  params: {
    functionAppName: functionAppName
    functionRGName: functionRGName
    eventGridTopicName: eventGridTopicName
    principalId: principalId
  }
}
