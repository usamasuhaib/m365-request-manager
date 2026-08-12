# Security Specifications — Microsoft 365 Request Manager

This document defines the security parameters, validation requirements, and secrets management configurations applied across the frontend and backend.

---

## 1. Backend JWT Token Validation

The Azure Functions backend is completely stateless and secures all HTTP endpoints (except `/api/health`) by validating the incoming Entra ID JSON Web Token (JWT).

The function's token validation middleware performs the following checks:
1. **Signature Verification:** Validates that the token was signed by Microsoft Entra ID using the public keys obtained from the OpenID Connect discovery endpoint:
   `https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration`
2. **Issuer Check:** Confirms the issuer (`iss`) claim matches:
   `https://login.microsoftonline.com/{tenantId}/v2.0`
3. **Audience Check:** Confirms the audience (`aud`) claim matches your registered Backend Application ID URI:
   `api://YOUR_BACKEND_APP_CLIENT_ID`
4. **Lifetime Check:** Verifies that the token has not expired (`exp` claim is in the future) and is active (`nbf` check).

---

## 2. Cross-Origin Resource Sharing (CORS)

To prevent cross-site scripting (XSS) and domain hijack attacks:
- **Wildcard Block:** Wildcard origins (`*`) are strictly blocked in production.
- **Explicit Whitelisting:** The backend Azure Function must configure CORS to allow only the Static Web Apps URL (e.g. `https://proud-stone-12345.azurestaticapps.net`).
- **Credentials Support:** Enable CORS Credentials (`Access-Control-Allow-Credentials: true`) to support cross-domain session handshakes if needed.

---

## 3. File Upload Sanitization & Validation

File uploads present a major security vector (e.g. malware, trojans, executable injections). We implement two layers of defense:

### Client-Side Validation
The React file input runs check handlers before calling the upload API:
* **Extension limit:** Rejects any extension not in: `.pdf`, `.docx`, `.png`, `.jpg`, `.jpeg`.
* **Size limit:** File sizes larger than 10MB are blocked immediately.

### Backend Validation & Proxying
* **Mime-type inspection:** The Azure Function checks the stream's binary signature to ensure the content type matches the declared extension (preventing renaming an `.exe` file to `.jpg`).
* **Path Sanitization:** File names are stripped of special characters and directory traversal indicators (e.g., `../`).
* **Isolated Folders:** Files are stored in request-specific subfolders (`REQ-XXXXX/`) in SharePoint.
* **No Direct File URLs:** The client never receives a direct link to the file inside SharePoint. Downloads are routed through:
  `GET /api/requests/{id}/documents/{docId}`
  This proxy endpoint validates the user's JWT token context before fetching the file stream from SharePoint, preventing anonymous document link sharing.

---

## 4. Secrets Management

- **Zero Git Secrets:** No secrets, keys, or tenant-specific client credentials must ever be committed to source repositories. The `.gitignore` file must explicitly include `local.settings.json` and `.env`.
- **Azure Key Vault:** In staging and production, Client Secrets are stored in Key Vault, and the Azure Function uses System-Assigned Managed Identity to reference them dynamically.
- **Least Privilege Permissions:** The Backend App Registration asks strictly for delegated Graph scopes, preventing the application from reading data outside the logged-in user's organizational boundary.
