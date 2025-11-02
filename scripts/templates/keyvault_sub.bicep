@description('The Azure region for the deployment')
param location string = 'eastus2'

@description('A unique value, like domain')
param stem string

param servicePrincipalId string

targetScope = 'subscription'


resource rg_keyvault 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  location: location
  name: 'rg-${stem}-keyvault'
  properties: {}
  tags: {
    stem: stem
    module: 'keyvault_module'
  }
}

module keyvault_module 'keyvault_module.bicep' = {
  name: 'keyvault_module'
  scope: resourceGroup(rg_keyvault.name)
  params: {
    keyVaultName: '${stem}-keyvault'
    objectId: servicePrincipalId
  }
}
