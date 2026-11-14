@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource ndbotkv 'Microsoft.KeyVault/vaults@2024-11-01' = {
  name: take('ndbotkv-${uniqueString(resourceGroup().id)}', 24)
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
  }
  tags: {
    'aspire-resource-name': 'ndbotkv'
  }
}

output vaultUri string = ndbotkv.properties.vaultUri

output name string = ndbotkv.name

output id string = ndbotkv.id