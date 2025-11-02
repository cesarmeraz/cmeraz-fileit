@description('The Azure region for the deployment')
param location string = 'eastus2'

@description('A unique value, like domain')
param stem string

targetScope = 'subscription'

resource rg_bus 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  location: location
  name: 'rg-${stem}-bus'
  properties: {}
  tags: {
    stem: stem
    module: 'bus_module'
  }
}

module bus_module 'bus_module.bicep' = {
  name: 'bus_module'
  scope: resourceGroup(rg_bus.name)
  params: {
    name: '${stem}-bus'
    location: location
    skuName: 'Standard'
    skuTier: 'Standard'
    skuCapacity: 1
    zoneRedundant: false
    minimumTlsVersion: '1.2'
    disableLocalAuth: false
    publicNetworkAccess: 'Enabled'
    tags: {
      stem: stem
      module: 'bus_module'
    }
  }
}
