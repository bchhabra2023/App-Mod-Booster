![Header image](https://github.com/DougChisholm/App-Mod-Booster/blob/main/repo-header-booster.png)

# App-Mod-Booster

A project to show how GitHub coding agent can turn screenshots of a legacy app into a working proof-of-concept for a cloud native Azure replacement if the legacy database schema is also provided.

## Quick Start - Modernize Your App

1. Fork this repo 
2. In new repo replace the screenshots and sql schema (or keep the samples)
3. Open the coding agent and use app-mod-booster agent telling it "modernise my app"
4. When the app code is generated (can take up to 30 minutes) there will be a pull request to approve.
5. Now you can use codespaces to deploy the app to azure (or open VS Code and clone the repo locally - you will need to install some tools locally or use the devcontainer)
6. Open terminal and type "az login" to set subscription/context
7. Then type "bash deploy.sh" to deploy the app and db or "bash deploy-with-chat.sh" to deploy the app, db and chat UI.

Supporting slides for Microsoft Employees:
[Here](<https://microsofteur-my.sharepoint.com/:p:/g/personal/dchisholm_microsoft_com/IQAY41LQ12fjSIfFz3ha4hfFAZc7JQQuWaOrF7ObgxRK6f4?e=p6arJs>)

---

# Expense Management System - Generated Application

This repository contains a fully functional, modern expense management system generated from legacy application screenshots.

## 🌟 What Was Generated

- ✅ **Infrastructure as Code**: Bicep templates for all Azure resources
- ✅ **ASP.NET Core 8.0 Application**: Modern web app with Razor Pages and REST APIs
- ✅ **Database Setup**: Schema, stored procedures, and setup scripts
- ✅ **Modern UI**: Clean, responsive design matching the legacy app functionality
- ✅ **AI Chat Assistant**: Optional Azure OpenAI-powered chat interface
- ✅ **Complete Deployment Scripts**: Automated deployment to Azure
- ✅ **API Documentation**: Swagger/OpenAPI documentation
- ✅ **Security**: Managed Identity, Azure AD-only authentication

## 🚀 Deployment Options

### Option 1: Basic Deployment (App + Database)
```bash
az login
bash deploy.sh
```
Deploys: App Service, SQL Database, Managed Identity (~$70-100/month)

### Option 2: Full Deployment (With AI Chat)
```bash
az login
bash deploy-with-chat.sh
```
Deploys: Everything + Azure OpenAI + AI Search (~$200-300/month)

## 📱 Features

### Core Functionality
- Create, view, update, and delete expenses
- Submit expenses for approval
- Approve or reject expenses (Manager role)
- Filter by status (Draft, Submitted, Approved, Rejected)
- Categories: Travel, Meals, Supplies, Accommodation, Other
- User and role management

### Technical Features
- REST APIs with full CRUD operations
- Stored procedures for all database access
- Managed Identity authentication (no passwords!)
- Azure AD-only SQL authentication (MCAPS compliant)
- Modern, responsive UI
- Error handling with graceful degradation
- Swagger/OpenAPI documentation at `/swagger`

### AI Features (Optional)
- Natural language chat interface
- Function calling to database
- Can create, view, and manage expenses via chat
- Retrieval-Augmented Generation (RAG) support

## 📚 Documentation

- **[ARCHITECTURE.md](ARCHITECTURE.md)**: Detailed architecture diagrams and component descriptions
- **Application URL**: Will be displayed after deployment
- **API Docs**: Available at `<your-app-url>/swagger`

## 🏗️ What's Included

```
├── infrastructure/              # Bicep IaC templates
│   ├── main.bicep
│   └── modules/
│       ├── app-service.bicep
│       ├── azure-sql.bicep
│       └── genai.bicep
├── src/ExpenseManagement/       # ASP.NET Core 8.0 app
│   ├── Controllers/             # REST API controllers
│   ├── Services/                # Business logic with AI
│   ├── Models/                  # Data models
│   ├── Pages/                   # Razor Pages UI
│   └── wwwroot/                 # CSS, JavaScript
├── Database-Schema/             # Original SQL schema
├── stored-procedures.sql        # All stored procedures
├── run-sql*.py                  # Database setup scripts
├── deploy.sh                    # Basic deployment
├── deploy-with-chat.sh          # Full deployment with AI
└── ARCHITECTURE.md              # Architecture documentation
```

## 🔐 Security

- **Managed Identity**: No passwords or connection strings with credentials
- **Azure AD-Only Auth**: SQL Server complies with governance policies
- **HTTPS Only**: All traffic encrypted
- **Stored Procedures**: Protection against SQL injection
- **Minimal Permissions**: Managed Identity has only required roles

## 🛠️ Prerequisites

- Azure subscription
- Azure CLI (`az login`)
- .NET 8.0 SDK (for local dev)
- Python 3 with pip
- ODBC Driver 18 for SQL Server
- jq (JSON processor)

See [ARCHITECTURE.md](ARCHITECTURE.md) for installation instructions.

## 📊 Generated from Legacy App

Original legacy application screenshots are in `Legacy-Screenshots/`:
- Expense entry form
- Expense list view
- Approval interface

The modern app replicates and enhances this functionality with:
- Cloud-native architecture
- Modern, responsive UI
- REST APIs
- AI-powered chat interface
- Secure Azure AD authentication
- Automated deployment

## 🎯 Azure Best Practices

Implements best practices from Microsoft Learn:
- Infrastructure as Code (Bicep)
- Managed Identity for authentication
- Azure AD-only database authentication
- API design with Swagger/OpenAPI
- Monitoring and diagnostics ready
- Retry and transient fault handling
- Proper error handling and logging

## 💡 Local Development

1. Run `az login`
2. Update `src/ExpenseManagement/appsettings.json` with your database connection
3. Use `Authentication=Active Directory Default` for local development
4. Run `dotnet run` from `src/ExpenseManagement/`
5. Access at https://localhost:5001/Index

## 🐛 Troubleshooting

**Database connection issues?**
- Verify Managed Identity is configured
- Check firewall rules allow your IP
- Run `python3 run-sql-dbrole.py` to set up database permissions

**AI Chat not working?**
- Ensure you deployed with `deploy-with-chat.sh`
- Wait 30 seconds after deployment for role assignments to propagate
- Check App Service settings include OpenAI configuration

**Can't access the app?**
- Application URL ends with `/Index` not just the root URL
- Check deployment output for the correct URL
- Verify App Service is running in Azure Portal

## 📞 Support

For issues with:
- **The generator**: See main repository documentation
- **Deployed app**: Check [ARCHITECTURE.md](ARCHITECTURE.md) and Azure Portal logs
- **Azure services**: Consult Azure documentation

---

**This application was automatically generated by App-Mod-Booster** 🚀
