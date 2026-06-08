![Header image](https://github.com/DougChisholm/App-Mod-Booster/blob/main/repo-header-booster.png)

# App-Mod-Booster
A project to show how GitHub coding agent can turn screenshots of a legacy app into a working proof-of-concept for a cloud native Azure replacement if the legacy database schema is also provided.

## Deployment

1. Set environment variables and login:
   - `az login`
   - `export RESOURCE_GROUP=<your-rg>`
   - `export ADMIN_UPN=<your-upn>`
   - `export ADMIN_OBJECT_ID=<your-object-id>`
2. Deploy app + db:
   - `bash deploy.sh`
3. Deploy app + db + chat/genai:
   - `bash deploy-with-chat.sh`
4. One-line summary deploy script:
   - `bash deploy-all.sh`

## App URL
After deployment, use `<app-url>/Index` (not the root URL).

## Local run with managed identity auth
For local development, set connection string auth to `Authentication=Active Directory Default` and sign in with `az login`.

## Architecture
See `azure-services-diagram.md`.
