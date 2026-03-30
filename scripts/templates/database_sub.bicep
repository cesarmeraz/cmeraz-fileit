@description('The Azure region for the deployment')
param location string

@description('A unique value, like domain')
param stem string

@description('The name of the parent resource group')
param resourceGroupName string

@description('The resource name')
param resourceName string

@description('Tag value for deployment')
param deploymentName string

@description('The administrator username for the SQL server.')
param adminLogin string = 'sa'

@description('The administrator password for the SQL server.')
@secure()
param adminPassword string

@description('Your local public IP address for firewall access.')
param myLocalIpAddress string

targetScope = 'subscription'

resource rg_database 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  location: location
  name: resourceGroupName
  properties: {}
  tags: {
    stem: stem
    module: 'database_module'
    deployment: deploymentName
  }
}

module database_module 'database_module.bicep' = {
  name: resourceName
  scope: resourceGroup(rg_database.name)
  params: {
    resourceName: resourceName
    location: location
    adminLogin: adminLogin
    adminPassword: adminPassword
    myLocalIpAddress: myLocalIpAddress
    tagsByResource: {
      stem: stem
      module: 'database_module' 
      deployment: deploymentName
    }
  }
}
