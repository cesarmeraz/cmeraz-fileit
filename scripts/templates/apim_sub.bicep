@description('The Azure region for the deployment')
param location string = 'eastus2'

@description('A unique value, like domain')
param stem string

targetScope = 'subscription'

resource rg_apim 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  location: location
  name: 'rg-${stem}-apim'
  properties: {}
  tags: {
    stem: stem
    module: 'apim_module'
  }
}

module apim_module 'apim_module.bicep' = {
  scope: rg_apim
  params: {
    apimName: '${stem}-apim'
    zones: []
    location: location
    tier: 'Consumption'
    capacity: 1
    adminEmail: 'cesar.meraz@gmail.com'
    organizationName: 'Cesar Meraz'
    virtualNetworkType: 'None'
    tagsByResource: {
      stem: stem
      module: 'apim_module'
    }
    appInsightsObject: {}
    vnet: {}
    privateDnsDeploymentName: ''
    subnetDeploymentName: ''
    customProperties: {}
    identity: {
      type: 'SystemAssigned'
    }
  }
}
