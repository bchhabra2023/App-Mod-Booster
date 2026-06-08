param location string
param suffix string

var planName = toLower('asp-expense-${suffix}')
var appName = toLower('app-expense-${suffix}')
var managedIdentityName = toLower('mid-appmodassist-${suffix}')

resource appIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
}

resource appPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  sku: {
    name: 'S1'
    tier: 'Standard'
    size: 'S1'
    capacity: 1
  }
  kind: 'app'
  properties: {
    reserved: false
  }
}

resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${appIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: appPlan.id
    httpsOnly: true
  }
}

output appServiceName string = appService.name
output managedIdentityPrincipalId string = appIdentity.properties.principalId
output managedIdentityClientId string = appIdentity.properties.clientId
output managedIdentityName string = appIdentity.name
