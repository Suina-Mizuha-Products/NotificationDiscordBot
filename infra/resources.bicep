@description('The location used for all deployed resources')
param location string = resourceGroup().location

@description('Id of the user or app to assign application roles')
param principalId string = ''

@description('Tags that will be applied to all resources')
param tags object = {}

@allowed([
  'create'
  'use-existing'
])
@description('create: ACA Environmentを作成 / use-existing: 既存ACA Environmentを参照')
param containerAppEnvironmentMode string = 'create'

@description('既存ACA Environment名 (containerAppEnvironmentMode = use-existing の場合に使用)')
param sharedContainerAppEnvironmentName string = ''

@description('既存ACA Environmentのリソースグループ名 (containerAppEnvironmentMode = use-existing の場合に使用)')
param sharedContainerAppEnvironmentResourceGroupName string = ''

var resourceToken = uniqueString(resourceGroup().id)
var useExistingContainerAppEnvironment = containerAppEnvironmentMode == 'use-existing'

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'mi-${resourceToken}'
  location: location
  tags: tags
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: replace('acr-${resourceToken}', '-', '')
  location: location
  sku: {
    name: 'Basic'
  }
  tags: tags
}

resource caeMiRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerRegistry.id, managedIdentity.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
  scope: containerRegistry
  properties: {
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = if (!useExistingContainerAppEnvironment) {
  name: 'law-${resourceToken}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: tags
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-02-02-preview' = if (!useExistingContainerAppEnvironment) {
  name: 'cae-${resourceToken}'
  location: location
  properties: {
    workloadProfiles: [
      {
        workloadProfileType: 'Consumption'
        name: 'consumption'
      }
    ]
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
  tags: tags

  resource aspireDashboard 'dotNetComponents' = {
    name: 'aspire-dashboard'
    properties: {
      componentType: 'AspireDashboard'
    }
  }
}

resource sharedContainerAppEnvironment 'Microsoft.App/managedEnvironments@2024-02-02-preview' existing = if (useExistingContainerAppEnvironment) {
  scope: resourceGroup(sharedContainerAppEnvironmentResourceGroupName)
  name: sharedContainerAppEnvironmentName
}

output MANAGED_IDENTITY_CLIENT_ID string = managedIdentity.properties.clientId
output MANAGED_IDENTITY_NAME string = managedIdentity.name
output MANAGED_IDENTITY_PRINCIPAL_ID string = managedIdentity.properties.principalId
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = useExistingContainerAppEnvironment ? '' : logAnalyticsWorkspace.name
output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = useExistingContainerAppEnvironment ? '' : logAnalyticsWorkspace.id
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.properties.loginServer
output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = managedIdentity.id
output AZURE_CONTAINER_REGISTRY_NAME string = containerRegistry.name
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = useExistingContainerAppEnvironment ? sharedContainerAppEnvironment.name : containerAppEnvironment.name
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = useExistingContainerAppEnvironment ? sharedContainerAppEnvironment.id : containerAppEnvironment.id
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = useExistingContainerAppEnvironment ? sharedContainerAppEnvironment.properties.defaultDomain : containerAppEnvironment.properties.defaultDomain
