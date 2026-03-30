@description('The Azure region for the deployment')
param location string = resourceGroup().location

@description('The resource name')
param resourceName string

@description('Tags applied to the resource')
param tagsByResource object

@description('The name of the SQL Database.')
param databaseName string = 'FileIt'

@description('The administrator username for the SQL server.')
param adminLogin string = 'sa'

@description('The administrator password for the SQL server.')
@secure()
param adminPassword string

@description('Your local public IP address for firewall access.')
param myLocalIpAddress string


// 1. Create the SQL Logical Server
resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: resourceName
  location: location
  properties: {
    administratorLogin: adminLogin
    administratorLoginPassword: adminPassword
  }
  tags: tagsByResource
}

// 2. Create the Serverless Database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'GP_S_Gen5'      // 'S' denotes Serverless
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 2            // Max vCores
  }
  properties: {
    minCapacity: any('0.5') // Min vCores available while active
    autoPauseDelay: 60      // Automatically pause after 60 mins of inactivity
  }
}

// 3. Add Firewall Rule for Local Access
resource firewallRule 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = {
  parent: sqlServer
  name: 'AllowLocalAccess'
  properties: {
    startIpAddress: myLocalIpAddress
    endIpAddress: myLocalIpAddress
  }
}

// 4. Optional: Allow Azure Services (needed if using Data Factory/Functions later)
resource allowAzureIps 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}
