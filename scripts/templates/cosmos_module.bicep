param name string
param location string
param locationName string
param defaultExperience string
param isZoneRedundant string

resource name_resource 'Microsoft.DocumentDb/databaseAccounts@2025-05-01-preview' = {
  name: name
  location: location
  tags: {
    defaultExperience: defaultExperience
    'hidden-workload-type': 'Learning'
    'hidden-cosmos-mmspecial': ''
  }
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        id: '${name}-${location}'
        failoverPriority: 0
        locationName: locationName
      }
    ]
    backupPolicy: {
      type: 'Periodic'
      periodicModeProperties: {
        backupIntervalInMinutes: 240
        backupRetentionIntervalInHours: 8
        backupStorageRedundancy: 'Local'
      }
    }
    isVirtualNetworkFilterEnabled: false
    virtualNetworkRules: []
    ipRules: []
    dependsOn: []
    minimalTlsVersion: 'Tls12'
    capabilities: []
    capacityMode: 'Serverless'
    enableFreeTier: false
    capacity: {
      totalThroughputLimit: 4000
    }
    disableLocalAuth: true
  }
}
