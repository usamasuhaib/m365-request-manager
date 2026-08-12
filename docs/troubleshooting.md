# Troubleshooting & Diagnostics — Microsoft 365 Request Manager

This document provides solutions for common issues encountered during local development, deployment, and testing.

---

## 1. Authentication & Teams SSO Errors

### 🔴 Error: `resource_disabled` or `Authentication failed` in Teams
* **Symptom:** The React client fails to acquire a silent SSO token from Teams.
* **Cause:** The Backend App Registration has not authorized the Frontend SPA client application ID.
* **Resolution:**
  1. Open the **Backend App Registration** in Entra ID.
  2. Go to **Expose an API**.
  3. Under **Authorized client applications**, confirm that the Frontend SPA Application Client ID is listed and mapped to scope `access_as_user`.

### 🔴 Error: `consent_required` or `admin_consent_required`
* **Symptom:** Calling the backend API returns a Graph access failure.
* **Cause:** The delegated Graph permissions (`Sites.ReadWrite.All`, `User.Read`) require administrative approval which has not been granted for the tenant.
* **Resolution:**
  1. Open the **Backend App Registration** in Entra ID.
  2. Go to **API Permissions**.
  3. Click **Grant admin consent for [Tenant Name]** and wait for the status to show green checkboxes.

### 🔴 Symptom: Infinite Redirect Loop on Login
* **Symptom:** The browser flips back and forth between Entra ID login pages.
* **Cause:** MSAL is attempting silent token acquisition, failing, and immediately triggering a redirect without waiting for user action.
* **Resolution:**
  1. Catch SSO errors inside the React application core.
  2. Do not auto-redirect on failure.
  3. Render a clean fallback landing page containing a **"Sign In"** button. The redirect must only trigger on user click.

---

## 2. Microsoft Graph & SharePoint Failures

### 🔴 Error: `403 Forbidden` on List Operations
* **Symptom:** The Azure Function returns `403 Forbidden` when attempting to write a list item or upload a document.
* **Cause:**
  1. The delegated user does not have permission to access the SharePoint site.
  2. Or, the app registration scopes are missing `Sites.ReadWrite.All`.
* **Resolution:**
  1. Verify the user can manually create a folder/item inside the SharePoint site.
  2. Verify the Backend App Registration permissions list contains `Sites.ReadWrite.All` (delegated).

### 🔴 Error: `429 Too Many Requests`
* **Symptom:** API calls fail, and Graph returns a throttling warning.
* **Resolution:**
  * Configure your C# Graph client handler with exponential backoff.
  * Ensure the React frontend is not polling the `/api/requests` endpoint in a tight loop. Cache metrics locally inside React context during the active session.

---

## 3. Local Development Tunnels & Network Errors

### 🔴 Symptom: Frontend fails to connect to Azure Function locally
* **Symptom:** The browser console shows `ERR_CONNECTION_REFUSED` or CORS blocking errors.
* **Resolution:**
  1. Check if the local Azure Functions host is active (terminal running `func start`).
  2. Verify that the frontend `.env` contains the exact HTTPS devtunnel URL of the backend (not `localhost`).
  3. Open `local.settings.json` on the backend and verify that the `CORS` setting whitelists the frontend devtunnel URL.

### 🔴 Symptom: SSL Certificate Warnings in Browser
* **Symptom:** The browser blocks local API calls with certificate warnings.
* **Resolution:**
  Trust the local dotnet developer certificate:
  ```powershell
  dotnet dev-certs https --clean
  dotnet dev-certs https --trust
  ```
  Restart your browser and terminal after running this command.
