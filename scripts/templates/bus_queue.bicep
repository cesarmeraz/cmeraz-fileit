param namespace string
param name string

// Reference the existing namespace
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: namespace
}

// Re-create the queue only after the deletion script finishes
resource newQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: name
  properties: {
    maxSizeInMegabytes: 1024
  }
}
