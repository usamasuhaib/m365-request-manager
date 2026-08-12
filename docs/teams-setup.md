# Teams Application Sideloading Guide — Microsoft 365 Request Manager

This document explains how to configure the unified app manifest, package it, and sideload it inside Microsoft Teams.

---

## 1. Unified App Manifest Structure (`manifest.json`)

Create a directory named `manifest/` at the root of your workspace and place the following `manifest.json` file inside it. Update all placeholders (`YOUR_*`) with your actual Entra ID Client IDs and Hosting URLs.

```json
{
  "$schema": "https://developer.microsoft.com/en-us/json-schemas/teams/v1.16/MicrosoftTeams.schema.json",
  "manifestVersion": "1.16",
  "version": "1.0.0",
  "id": "e7b0e14a-717b-402a-9e12-c2b64d5fa3f9",
  "packageName": "com.solvefy.m365requestmanager",
  "developer": {
    "name": "Solvefy",
    "websiteUrl": "https://solvefy.com",
    "privacyUrl": "https://solvefy.com/privacy",
    "termsOfUseUrl": "https://solvefy.com/terms"
  },
  "icons": {
    "color": "color.png",
    "outline": "outline.png"
  },
  "name": {
    "short": "Request Manager",
    "full": "Microsoft 365 Request & Document Manager"
  },
  "description": {
    "short": "Manage requests and documents natively inside M365",
    "full": "A proof of concept demo showing native Microsoft 365 tab and email add-in integration with SharePoint and Azure Functions."
  },
  "accentColor": "#0078d4",
  "configurableTabs": [],
  "staticTabs": [
    {
      "entityId": "dashboardTab",
      "name": "Request Dashboard",
      "contentUrl": "https://your-frontend-domain.azurestaticapps.net/index.html#/dashboard?name={loginHint}",
      "websiteUrl": "https://your-frontend-domain.azurestaticapps.net/index.html#/dashboard",
      "scopes": ["personal"]
    }
  ],
  "permissions": [
    "identity",
    "messageBacklog"
  ],
  "validDomains": [
    "your-frontend-domain.azurestaticapps.net",
    "your-backend-domain.azurewebsites.net",
    "localhost"
  ],
  "webApplicationInfo": {
    "id": "YOUR_BACKEND_APP_CLIENT_ID",
    "resource": "api://YOUR_BACKEND_APP_CLIENT_ID"
  }
}
```

---

## 2. Packaging App Icons

To create a valid Teams app package, you must include two PNG icons in the `manifest/` directory alongside `manifest.json`:
1. **`color.png`:** A full-color square icon, size **96x96 pixels**.
2. **`outline.png`:** A white/transparent outline icon, size **32x32 pixels**.

Once these files are in place, compress the directory contents (do not compress the parent folder, compress the files directly inside it) into a zip file named `app-package.zip`:
```powershell
# PowerShell script to create the package
Compress-Archive -Path .\manifest\manifest.json, .\manifest\color.png, .\manifest\outline.png -DestinationPath .\app-package.zip -Force
```

---

## 3. Sideloading into Microsoft Teams

### Web App Testing
1. Navigate to the [Microsoft Teams Web Client](https://teams.microsoft.com/) in your browser.
2. Select **Apps** in the bottom left corner.
3. Click **Manage your apps** -> **Upload an app** -> **Upload a custom app**.
4. Select the `app-package.zip` file you created.
5. Click **Add** to install the tab app in your personal workspace.

### Admin Center Deployment (Production Prep)
If you wish to make the app available to all tenant users:
1. Open the [Microsoft Teams Admin Center](https://admin.teams.microsoft.com/).
2. Navigate to **Teams apps** -> **Manage apps**.
3. Click **Upload new app** and upload `app-package.zip`.
4. Publish the app to your organization catalog.

---

## 4. Silent SSO Configuration & Token Validation

The `webApplicationInfo` object in the manifest is crucial. It tells Teams that when our React client calls `microsoftTeams.authentication.getAuthToken()`, the client is requesting an ID token from Entra ID for the Backend Client ID `YOUR_BACKEND_APP_CLIENT_ID`.

If Teams fails to return a token (e.g. error `resource_disabled` or `consent_required`), check the troubleshooting steps in **[troubleshooting.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/troubleshooting.md)**.
