#!/bin/bash

# Deployment script for Expense Management System
# This script deploys the infrastructure and application without GenAI resources

set -e  # Exit on error

echo "======================================"
echo "Expense Management System Deployment"
echo "======================================"
echo ""

# Check if Azure CLI is logged in
if ! az account show > /dev/null 2>&1; then
    echo "❌ Not logged into Azure CLI. Please run 'az login' first."
    exit 1
fi

# Variables - Update these as needed
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-expensemgmt-demo}"
LOCATION="${LOCATION:-uksouth}"
DEPLOY_GENAI=false

# Get current user info for SQL admin
CURRENT_USER=$(az account show --query user.name -o tsv)
CURRENT_USER_OID=$(az ad signed-in-user show --query id -o tsv)

echo "📋 Deployment Configuration:"
echo "   Resource Group: $RESOURCE_GROUP"
echo "   Location: $LOCATION"
echo "   SQL Admin: $CURRENT_USER"
echo "   Deploy GenAI: $DEPLOY_GENAI"
echo ""

# Create resource group
echo "🔨 Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION --output none
echo "✓ Resource group created"
echo ""

# Deploy infrastructure
echo "🚀 Deploying infrastructure (App Service, Managed Identity, Azure SQL)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group $RESOURCE_GROUP \
    --template-file infrastructure/main.bicep \
    --parameters adminObjectId=$CURRENT_USER_OID \
    --parameters adminLogin=$CURRENT_USER \
    --parameters deployGenAI=$DEPLOY_GENAI \
    --query 'properties.outputs' \
    --output json)

echo "✓ Infrastructure deployed"
echo ""

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceUrl.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.databaseName.value')

echo "📊 Deployment Outputs:"
echo "   App Service: $APP_SERVICE_NAME"
echo "   App URL: $APP_SERVICE_URL"
echo "   Managed Identity: $MANAGED_IDENTITY_NAME"
echo "   SQL Server: $SQL_SERVER_FQDN"
echo "   Database: $DATABASE_NAME"
echo ""

# Configure App Service settings
echo "⚙️  Configuring App Service settings..."
CONNECTION_STRING="Server=tcp:${SQL_SERVER_FQDN};Database=${DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};"

az webapp config appsettings set \
    --name $APP_SERVICE_NAME \
    --resource-group $RESOURCE_GROUP \
    --settings \
        "ConnectionStrings__DefaultConnection=$CONNECTION_STRING" \
        "ManagedIdentityClientId=$MANAGED_IDENTITY_CLIENT_ID" \
        "AZURE_CLIENT_ID=$MANAGED_IDENTITY_CLIENT_ID" \
    --output none

echo "✓ App Service configured"
echo ""

# Wait for SQL Server to be fully ready
echo "⏳ Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30

# Add current IP to SQL firewall
echo "🔥 Configuring SQL Server firewall..."
MY_IP=$(curl -s https://api.ipify.org)
SQL_SERVER_NAME=$(echo $SQL_SERVER_FQDN | cut -d'.' -f1)

# Allow Azure services access
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowAllAzureIPs" \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0 \
    --output none

# Add deployment IP
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowDeploymentIP" \
    --start-ip-address $MY_IP \
    --end-ip-address $MY_IP \
    --output none

echo "✓ Firewall rules configured"
echo "⏳ Waiting 15 seconds for firewall rules to propagate..."
sleep 15
echo ""

# Update Python scripts with actual server and database names
echo "📝 Updating Python scripts..."
sed -i.bak "s/SERVER = \".*\"/SERVER = \"$SQL_SERVER_FQDN\"/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/DATABASE = \".*\"/DATABASE = \"$DATABASE_NAME\"/g" run-sql.py && rm -f run-sql.py.bak

sed -i.bak "s/SERVER = \".*\"/SERVER = \"$SQL_SERVER_FQDN\"/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/DATABASE = \".*\"/DATABASE = \"$DATABASE_NAME\"/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak

sed -i.bak "s/SERVER = \".*\"/SERVER = \"$SQL_SERVER_FQDN\"/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
sed -i.bak "s/DATABASE = \".*\"/DATABASE = \"$DATABASE_NAME\"/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak

# Update script.sql with managed identity name
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak

echo "✓ Scripts updated"
echo ""

# Install Python dependencies
echo "📦 Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity
echo "✓ Dependencies installed"
echo ""

# Import database schema
echo "💾 Importing database schema..."
python3 run-sql.py
echo "✓ Schema imported"
echo ""

# Configure database roles for managed identity
echo "🔐 Configuring database roles for managed identity..."
python3 run-sql-dbrole.py
echo "✓ Database roles configured"
echo ""

# Create stored procedures
echo "📋 Creating stored procedures..."
python3 run-sql-stored-procs.py
echo "✓ Stored procedures created"
echo ""

# Build and package application
echo "🔨 Building application..."
cd src/ExpenseManagement
dotnet publish -c Release -o ../../publish
cd ../..

# Create deployment zip
echo "📦 Creating deployment package..."
cd publish
zip -r ../app.zip . > /dev/null
cd ..
echo "✓ Deployment package created"
echo ""

# Deploy application
echo "🚀 Deploying application to Azure..."
az webapp deploy \
    --resource-group $RESOURCE_GROUP \
    --name $APP_SERVICE_NAME \
    --src-path ./app.zip \
    --type zip \
    --output none
echo "✓ Application deployed"
echo ""

# Restart app service
echo "🔄 Restarting App Service..."
az webapp restart --name $APP_SERVICE_NAME --resource-group $RESOURCE_GROUP --output none
echo "✓ App Service restarted"
echo ""

echo "======================================"
echo "✅ Deployment Complete!"
echo "======================================"
echo ""
echo "📱 Application URL: $APP_SERVICE_URL/Index"
echo ""
echo "🔗 Swagger API Documentation: $APP_SERVICE_URL/swagger"
echo ""
echo "💡 Note: The chat feature will show a message that GenAI resources are not deployed."
echo "   To enable AI chat, run: ./deploy-with-chat.sh"
echo ""
echo "📝 To run the app locally:"
echo "   1. Update src/ExpenseManagement/appsettings.json with your connection string"
echo "   2. Use 'Authentication=Active Directory Default' for local dev"
echo "   3. Run 'az login' before starting the app"
echo "   4. Run 'dotnet run' from src/ExpenseManagement directory"
echo ""
