# Local Development Guide — Microsoft 365 Request Manager

This document explains how to run, debug, and test the React frontend and C# Azure Functions backend locally, integrated with Microsoft Teams and Outlook shells.

---

## 1. Local Networking & HTTPS Tunnels

Teams and Outlook apps load content inside iframe containers that strictly require **HTTPS**. Since local servers run on `localhost` HTTP, you must set up an HTTPS proxy tunnel to test the M365 integration.

### Option A: VS Code Dev Tunnels (Recommended)
VS Code has built-in port forwarding:
1. Open the **Ports** panel in VS Code.
2. Click **Forward a Port** and enter `7071` (Backend) and `5173` (Frontend).
3. Right-click the port and set **Port Visibility** to **Public**.
4. Copy the generated HTTPS forwarding URLs.

### Option B: DevTunnel CLI
Alternatively, run the Microsoft DevTunnel CLI tool:
```bash
# Log in to your Azure/Microsoft Account
devtunnel user login

# Host backend tunnel
devtunnel host -p 7071 --allow-anonymous

# Host frontend tunnel
devtunnel host -p 5173 --allow-anonymous
```

---

## 2. Configuration Files Setup

### Backend: `src/RequestManager.Functions/local.settings.json`
Create a `local.settings.json` file in the backend root directory. This contains Entra ID app credentials and SharePoint identifiers. **Do not commit this file to git.**

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "MicrosoftTenantId": "YOUR_M365_TENANT_ID",
    "MicrosoftClientId": "YOUR_BACKEND_APP_CLIENT_ID",
    "MicrosoftClientSecret": "YOUR_BACKEND_APP_CLIENT_SECRET",
    "SharePointSiteId": "YOUR_SHAREPOINT_SITE_ID",
    "SharePointDriveId": "YOUR_DOCUMENT_LIBRARY_DRIVE_ID",
    "SharePointRequestsListId": "YOUR_REQUESTS_LIST_ID",
    "SharePointCategoriesListId": "YOUR_CATEGORIES_LIST_ID",
    "SharePointCommentsListId": "YOUR_COMMENTS_LIST_ID",
    "SharePointApprovalsListId": "YOUR_APPROVALS_LIST_ID"
  },
  "Host": {
    "LocalHttpPort": 7071,
    "CORS": "https://localhost:5173,https://your-frontend-devtunnel.rel.tunnels.api.visualstudio.com",
    "CORSCredentials": true
  }
}
```

### Frontend: `src/RequestManager.Frontend/.env`
Create a `.env` file in the React project root:

```env
VITE_ENTRA_CLIENT_ID="YOUR_FRONTEND_APP_CLIENT_ID"
VITE_ENTRA_TENANT_ID="YOUR_M365_TENANT_ID"
VITE_API_BASE_URL="https://your-backend-devtunnel.rel.tunnels.api.visualstudio.com"
```

---

## 3. Launching the Application

### Start the Backend API
Navigate to the Functions folder and launch the Azure Functions host:
```powershell
cd src/RequestManager.Functions
dotnet restore
dotnet build
func start
```
Verify the server is running by opening `GET http://localhost:7071/api/health` in your browser.

### Start the Frontend React SPA
Navigate to the Frontend folder and run Vite:
```powershell
cd src/RequestManager.Frontend
npm install
npm run dev
```
Open the local dev server (default `http://localhost:5173`) to confirm the app renders correctly outside Teams.

---

## 4. Testing inside Microsoft Teams

1. Make sure your local frontend devtunnel URL matches the tab content source URL inside your `manifest.json`.
2. Package your manifest (sideloading steps in **[teams-setup.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/teams-setup.md)**).
3. Open Microsoft Teams Web App, click **Apps** -> **Manage your apps** -> **Upload an app** -> **Upload a custom app** and select your zipped manifest package.
4. Launch the application tab and open Developer Tools (`F12`) to inspect console logs and traffic.
