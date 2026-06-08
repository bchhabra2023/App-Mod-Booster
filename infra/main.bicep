param location string = resourceGroup().location
param deployGenAi bool = false
param adminLogin string
param adminObjectId string

var suffix = uniqueString(resourceGroup().id)

module appService 'app-service.bicep' = {
  name: 'appsvc-${suffix}'
  params: {
    location: location
    suffix: suffix
  }
}

module sql 'azure-sql.bicep' = {
  name: 'sql-${suffix}'
  params: {
    location: location
    suffix: suffix
    adminLogin: adminLogin
    adminObjectId: adminObjectId
    managedIdentityName: appService.outputs.managedIdentityName
  }
}

module genai 'genai.bicep' = if (deployGenAi) {
  name: 'genai-${suffix}'
  params: {
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
    location: 'swedencentral'
    suffix: suffix
  }
}

output appServiceName string = appService.outputs.appServiceName
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = appService.outputs.managedIdentityPrincipalId
output managedIdentityName string = appService.outputs.managedIdentityName
output sqlServerFqdn string = sql.outputs.sqlServerFqdn
output databaseName string = sql.outputs.databaseName
output openAIEndpoint string = genai.outputs.openAIEndpoint ?? ''
output openAIModelName string = genai.outputs.openAIModelName ?? ''
output openAIName string = genai.outputs.openAIName ?? ''
output searchEndpoint string = genai.outputs.searchEndpoint ?? ''
