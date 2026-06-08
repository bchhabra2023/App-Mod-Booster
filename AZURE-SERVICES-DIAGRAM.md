# Azure Services Diagram

```
Azure Resource Group (uksouth)
├── App Service (S1) ──────────────► Azure SQL (Basic)
│   └── User-Assigned MI             └── Northwind DB
│         │
│         ▼ (Optional - deploy-with-chat.sh)
│   Azure OpenAI (swedencentral)
│   └── GPT-4o model
│
└── AI Search (uksouth)
    └── Expense index

Connections:
- App Service → SQL: Managed Identity (AAD auth)
- App Service → OpenAI: Managed Identity (Cognitive Services OpenAI User role)
- Chat UI → OpenAI: function calling + RAG
- AI Search → OpenAI: RAG augmentation
```
