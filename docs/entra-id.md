# Microsoft Entra ID App Registrations — Microsoft 365 Request Manager

This document provides step-by-step instructions for registering and configuring the application identities in **Microsoft Entra ID** to support Single Sign-On (SSO) and the OAuth 2.0 On-Behalf-Of (OBO) flow.

---

## 🏗️ Authentication Topology

We register **two separate apps** to maintain a clean security boundary:
1. **Frontend Client Application (SPA):** Authenticates the user silently inside Teams or Outlook via MSAL.js, obtaining an ID token.
2. **Backend Web API Application (Web):** Validates the frontend's token and exchanges it for a Microsoft Graph access token to read/write SharePoint data.

---

## 1. Registering the Backend Web API Application

1. Open the [Microsoft Entra Admin Center](https://entra.microsoft.com/) as a Global Administrator.
2. Go to **Identity** -> **Applications** -> **App registrations** -> **New registration**.
3. Configure the application:
   * **Name:** `M365 Request Manager Backend`
   * **Supported account types:** Accounts in this organizational directory only (Single Tenant)
   * **Redirect URI:** Select **Web** and set to `https://localhost:7071/.auth/login/aad/callback` (or your deployed Azure Function callback).
4. Click **Register** and note down the **Application (client) ID** and **Directory (tenant) ID**.

### A. Expose an API (Configure Scopes)
1. In the app registration, select **Expose an API**.
2. Click **Set** next to the Application ID URI (defaults to `api://YOUR_BACKEND_CLIENT_ID`).
3. Click **Add a scope** and configure:
   * **Scope name:** `access_as_user`
   * **Who can consent:** Admins and users
   * **Admin consent display name:** `Access M365 Request Manager Backend`
   * **Admin consent description:** `Allows the React client app to access the backend APIs on behalf of the logged-in user.`
4. Click **Add scope**.

### B. Configure Microsoft Graph API Permissions
1. Select **API permissions** -> **Add a permission**.
2. Choose **Microsoft Graph** -> **Delegated permissions**.
3. Select and add the following permissions:
   * `User.Read` (Read user profile and display name)
   * `Sites.ReadWrite.All` (Read and write SharePoint lists and document libraries)
4. Click **Grant admin consent for [Your Tenant Name]** to authorize these permissions tenant-wide.

### C. Generate Client Secret
1. Select **Certificates & secrets** -> **New client secret**.
2. Set Description to `Local Dev Secret` and set Expiry.
3. Click **Add** and immediately copy the **Value** (not the ID).

---

## 2. Registering the Frontend SPA Application

1. Return to **App registrations** -> **New registration**.
2. Configure the application:
   * **Name:** `M365 Request Manager Frontend`
   * **Supported account types:** Single Tenant
   * **Redirect URI:** Select **Single-page application (SPA)** and set to:
     * `http://localhost:5173` (Local React port)
     * Your deployed Azure Static Web Apps URL (e.g. `https://proud-stone-12345.azurestaticapps.net`)
3. Click **Register** and note down the **Application (client) ID**.

### A. Authorize Frontend in Backend API
You must pre-authorize the frontend app client ID so that users aren't prompted for consent when opening Teams:
1. Open the **Backend App Registration** (`M365 Request Manager Backend`).
2. Go to **Expose an API**.
3. Under **Authorized client applications**, click **Add a client application**.
4. Enter the **Frontend Client ID** and check the box for the custom scope (`api://YOUR_BACKEND_CLIENT_ID/access_as_user`).
5. Click **Add application**.
