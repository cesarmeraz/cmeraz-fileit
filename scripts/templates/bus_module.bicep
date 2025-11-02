param name string
param location string
param skuName string
param skuTier string
param skuCapacity int
param zoneRedundant bool
param minimumTlsVersion string
param disableLocalAuth bool
param publicNetworkAccess string
param tags object

resource name_resource 'Microsoft.ServiceBus/namespaces@2025-05-01-preview' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: skuTier
    tier: skuTier
    capacity: skuCapacity
  }
  properties: {
    zoneRedundant: zoneRedundant
    minimumTlsVersion: minimumTlsVersion
    disableLocalAuth: disableLocalAuth
    publicNetworkAccess: publicNetworkAccess
  }
}
