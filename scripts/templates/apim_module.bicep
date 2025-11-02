param apimName string
param zones array
param location string
param tier string
param capacity int
param adminEmail string
param organizationName string
param virtualNetworkType string
param tagsByResource object
param vnet object
param customProperties object
param identity object
param appInsightsObject object
param privateDnsDeploymentName string
param subnetDeploymentName string

var apimNsgName = 'apimnsg${uniqueString(resourceGroup().id)}${apimName}'

resource apim 'Microsoft.ApiManagement/service@2022-09-01-preview' = {
  name: apimName
  location: location
  tags: tagsByResource
  sku: {
    name: tier
    capacity: capacity
  }
  zones: zones
  identity: identity
  properties: {
    publisherEmail: adminEmail
    publisherName: organizationName
    customProperties: customProperties
  }
  dependsOn: []
}
