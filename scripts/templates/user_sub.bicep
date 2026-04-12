
param servicesGroupName string = 'rg-fileit-services'

param simpleGroupName string = 'rg-fileit-simple'


targetScope = 'subscription'

module userServices 'user_module.bicep' = {
  name: 'miFileitServicesModule'
  scope: resourceGroup(servicesGroupName)
  params: {
    identityName: 'mi-fileit-services'
  }
}

module userSimple 'user_module.bicep' = {
  name: 'miFilitSimpleModule'
  scope: resourceGroup(simpleGroupName)
  params: {
    identityName: 'mi-fileit-simple'
  }
}

output servicesClientId string = userServices.outputs.clientId
output servicesId string = userServices.outputs.identityId
output servicesPrincipalId string = userServices.outputs.principalId

output simpleClientId string = userSimple.outputs.clientId
output simpleId string = userSimple.outputs.identityId
output simplePrincipalId string = userSimple.outputs.principalId
