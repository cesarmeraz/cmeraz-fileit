@description('The Azure region for the deployment')
param location string

@description('The resource name')
param resourceName string

@description('Tags applied to the resource')
param tagsByResource object


resource namespaces_cmeraz_fileit_bus_name_resource 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: resourceName
  location: location
  tags: tagsByResource
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    premiumMessagingPartitions: 0
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
    zoneRedundant: true
  }
}

resource namespaces_cmeraz_fileit_bus_name_RootManageSharedAccessKey 'Microsoft.ServiceBus/namespaces/authorizationrules@2024-01-01' = {
  parent: namespaces_cmeraz_fileit_bus_name_resource
  name: 'RootManageSharedAccessKey'
  properties: {
    rights: [
      'Listen'
      'Manage'
      'Send'
    ]
  }
}

resource namespaces_cmeraz_fileit_bus_name_default 'Microsoft.ServiceBus/namespaces/networkrulesets@2024-01-01' = {
  parent: namespaces_cmeraz_fileit_bus_name_resource
  name: 'default'
  properties: {
    publicNetworkAccess: 'Enabled'
    defaultAction: 'Allow'
    virtualNetworkRules: []
    ipRules: []
    trustedServiceAccessEnabled: false
  }
}

resource namespaces_cmeraz_fileit_bus_name_simple 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  parent: namespaces_cmeraz_fileit_bus_name_resource
  name: 'simple'
  properties: {
    maxMessageSizeInKilobytes: 256
    lockDuration: 'PT1M'
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: false
    requiresSession: false
    defaultMessageTimeToLive: 'P14D'
    deadLetteringOnMessageExpiration: false
    enableBatchedOperations: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    maxDeliveryCount: 10
    status: 'Active'
    autoDeleteOnIdle: 'P10675199DT2H48M5.4775807S'
    enablePartitioning: false
    enableExpress: false
  }
}
