#!/usr/bin/env bash
set -euo pipefail

: "${RESOURCE_GROUP:?Set RESOURCE_GROUP}"
: "${LOCATION:=uksouth}"
: "${ADMIN_UPN:?Set ADMIN_UPN}"
: "${ADMIN_OBJECT_ID:?Set ADMIN_OBJECT_ID}"

DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file infra/main.bicep \
  --parameters location="$LOCATION" deployGenAi=true adminLogin="$ADMIN_UPN" adminObjectId="$ADMIN_OBJECT_ID" \
  --query properties.outputs -o json)

APP_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.appServiceName.value')
SQL_SERVER_FQDN=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.sqlServerFqdn.value')
SQL_DATABASE_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.databaseName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.managedIdentityClientId.value')
OPENAI_ENDPOINT=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.openAIEndpoint.value')
OPENAI_MODEL_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.openAIModelName.value')
SEARCH_ENDPOINT=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.searchEndpoint.value')

# Add current IP to SQL firewall
echo "Adding current IP to SQL firewall..."
MY_IP=$(curl -s https://api.ipify.org)
SQL_SERVER_NAME=$(echo "$SQL_SERVER_FQDN" | cut -d'.' -f1)

# Allow Azure services access
echo "Allowing Azure services access to SQL Server..."
az sql server firewall-rule create \
    --resource-group "$RESOURCE_GROUP" \
    --server "$SQL_SERVER_NAME" \
    --name "allowallazureips" \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0 \
    --output none

# Add deployment IP
az sql server firewall-rule create \
    --resource-group "$RESOURCE_GROUP" \
    --server "$SQL_SERVER_NAME" \
    --name "allowdeploymentip" \
    --start-ip-address "$MY_IP" \
    --end-ip-address "$MY_IP" \
    --output none

echo "Waiting additional 15 seconds for firewall rules to propagate..."
sleep 15

pip3 install --quiet pyodbc azure-identity

export SQL_SERVER_FQDN
export SQL_DATABASE_NAME
python3 run-sql.py
python3 run-sql-dbrole.py
python3 run-sql-stored-procs.py

az webapp config appsettings set --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" --settings \
  "ConnectionStrings__ExpenseDb=Server=tcp:${SQL_SERVER_FQDN};Database=${SQL_DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
  "OpenAI__Endpoint=${OPENAI_ENDPOINT}" \
  "OpenAI__DeploymentName=${OPENAI_MODEL_NAME}" \
  "Search__Endpoint=${SEARCH_ENDPOINT}" \
  "AZURE_CLIENT_ID=${MANAGED_IDENTITY_CLIENT_ID}" \
  "ManagedIdentityClientId=${MANAGED_IDENTITY_CLIENT_ID}" >/dev/null

pushd ExpenseManagementApp >/dev/null
dotnet publish -c Release -o ../app
popd >/dev/null

(cd app && zip -r ../app.zip .)

az webapp deploy \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --src-path ./app.zip \
  --type zip >/dev/null

echo "App + chat deployed: https://${APP_NAME}.azurewebsites.net/Index"
