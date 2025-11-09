@description('The Azure region for the deployment')
param location string = 'centralus'

@description('A unique value, like domain')
param stem string

param group_name string

param name string

param deployment_name string

targetScope = 'subscription'


resource rg_storage 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  location: location
  name: group_name
  properties: {}
  tags: {
    stem: stem
    module: 'storage_module'
    deployment: deployment_name
  }
}

module storage_module 'storage_module.bicep' = {
  name: 'storage_module'
  scope: resourceGroup(rg_storage.name)
  params: {
    location: location
    storageAccountName: name
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
    deploymentName: deployment_name
  }
}
