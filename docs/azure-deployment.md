# Azure Deployment Guide — Microsoft 365 Request Manager

This document provides instructions for deploying the **Microsoft 365 Request & Document Manager** application to production hosting in Azure.

---

## 1. Hosting Architecture & Cost Control

To keep Azure costs at $0 (or minimal usage charge), we use the following serverless stack:
1. **Frontend Hosting:** Azure Static Web Apps (Free Tier). Offers free SSL, custom domain support, and global CDN deployment.
2. **Backend API:** Azure Functions (Consumption Plan / Serverless). Charges only for execution time; the first 1 million requests per month are free.
3. **Application Storage:** SharePoint Online lists and libraries. Stored entirely within your existing Microsoft 365 license footprint—generating $0 in new Azure Storage/Database fees.

---

## 2. Deploying Frontend: Azure Static Web Apps

1. Push your React project code to GitHub or Azure DevOps.
2. Go to the Azure Portal and create a new **Static Web App**.
3. Select your repository (GitHub/ADO) and configure the build settings:
   * **Build Preset:** `Vite`
   * **App location:** `/src/RequestManager.Frontend`
   * **Api location:** (Leave blank)
   * **Output location:** `dist`
4. In the Static Web App dashboard under **Configuration**, add the following **Application settings**:
   * `VITE_ENTRA_CLIENT_ID`
   * `VITE_ENTRA_TENANT_ID`
   * `VITE_API_BASE_URL` (Set to the deployed Azure Function URL)
5. Copy the generated Static Web App default URL (e.g. `https://proud-stone-12345.azurestaticapps.net`).

---

## 3. Deploying Backend: Azure Functions

1. Build the C# project in release mode:
   ```bash
   cd src/RequestManager.Functions
   dotnet publish -c Release
   ```
2. Create an **Azure Function App** in the Azure Portal:
   * **Runtime Stack:** `.NET 8 Isolated`
   * **Operating System:** `Windows` (or `Linux`)
   * **Plan Type:** `Consumption (Serverless)`
3. Deploy the compiled package using VS Code Azure Extensions, GitHub Actions, or CLI zip deploy.
4. Set the **Application Settings** under Configuration in the Azure Portal:
   * `MicrosoftTenantId`
   * `MicrosoftClientId`
   * `MicrosoftClientSecret` (stored in Key Vault in production)
   * `SharePointSiteId`
   * `SharePointDriveId`
   * `SharePointRequestsListId`
   * `SharePointCategoriesListId`
   * `SharePointCommentsListId`
   * `SharePointApprovalsListId`

---

## 4. Production Security Hardening

### Key Vault Secrets Management
Do not store the `MicrosoftClientSecret` in cleartext application settings:
1. Create an **Azure Key Vault** resource.
2. Store the secret in Key Vault as `EntraClientSecret`.
3. Enable **System-Assigned Managed Identity** on your Azure Function app.
4. In Key Vault, create an access policy granting the Function app's identity `Get` permissions for secrets.
5. Reference the Key Vault secret in the Function's App Settings:
   ```text
   @Microsoft.KeyVault(SecretUri=https://your-key-vault.vault.azure.net/secrets/EntraClientSecret/)
   ```

### CORS Configuration
In the Azure Function App:
1. Go to **CORS** in the left menu.
2. Remove any wildcard `*` values.
3. Add your Static Web App URL: `https://proud-stone-12345.azurestaticapps.net`.
4. Check **Enable Access-Control-Allow-Credentials**.
5. Save the configuration.
