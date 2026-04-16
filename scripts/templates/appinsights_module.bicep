param appInsightsName string
param workspaceName string
param workspaceRetentionDays int = 30
param tagsByResource object

resource workspace 'Microsoft.OperationalInsights/workspaces@2025-07-01' = {
  name: workspaceName
  location: resourceGroup().location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    retentionInDays: workspaceRetentionDays
  }
  tags: tagsByResource
}
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: resourceGroup().location
  kind: 'other'
  properties: {
    Application_Type: 'other'
    // The WorkspaceResourceId property links Application Insights to a Log Analytics workspace.
    WorkspaceResourceId: workspace.id
    Request_Source: 'rest'
  }
  tags: tagsByResource
}

// Output the instrumentation key for use in other resources
output instrumentationKey string = appInsights.properties.InstrumentationKey
output connectionString string = appInsights.properties.ConnectionString
