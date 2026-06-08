```mermaid
flowchart LR
    U[User Browser] --> A[Azure App Service\nRazor UI + API + Chat API]
    C[Chat UI] --> A
    A --> MI[User Assigned Managed Identity]
    A --> SQL[(Azure SQL Northwind)]
    A --> AOAI[Azure OpenAI\nGPT-4o swedencentral]
    A --> AIS[Azure AI Search]
    MI --> SQL
    MI --> AOAI
    MI --> AIS
```
