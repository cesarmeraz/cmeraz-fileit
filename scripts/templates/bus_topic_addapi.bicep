param serviceBusNamespaceName string
param topicName string = 'myTopic'

// 1. Reference the existing namespace
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: serviceBusNamespaceName
}

// 2. Create the topic under that namespace
resource serviceBusTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: topicName
  properties: {
    supportOrdering: true
    defaultMessageTimeToLive: 'P14D' // 14 days
    enablePartitioning: false
  }
}
