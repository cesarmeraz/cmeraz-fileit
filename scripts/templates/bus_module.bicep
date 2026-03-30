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
    zoneRedundant: false
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
