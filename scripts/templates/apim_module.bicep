@description('The Azure region for the deployment')
param location string

@description('The resource name')
param resourceName string

@description('Tags applied to the resource')
param tagsByResource object

param tier string
param capacity int
param adminEmail string
param organizationName string
param customProperties object
param identity object
param zones array

resource apim 'Microsoft.ApiManagement/service@2022-09-01-preview' = {
  name: resourceName
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
