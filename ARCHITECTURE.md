# Azure Services Architecture Diagram

## Expense Management System - Cloud Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              AZURE CLOUD                                     │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                         Resource Group                                │  │
│  │                      rg-expensemgmt-demo                              │  │
│  │                                                                        │  │
│  │  ┌─────────────────────────────────────────────────────────────┐    │  │
│  │  │  User-Assigned Managed Identity                              │    │  │
│  │  │  mid-appmodassist-[timestamp]                                │    │  │
│  │  │  • Authenticates to Azure SQL                                │    │  │
│  │  │  • Authenticates to Azure OpenAI                             │    │  │
│  │  │  • Authenticates to AI Search                                │    │  │
│  │  └───────────────┬─────────────────────────────────────────────┘    │  │
│  │                  │                                                    │  │
│  │                  │ (uses identity)                                   │  │
│  │                  │                                                    │  │
│  │  ┌───────────────▼──────────────────────────────────────────────┐   │  │
│  │  │  App Service (Linux)                                          │   │  │
│  │  │  app-expensemgmt-[unique]                                     │   │  │
│  │  │  • ASP.NET Core 8.0 (Razor Pages + Web API)                   │   │  │
│  │  │  • S1 SKU (Standard tier)                                     │   │  │
│  │  │  • Environment Variables:                                     │   │  │
│  │  │    - ConnectionStrings__DefaultConnection                     │   │  │
│  │  │    - ManagedIdentityClientId                                  │   │  │
│  │  │    - AZURE_CLIENT_ID                                          │   │  │
│  │  │    - OpenAI__Endpoint (if GenAI deployed)                     │   │  │
│  │  │    - OpenAI__DeploymentName (if GenAI deployed)               │   │  │
│  │  │  • HTTPS only                                                 │   │  │
│  │  │  • Always On enabled                                          │   │  │
│  │  └───────────────┬──────────────────────────────────────────────┘   │  │
│  │                  │                                                    │  │
│  │                  │ (connects using Managed Identity)                 │  │
│  │                  │                                                    │  │
│  │  ┌───────────────▼──────────────────────────────────────────────┐   │  │
│  │  │  Azure SQL Database                                           │   │  │
│  │  │  sql-expensemgmt-[unique].database.windows.net                │   │  │
│  │  │  • Database: Northwind                                        │   │  │
│  │  │  • Basic tier                                                 │   │  │
│  │  │  • Azure AD-only authentication (MCAPS compliant)             │   │  │
│  │  │  • Firewall:                                                  │   │  │
│  │  │    - Allow Azure Services (0.0.0.0)                           │   │  │
│  │  │    - Deployment IP address                                    │   │  │
│  │  │  • Tables:                                                    │   │  │
│  │  │    - Users, Roles, Expenses                                   │   │  │
│  │  │    - ExpenseCategories, ExpenseStatus                         │   │  │
│  │  │  • Stored Procedures:                                         │   │  │
│  │  │    - GetAllExpenses, CreateExpense, etc.                      │   │  │
│  │  └───────────────────────────────────────────────────────────────┘   │  │
│  │                                                                        │  │
│  │  ┌────────────────────────────────────────────────────────────────┐  │  │
│  │  │  Azure OpenAI (Optional - deploy-with-chat.sh)                 │  │  │
│  │  │  openai-expensemgmt-[unique]                                   │  │  │
│  │  │  • Location: Sweden Central                                    │  │  │
│  │  │  • S0 SKU                                                      │  │  │
│  │  │  • Model Deployment:                                           │  │  │
│  │  │    - gpt-4o (capacity: 8)                                      │  │  │
│  │  │  • Authentication: Managed Identity                            │  │  │
│  │  │  • Role: Cognitive Services OpenAI User                        │  │  │
│  │  │  • Function Calling enabled for:                               │  │  │
│  │  │    - Expense management                                        │  │  │
│  │  │    - User management                                           │  │  │
│  │  │    - Category lookups                                          │  │  │
│  │  └────────────────────────────────────────────────────────────────┘  │  │
│  │                                                                        │  │
│  │  ┌────────────────────────────────────────────────────────────────┐  │  │
│  │  │  Azure AI Search (Optional - deploy-with-chat.sh)              │  │  │
│  │  │  search-expensemgmt-[unique]                                   │  │  │
│  │  │  • Basic SKU                                                   │  │  │
│  │  │  • 1 replica, 1 partition                                      │  │  │
│  │  │  • Authentication: Managed Identity                            │  │  │
│  │  │  • Role: Search Index Data Contributor                         │  │  │
│  │  │  • Used for RAG (Retrieval-Augmented Generation)               │  │  │
│  │  └────────────────────────────────────────────────────────────────┘  │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

                                     │
                                     │ HTTPS
                                     │
                                     ▼
                              ┌──────────────┐
                              │    Users     │
                              │   Browser    │
                              │              │
                              │  • Web UI    │
                              │  • REST API  │
                              │  • Swagger   │
                              │  • Chat UI   │
                              └──────────────┘
```

## Architecture Components

### 1. **User-Assigned Managed Identity**
   - Provides secure, password-less authentication
   - Eliminates need for connection strings with credentials
   - Assigned to App Service and granted permissions to:
     - Azure SQL Database (db_datareader, db_datawriter, EXECUTE)
     - Azure OpenAI (Cognitive Services OpenAI User)
     - AI Search (Search Index Data Contributor)

### 2. **App Service**
   - Hosts the ASP.NET Core 8.0 application
   - Linux-based with .NET runtime
   - Always On to prevent cold starts
   - Uses Managed Identity for all Azure service connections
   - Exposes:
     - Razor Pages UI (modern expense management interface)
     - REST APIs for expense operations
     - Swagger/OpenAPI documentation
     - Chat interface (when GenAI deployed)

### 3. **Azure SQL Database**
   - Stores all application data
   - Azure AD-only authentication (no SQL authentication)
   - Complies with MCAPS governance policies
   - Schema includes:
     - Users and Roles
     - Expenses with full lifecycle (Draft → Submitted → Approved/Rejected)
     - Categories (Travel, Meals, Supplies, Accommodation, Other)
   - All data access through stored procedures (no direct table access from app)

### 4. **Azure OpenAI** (Optional)
   - Deployed in Sweden Central for GPT-4o availability
   - Powers the AI chat assistant
   - Function calling enabled for database operations
   - Can:
     - List and filter expenses
     - Create new expenses
     - Submit, approve, or reject expenses
     - Query users and categories
   - Provides natural language interface to the system

### 5. **Azure AI Search** (Optional)
   - Enables RAG (Retrieval-Augmented Generation)
   - Can index documentation and provide context to AI
   - Enhances chat responses with relevant information

## Data Flow

1. **User Authentication**: Users access the web application via browser
2. **App Service**: Processes requests and routes to appropriate controllers
3. **Database Access**: App Service uses Managed Identity to connect to SQL Database
4. **Stored Procedures**: All CRUD operations executed via stored procedures
5. **AI Chat** (Optional): 
   - User sends natural language query
   - Chat Controller forwards to ChatService
   - ChatService calls Azure OpenAI with function definitions
   - OpenAI decides which functions to call
   - Functions execute via existing services (ExpenseService, UserService, etc.)
   - Results formatted and returned to user

## Security Features

- **No Passwords**: All authentication via Managed Identity and Azure AD
- **Azure AD-Only**: SQL Server configured for Azure AD authentication only
- **HTTPS Only**: All traffic encrypted in transit
- **Firewall Rules**: SQL Server accessible only from Azure services and approved IPs
- **Role-Based Access**: Managed Identity has minimum required permissions
- **Connection String Security**: No credentials in connection strings

## Deployment Options

### Option 1: Basic Deployment (deploy.sh)
- Deploys: App Service + Managed Identity + Azure SQL
- Cost: ~$70-100/month
- Use case: Core expense management functionality

### Option 2: Full Deployment (deploy-with-chat.sh)
- Deploys: All basic components + Azure OpenAI + AI Search
- Cost: ~$200-300/month
- Use case: AI-powered expense management with chat assistant

## Scalability

- **App Service**: Can scale up to higher SKUs (P1V2, P2V2, etc.) or scale out with multiple instances
- **SQL Database**: Can scale to higher tiers (Standard, Premium) for better performance
- **OpenAI**: Capacity can be increased beyond 8 for higher throughput
- **Search**: Can scale to higher tiers with more replicas and partitions

## Best Practices Implemented

✅ Azure Well-Architected Framework principles
✅ Managed Identity for security (no secrets)
✅ Infrastructure as Code (Bicep)
✅ Automated deployment scripts
✅ Stored procedures for data access
✅ API-first design with Swagger documentation
✅ Modern, responsive UI design
✅ Comprehensive error handling
✅ Logging and monitoring ready
✅ HTTPS-only communication
✅ Cross-platform deployment scripts (Mac/Linux/Windows)
