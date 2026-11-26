@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param ndbotkv_outputs_name string

param principalType string

param principalId string

resource ndbotkv 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: ndbotkv_outputs_name
}

resource ndbotkv_KeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(ndbotkv.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalType: principalType
  }
  scope: ndbotkv
}