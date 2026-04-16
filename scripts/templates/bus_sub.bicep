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

module queue_simple 'bus_queue.bicep' = {
  name: '${resourceName}-queue-simple'
  scope: resourceGroup(rg_bus.name)
  params: {
    namespace: resourceName
     name: 'simple'
  }
}

module queue_api_add 'bus_queue.bicep' = {
  name: '${resourceName}-queue-api-add'
  scope: resourceGroup(rg_bus.name)
  params: {
    namespace: resourceName
     name: 'api-add'
  }
}

module topic_api_add_topic 'bus_topic.bicep' = {
  name: '${resourceName}-topic-api-add-topic'
  scope: resourceGroup(rg_bus.name)
  params: {
    namespace: resourceName
     name: 'api-add-topic'
  }
}

module api_add_topic_subscription 'bus_subscription.bicep' = {
  name: '${resourceName}-topic-api-add-topic-subscription'
  scope: resourceGroup(rg_bus.name)
  params: {
    namespace: resourceName
    topicName: 'api-add-topic'
    subscriptionName: 'api-add-topic-subscription'
  }
}
