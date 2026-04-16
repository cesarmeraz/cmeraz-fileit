@description('The name of the key vault')
param keyVaultName string

@description('The SKU of the vault to deploy')
@allowed([
  'premium'
  'standard'
])
param skuName string = 'standard'

@description('Object ID of the AAD user or service principal that will have access to the vault')
param objectId string

resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: keyVaultName
  location: resourceGroup().location
  properties: {
    tenantId: subscription().tenantId
    enabledForDeployment: false
    enabledForTemplateDeployment: true
    enabledForDiskEncryption: false
    enableRbacAuthorization: true
    enableSoftDelete: false
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
    sku: {
      name: skuName
      family: 'A'
    }
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: objectId
        permissions: {
          keys: [
            'get'
            'list'
            'create'
            'delete'
          ]
          secrets: [
            'get'
            'list'
            'set'
            'delete'
          ]
          certificates: [
            'get'
            'list'
            'create'
            'delete'
          ]
        }
      }
    ]
  }
}

output keyVaultUri string = keyVault.properties.vaultUri
