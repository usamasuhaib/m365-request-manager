# Setup & Configuration Checklist — Microsoft 365 Request Manager

This document provides a master step-by-step guide to configuring the environment for the **Microsoft 365 Request & Document Manager** application.

---

## 🛠️ Prerequisites

Before starting, ensure you have access to:
1. **Microsoft 365 Developer Tenant:** With Global Administrator access (join the [Microsoft 365 Developer Program](https://developer.microsoft.com/microsoft-365/dev-program) if you don't have one).
2. **Azure Subscription:** With permissions to register resources and configure Managed Identity/Static Web Apps.
3. **Local Dev Environment:**
   * Node.js v18+ and npm
   * .NET 8.0 SDK
   * Azure Functions Core Tools v4 (install via `npm install -g azure-functions-core-tools@4`)
   * VS Code with C# Dev Kit and Azure Functions extensions

---

## 📋 Master Setup Checklist

Follow these steps in sequence to get the system operational. Refer to the corresponding sub-documents for specific configuration details:

### Step 1: Microsoft Entra ID Registrations
You must register two separate applications in your tenant's Entra ID directory:
1. **Backend Web API App Registration:** Exposes the API endpoints, defines custom user scopes (`access_as_user`), and requests Microsoft Graph delegated permissions (`Sites.ReadWrite.All`, `User.Read`).
2. **Frontend App Registration:** Represents the React application running in Teams and Outlook. Configures single-tenant SPA settings and references the Backend App's scope.
   * *Detailed instructions:* **[entra-id.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/entra-id.md)**

### Step 2: SharePoint Site & Schema Provisioning
Provision the data store inside your Microsoft 365 tenant:
1. Create a SharePoint Communication or Team site named `M365 Request Manager`.
2. Provision five Lists: `Requests`, `RequestCategories`, `RequestComments`, `RequestApprovals`, and `AppSettings`.
3. Configure site column fields (lookups, text notes, dates).
4. Create a Document Library named `RequestDocuments`.
   * *Detailed instructions:* **[sharepoint-setup.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/sharepoint-setup.md)**

### Step 3: Local Configuration Files
Configure your credentials locally (make sure these are never committed to git):
1. **Backend configuration:** Create `local.settings.json` in the backend project directory. Set Tenant ID, Backend Client ID, Client Secret, and SharePoint IDs.
2. **Frontend configuration:** Create `.env` in the frontend React root. Set Client ID, API baseUrl, and Tenant ID.
   * *Detailed instructions:* **[local-development.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/local-development.md)**

### Step 4: Build & Sideload Manifests
Create and upload the Teams/Outlook unified app package:
1. Edit `manifest/manifest.json` with your Backend ID, Frontend URL, and scopes.
2. Generate the app package zip (including icon assets).
3. Upload to the Microsoft Teams Admin Center or sideload directly in Outlook and Teams web app.
   * *Detailed instructions:* **[teams-setup.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/teams-setup.md)** and **[outlook-setup.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/outlook-setup.md)**

### Step 5: Test and Verify
1. Run local dev tunnels (e.g. `devtunnel` or `ngrok`) to expose your backend over HTTPS.
2. Run frontend and backend servers.
3. Open Teams, launch the tab, and submit your first request to verify list item and document creations.
   * *Detailed instructions:* **[testing.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/testing.md)**
