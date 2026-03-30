
param commonGroupName string = 'rg-fileit-common'

param simpleGroupName string = 'rg-fileit-simple'


targetScope = 'subscription'

module userCommon 'user_module.bicep' = {
  name: 'miFileitCommonModule'
  scope: resourceGroup(commonGroupName)
  params: {
    identityName: 'mi-fileit-common'
  }
}

module userSimple 'user_module.bicep' = {
  name: 'miFilitSimpleModule'
  scope: resourceGroup(simpleGroupName)
  params: {
    identityName: 'mi-fileit-simple'
  }
}

output commonClientId string = userCommon.outputs.clientId
output commonId string = userCommon.outputs.identityId
output commonPrincipalId string = userCommon.outputs.principalId

output simpleClientId string = userSimple.outputs.clientId
output simpleId string = userSimple.outputs.identityId
output simplePrincipalId string = userSimple.outputs.principalId
