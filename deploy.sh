#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

DEPLOYMENT_NAME="expense-modernize"
LOCATION="uksouth"

read -r -p "Resource group name: " RESOURCE_GROUP
read -r -p "Azure AD admin object ID: " ADMIN_OBJECT_ID
read -r -p "Azure AD admin login (UPN/email): " ADMIN_LOGIN

echo "[1/14] Creating resource group..."
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none

echo "[2/14] Deploying main.bicep (deployGenAI=false)..."
DEPLOYMENT_OUTPUT="$(az deployment group create \
  --name "$DEPLOYMENT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --template-file infra/main.bicep \
  --parameters adminObjectId="$ADMIN_OBJECT_ID" adminLogin="$ADMIN_LOGIN" deployGenAI=false \
  --query properties.outputs \
  --output json)"

readarray -t DEPLOY_VALUES < <(python3 - <<'PY' "$DEPLOYMENT_OUTPUT"
import json,sys
outputs = json.loads(sys.argv[1])
print(outputs['appServiceUrl']['value'].split('//',1)[1].split('.',1)[0])
print(outputs['sqlServerFqdn']['value'])
print(outputs['sqlDatabaseName']['value'])
print(outputs['managedIdentityClientId']['value'])
PY
)
APP_NAME="${DEPLOY_VALUES[0]}"
SQL_FQDN="${DEPLOY_VALUES[1]}"
SQL_DB="${DEPLOY_VALUES[2]}"
MI_CLIENT_ID="${DEPLOY_VALUES[3]}"

echo "[3/14] Configuring App Service settings..."
CONNECTION_STRING="Server=tcp:${SQL_FQDN},1433;Database=${SQL_DB};Authentication=Active Directory Managed Identity;User Id=${MI_CLIENT_ID};"
az webapp config appsettings set \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --settings \
    "ConnectionStrings__DefaultConnection=${CONNECTION_STRING}" \
    "AZURE_CLIENT_ID=${MI_CLIENT_ID}" \
    "ManagedIdentityClientId=${MI_CLIENT_ID}" \
  --output none

echo "[4/14] Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30

SQL_SERVER_NAME="${SQL_FQDN%%.*}"
CURRENT_IP="$(curl -s https://api.ipify.org)"

echo "[5/14] Adding current IP to SQL firewall..."
az sql server firewall-rule create \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER_NAME" \
  --name allowcurrentip \
  --start-ip-address "$CURRENT_IP" \
  --end-ip-address "$CURRENT_IP" \
  --output none

echo "[6/14] Allowing Azure services through SQL firewall..."
az sql server firewall-rule create \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER_NAME" \
  --name allowazureservices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0 \
  --output none

echo "[7/14] Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity

echo "[8/14] Updating SQL execution scripts..."
sed -i.bak "s/SERVER = \"example.database.windows.net\"/SERVER = \"${SQL_FQDN}\"/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/SERVER = \"example.database.windows.net\"/SERVER = \"${SQL_FQDN}\"/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/SERVER = \"example.database.windows.net\"/SERVER = \"${SQL_FQDN}\"/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
sed -i.bak "s/DATABASE = \"Northwind\"/DATABASE = \"${SQL_DB}\"/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/DATABASE = \"Northwind\"/DATABASE = \"${SQL_DB}\"/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/DATABASE = \"Northwind\"/DATABASE = \"${SQL_DB}\"/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
MI_NAME="$(az identity list --resource-group "$RESOURCE_GROUP" --query "[0].name" -o tsv)"
sed -i.bak "s/MANAGED-IDENTITY-NAME/${MI_NAME}/g" script.sql && rm -f script.sql.bak

echo "[9/14] Running database schema script..."
python3 run-sql.py

echo "[10/14] Running managed identity DB role script..."
python3 run-sql-dbrole.py

echo "[11/14] Running stored procedure script..."
python3 run-sql-stored-procs.py

echo "[12/14] Building application..."
rm -rf app/ExpenseApp/publish app.zip
dotnet publish -c Release -o app/ExpenseApp/publish app/ExpenseApp/ExpenseApp.csproj

echo "[13/14] Creating app.zip..."
(
  cd app/ExpenseApp/publish
  zip -r ../../../app.zip . >/dev/null
)

echo "[14/14] Deploying App Service package..."
az webapp deploy --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" --src-path ./app.zip --type zip --output none

echo "App available at: https://${APP_NAME}.azurewebsites.net/Index"
