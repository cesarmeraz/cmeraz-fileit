@description('The Azure region for the deployment')
param location string = 'eastus2'

@description('A unique value, like domain')
param stem string

targetScope = 'subscription'


resource rg_storage 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  location: location
  name: 'rg-${stem}-storage'
  properties: {}
  tags: {
    stem: stem
    module: 'storage_module'
  }
}

module storage_module 'storage_module.bicep' = {
  name: 'storage_module'
  scope: resourceGroup(rg_storage.name)
  params: {
    location: location
    storageAccountName: 'cmerazfileitstorage'
    accountType: 'Standard_LRS'
    accessTier: 'Hot'
    kind: 'StorageV2'
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    defaultOAuth: false
    publicNetworkAccess: 'Enabled'
    allowCrossTenantReplication: false
    networkAclsBypass: 'AzureServices'
    networkAclsDefaultAction: 'Allow'
    networkAclsIpRules: []
    networkAclsIpv6Rules: []
    publishIpv6Endpoint: false
    dnsEndpointType: 'Standard'
    isHnsEnabled: true
    isSftpEnabled: false
    largeFileSharesState: 'Enabled'
    keySource: 'Microsoft.Storage'
    encryptionEnabled: true
    keyTypeForTableAndQueueEncryption: 'Account'
    infrastructureEncryptionEnabled: false
    isShareSoftDeleteEnabled: true
  }
}
