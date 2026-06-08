@description('Object ID of the Entra ID administrator for SQL')
param adminObjectId string

@description('User principal name of the Entra ID administrator for SQL')
param adminLogin string

@description('Deploy Azure OpenAI and Azure AI Search resources')
param deployGenAI bool = false

module appService 'app-service.bicep' = {
  name: 'deploy-app-service'
}

module azureSql 'azure-sql.bicep' = {
  name: 'deploy-azure-sql'
  params: {
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

module genai 'genai.bicep' = if (deployGenAI) {
  name: 'deploy-genai'
  params: {
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
    managedIdentityClientId: appService.outputs.managedIdentityClientId
  }
}

output appServiceUrl string = appService.outputs.appServiceUrl
output sqlServerFqdn string = azureSql.outputs.sqlServerFqdn
output sqlDatabaseName string = azureSql.outputs.sqlDatabaseName
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output openAIEndpoint string = genai.?outputs.?openAIEndpoint ?? ''
output openAIModelName string = genai.?outputs.?openAIModelName ?? ''
output searchEndpoint string = genai.?outputs.?searchEndpoint ?? ''
