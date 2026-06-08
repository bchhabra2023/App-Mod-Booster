param location string = 'swedencentral'
param suffix string
param managedIdentityPrincipalId string

var openAiName = toLower('aoai-expense-${suffix}')
var searchName = toLower('aisearch-expense-${suffix}')
var openAiModelName = 'gpt-4o'

resource openAi 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAiName
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
    capacity: 8
  }
  properties: {
    customSubDomainName: openAiName
    publicNetworkAccess: 'Enabled'
  }
}

resource openAiDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAi
  name: openAiModelName
  sku: {
    name: 'Standard'
    capacity: 8
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-11-20'
    }
    raiPolicyName: 'Microsoft.Default'
  }
}

resource search 'Microsoft.Search/searchServices@2023-11-01' = {
  name: searchName
  location: location
  sku: {
    name: 'standard'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
  }
}

resource openAiRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAi.id, managedIdentityPrincipalId, 'openai-user')
  scope: openAi
  properties: {
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
  }
}

resource searchRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(search.id, managedIdentityPrincipalId, 'search-index-data-reader')
  scope: search
  properties: {
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '1407120a-92aa-4202-b7e9-c0e197c71c8f')
  }
}

output openAIEndpoint string = openAi.properties.endpoint
output openAIModelName string = openAiDeployment.name
output openAIName string = openAi.name
output searchEndpoint string = 'https://${search.name}.search.windows.net'
