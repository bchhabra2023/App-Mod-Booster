// Main Bicep template for Expense Management System
// Deploys App Service, Managed Identity, Azure SQL Database, and optionally GenAI resources

@description('Location for all resources')
param location string = 'uksouth'

@description('Administrator object ID for SQL Server')
param adminObjectId string

@description('Administrator login name for SQL Server')
param adminLogin string

@description('Deploy GenAI resources (Azure OpenAI and AI Search)')
param deployGenAI bool = false

// Generate unique names using resource group ID
var uniqueSuffix = toLower(uniqueString(resourceGroup().id))
var appServiceName = 'app-expensemgmt-${uniqueSuffix}'
var sqlServerName = 'sql-expensemgmt-${uniqueSuffix}'
var databaseName = 'Northwind'

// Deploy App Service with Managed Identity
module appService 'modules/app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
    appServiceName: appServiceName
  }
}

// Deploy Azure SQL Database
module azureSQL 'modules/azure-sql.bicep' = {
  name: 'azureSQLDeployment'
  params: {
    location: location
    sqlServerName: sqlServerName
    databaseName: databaseName
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
    managedIdentityName: appService.outputs.managedIdentityName
  }
}

// Optionally deploy GenAI resources
module genAI 'modules/genai.bicep' = if (deployGenAI) {
  name: 'genAIDeployment'
  params: {
    location: location
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityName string = appService.outputs.managedIdentityName
output sqlServerFqdn string = azureSQL.outputs.sqlServerFqdn
output databaseName string = azureSQL.outputs.databaseName

// GenAI outputs (only available when deployed)
output openAIEndpoint string = deployGenAI ? genAI.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genAI.outputs.openAIModelName : ''
output searchEndpoint string = deployGenAI ? genAI.outputs.searchEndpoint : ''
output openAIName string = deployGenAI ? genAI.outputs.openAIName : ''
