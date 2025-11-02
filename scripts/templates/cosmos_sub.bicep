@description('The Azure region for the deployment')
param location string = 'eastus2'

@description('A unique value, like domain')
param stem string

targetScope = 'subscription'


resource rg_cosmos 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  location: location
  name: 'rg-${stem}-cosmos'
  properties: {}
  tags: {
    stem: stem
    module: 'cosmos_module'
  }
}

module cosmos_module 'cosmos_module.bicep' = {
  name: 'cosmos_module'
  scope: resourceGroup(rg_cosmos.name)
  params: {
    defaultExperience: 'CoreSQL'
    location: location
    name: '${stem}-cosmos'
    locationName: location
    isZoneRedundant: 'false'
  }
}
