param location string
param storageAccountName string
param tagsByResource object
param accountType string
param kind string
param minimumTlsVersion string
param supportsHttpsTrafficOnly bool
param allowBlobPublicAccess bool
param allowSharedKeyAccess bool
param defaultOAuth bool
param accessTier string
param publicNetworkAccess string
param allowCrossTenantReplication bool
param networkAclsBypass string
param networkAclsDefaultAction string
param networkAclsIpRules array
param networkAclsIpv6Rules array
param publishIpv6Endpoint bool
param dnsEndpointType string
param isHnsEnabled bool
param isSftpEnabled bool
param largeFileSharesState string
param keySource string
param encryptionEnabled bool
param keyTypeForTableAndQueueEncryption string
param infrastructureEncryptionEnabled bool
param isShareSoftDeleteEnabled bool

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tagsByResource
  sku: {
    name: accountType
  }
  kind: kind
  properties: {
    minimumTlsVersion: minimumTlsVersion
    supportsHttpsTrafficOnly: supportsHttpsTrafficOnly
    allowBlobPublicAccess: allowBlobPublicAccess
    allowSharedKeyAccess: allowSharedKeyAccess
    defaultToOAuthAuthentication: defaultOAuth
    accessTier: accessTier
    publicNetworkAccess: publicNetworkAccess
    allowCrossTenantReplication: allowCrossTenantReplication
    networkAcls: {
      bypass: networkAclsBypass
      defaultAction: networkAclsDefaultAction
      ipRules: networkAclsIpRules
      ipv6Rules: networkAclsIpv6Rules
    }
    dualStackEndpointPreference: {
      publishIpv6Endpoint: publishIpv6Endpoint
    }
    dnsEndpointType: dnsEndpointType
    isHnsEnabled: isHnsEnabled
    isSftpEnabled: isSftpEnabled
    largeFileSharesState: largeFileSharesState
    encryption: {
      keySource: keySource
      services: {
        blob: {
          enabled: encryptionEnabled
        }
        file: {
          enabled: encryptionEnabled
        }
        table: {
          enabled: encryptionEnabled
        }
        queue: {
          enabled: encryptionEnabled
        }
      }
      requireInfrastructureEncryption: infrastructureEncryptionEnabled
    }
  }
  dependsOn: []
}

resource storageAccountName_default 'Microsoft.Storage/storageAccounts/fileservices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    protocolSettings: null
    shareDeleteRetentionPolicy: {
      enabled: isShareSoftDeleteEnabled
    }
  }
}
