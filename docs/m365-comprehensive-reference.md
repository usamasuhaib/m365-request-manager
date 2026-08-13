# Microsoft 365 Developer Ecosystem — Comprehensive Technical Reference

> **Scope:** General-purpose reference for any development team building integrations with Microsoft 365 APIs.  
> **Coverage:** Microsoft Graph, Entra ID authentication, SharePoint, Teams, Outlook, M365 subscriptions/plans, Copilot integrations, implementation patterns, known risks, and production blockers.  
> **Last Verified:** August 2026 | SDK versions pinned at end of document.

---

## Table of Contents

1. [The Microsoft 365 Platform Overview](#1-the-microsoft-365-platform-overview)
2. [Microsoft 365 Subscriptions & Plans](#2-microsoft-365-subscriptions--plans)
3. [Microsoft Entra ID & Authentication](#3-microsoft-entra-id--authentication)
4. [Microsoft Graph API](#4-microsoft-graph-api)
5. [SharePoint Online APIs](#5-sharepoint-online-apis)
6. [Microsoft Teams Platform APIs](#6-microsoft-teams-platform-apis)
7. [Outlook Add-in & Mail APIs](#7-outlook-add-in--mail-apis)
8. [Microsoft 365 Copilot Integrations](#8-microsoft-365-copilot-integrations)
9. [Cross-Cutting Concerns](#9-cross-cutting-concerns)
10. [Risks & Possible Blockers Reference](#10-risks--possible-blockers-reference)
11. [Quick Reference Cheat Sheet](#11-quick-reference-cheat-sheet)
12. [Pricing Tiers for M365 Apps](#12-pricing-tiers-for-m365-apps)

---

## 1. The Microsoft 365 Platform Overview

### 1.1 What Is the M365 Platform?
Microsoft 365 (formerly Office 365) is a cloud-based productivity ecosystem providing hosted services for communication, collaboration, and content. From a developer's perspective, it exposes APIs, SDKs, and extensibility frameworks that let you build apps that:

- Embed natively inside Teams, Outlook, Word, Excel, PowerPoint, or SharePoint
- Read and write data stored in users' mailboxes, calendars, drives, and chats
- Send notifications and trigger workflows using Microsoft-managed infrastructure
- Surface content and AI experiences through Microsoft 365 Copilot

### 1.2 Developer Entry Points

| Surface | Description | Primary SDK |
|---|---|---|
| **Microsoft Graph** | Unified REST API gateway for all M365 data | Graph .NET SDK v5 / `@microsoft/microsoft-graph-client` |
| **Teams Apps** | Tabs, Bots, Messaging Extensions, Meeting Apps, Agents | `@microsoft/teams-js` v2.x, Bot Framework SDK |
| **Outlook Add-ins** | Task panes and mail compose/read extensions | Office.js (Requirement Set 1.14+) |
| **SharePoint Framework (SPFx)** | Custom Web Parts and Extensions inside SharePoint | SPFx 1.20+ (Yeoman generator) |
| **Power Platform Connectors** | Low-code integrations for Power Apps / Automate | Custom Connector SDK |
| **Copilot Connectors** | Bring external data into Graph/Copilot for AI reasoning | Graph Connectors REST API / .NET SDK |
| **Teams AI Library** | Build intelligent, LLM-backed Teams bots and agents | `@microsoft/teams-ai` |

### 1.3 Fundamental Architecture Principle

All M365 APIs sit **behind Microsoft Entra ID**. Every API call requires a bearer token issued by Entra ID. There is no API-key-based access to Microsoft Graph — identity is always the gatekeeper.

```
Your App  →  Entra ID (OAuth 2.0)  →  Microsoft Graph  →  Teams / SharePoint / Outlook / OneDrive / Copilot
```

> 🔴 **RETIRED:** The **Azure AD Graph API** (`graph.windows.net`) was **fully and permanently retired on August 31, 2025**. Any code still using this legacy endpoint will fail. Migrate all calls to `graph.microsoft.com/v1.0`.

---

## 2. Microsoft 365 Subscriptions & Plans

Understanding the M365 licensing model is essential for developers — the plan determines which APIs return data and which features are even available at runtime.

### 2.1 Plan Tiers Overview

| Plan Family | Max Users | Key Capabilities |
|---|---|---|
| **Microsoft 365 Business Basic** | 300 | Web/mobile apps, Exchange, Teams, SharePoint, OneDrive |
| **Microsoft 365 Business Standard** | 300 | + Desktop Office apps, webinars |
| **Microsoft 365 Business Premium** | 300 | + Intune MDM, Entra ID P1, Defender for Business |
| **Microsoft 365 Enterprise E1** | Unlimited | Web Office, Teams, SharePoint, no desktop apps |
| **Microsoft 365 Enterprise E3** | Unlimited | + Desktop apps, compliance (DLP, retention), Entra P1 |
| **Microsoft 365 Enterprise E5** | Unlimited | + Advanced security (Defender P2), Power BI Pro, Entra P2, eDiscovery, Purview |
| **Microsoft 365 F1/F3** | Unlimited | Frontline worker plans (limited feature set) |
| **Microsoft 365 Copilot** | Add-on | Copilot AI layer on top of E3/E5 (requires E3 minimum) |

> ⚠️ **Business plans are hard-capped at 300 seats.** If you exceed this, you must migrate to Enterprise. Plan migration in your architecture if your target customers may grow.

### 2.2 API Access vs. License Tier

This is a common misconception: **the Microsoft Graph API itself is accessible on all tiers.** The licensing constraint is about *which data/features exist to query*, not about API access:

| Scenario | Requires |
|---|---|
| Call `GET /me/messages` | Any M365 plan with Exchange Online |
| Call `GET /me/drive` | Any plan with OneDrive |
| Call security or compliance APIs (e.g., eDiscovery, Purview) | E5 or E5 Compliance add-on |
| Use Copilot Connectors to surface data in Copilot chat | Microsoft 365 Copilot add-on license |
| Call advanced identity APIs (Identity Protection, Risky Users) | Entra ID P2 (included in E5) |
| Send Teams channel messages via Graph | Teams-enabled plan (E1, E3, E5, Business) |

### 2.3 Microsoft 365 Developer Program (Free Sandbox)

For development and testing, **do not use production tenant seats**. Use the Microsoft 365 Developer Program:

- **Free renewable Microsoft 365 E5 sandbox subscription** (90-day, auto-renewed with active use)
- Includes 25 pre-configured sample user accounts
- Full access to all E5 APIs including security, compliance, Teams, SharePoint
- Access at: [developer.microsoft.com/en-us/microsoft-365/dev-program](https://developer.microsoft.com/en-us/microsoft-365/dev-program)

### 2.4 Integration-Specific License Requirements

| Integration Type | Minimum License Needed |
|---|---|
| Teams Tab App | Microsoft 365 Business Basic or E1 |
| Outlook Add-in | Microsoft 365 Business Basic or E1 |
| SharePoint Lists & Files | Microsoft 365 Business Basic or E1 |
| Microsoft 365 Copilot Extensions | M365 Copilot add-on (requires E3 base) |
| Azure AD B2C (external users) | Separate Azure subscription (not M365) |
| Viva Connections (SharePoint home) | F3, E3, or E5 |
| Microsoft Teams Phone | Teams Phone add-on |

---

## 3. Microsoft Entra ID & Authentication

### 3.1 App Registration (Pre-requisite for Everything)

Every integration requires an **App Registration** in the Microsoft Entra portal (`portal.azure.com → Entra ID → App Registrations`):

| Field | Description |
|---|---|
| **Application (client) ID** | Identifies your app to Entra |
| **Tenant ID** | Identifies the M365 organization |
| **Client Secret / Certificate** | Used for daemon/server flows (prefer certificates in production) |
| **Redirect URIs** | Where Entra sends auth codes back |
| **API Permissions** | The Graph scopes your app can request |
| **Expose an API** | Required if you build your own backend that frontend tokens target |

> 🔒 **Security Best Practice:** Prefer **certificates** over client secrets in production. Secrets have a maximum lifetime of 2 years; certificates can be managed via Azure Key Vault rotation.

### 3.2 OAuth 2.0 Grant Types

#### 3.2.1 Authorization Code Flow + PKCE (Web Apps / SPAs)
The current recommended flow for web apps and SPAs. PKCE (Proof Key for Code Exchange) is **required** for public clients (SPAs without a backend):
```
1. Redirect user to:
   https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize
   ?client_id={CLIENT_ID}
   &response_type=code
   &redirect_uri={YOUR_REDIRECT_URI}
   &scope=openid profile email User.Read
   &code_challenge={PKCE_CHALLENGE}
   &code_challenge_method=S256
   &state={random_state}

2. Entra redirects back: ?code={AUTH_CODE}&state={state}

3. Exchange code for tokens:
   POST https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token
   Body: grant_type=authorization_code&code={AUTH_CODE}&client_id=...
         &code_verifier={PKCE_VERIFIER}&redirect_uri=...
```

#### 3.2.2 Client Credentials Flow (Server-to-Server / Daemon Apps)
For background jobs and services with no user context:
```http
POST https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id={CLIENT_ID}
&client_secret={CLIENT_SECRET}
&scope=https://graph.microsoft.com/.default
```

> ⚠️ **Risk:** Client Credentials tokens carry **Application permissions** — they act as the app, not as a specific user. This is powerful but bypasses user-level auditing. Use delegated flows wherever compliance allows.

#### 3.2.3 On-Behalf-Of (OBO) Flow (API Chaining)
Used when your backend API receives a user token from the frontend and needs to call Graph on that user's behalf:
```http
POST https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token

grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer
&client_id={BACKEND_CLIENT_ID}
&client_secret={BACKEND_CLIENT_SECRET}
&assertion={INCOMING_USER_TOKEN}
&requested_token_use=on_behalf_of
&scope=https://graph.microsoft.com/Sites.ReadWrite.All
```

#### 3.2.4 Teams Silent SSO (Tab Authentication)
Teams Tabs use a specialized silent SSO — the Teams host container fetches an Entra token on behalf of the already-signed-in user without any interactive prompt:
```typescript
import { app, authentication } from "@microsoft/teams-js";

await app.initialize();

// Capability check — always verify before using
if (authentication.isSupported()) {
  const token = await authentication.getAuthToken();
  // token is a JWT (audience = api://{CLIENT_ID}). Pass as Bearer to your backend.
}
```

> 📌 **SDK Change (v2.x):** The old `microsoftTeams.authentication.getAuthToken({ successCallback, failureCallback })` callback pattern is **deprecated**. Use the Promise-based API shown above.

### 3.3 Token Validation on the Backend

```csharp
// C# — Microsoft.IdentityModel.Tokens + System.IdentityModel.Tokens.Jwt
var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
    $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration",
    new OpenIdConnectConfigurationRetriever());

var config = await configManager.GetConfigurationAsync();

var validationParameters = new TokenValidationParameters
{
    ValidateAudience         = true,
    ValidAudience            = $"api://{clientId}",    // Must match Expose-an-API URI
    ValidateIssuer           = true,
    ValidIssuer              = $"https://login.microsoftonline.com/{tenantId}/v2.0",
    ValidateIssuerSigningKey = true,
    IssuerSigningKeys        = config.SigningKeys,      // Fetched dynamically from Entra
    ValidateLifetime         = true,
    ClockSkew                = TimeSpan.FromMinutes(5)
};

var handler   = new JwtSecurityTokenHandler();
var principal = handler.ValidateToken(incomingToken, validationParameters, out _);

// Key claims to extract:
var userEmail = principal.FindFirst("preferred_username")?.Value
             ?? principal.FindFirst(ClaimTypes.Upn)?.Value;
var userName  = principal.FindFirst("name")?.Value;
var oid       = principal.FindFirst("oid")?.Value;  // Immutable user object ID
```

### 3.4 Admin Consent vs. User Consent

| Type | When Required | Scope Examples |
|---|---|---|
| **User Consent** | App requests only user-specific data | `User.Read`, `Mail.Read`, `Calendars.Read` |
| **Admin Consent** | App requests tenant-wide or app-level data | `Mail.Read.All`, `Sites.ReadWrite.All`, `TeamMember.Read.All` |

> ⚠️ **Blocker:** Many production M365 tenants block user consent for any app not pre-approved by the IT admin. Always identify admin-consent-required scopes early and arrange for admin pre-approval before Go Live.

---

## 4. Microsoft Graph API

### 4.1 API Basics

| Property | Value |
|---|---|
| **Stable Base URL** | `https://graph.microsoft.com/v1.0` |
| **Preview Base URL** | `https://graph.microsoft.com/beta` — not for production |
| **Auth Header** | `Authorization: Bearer {access_token}` |
| **Content Type** | `application/json` |
| **Throttle Limit** | ~10,000 requests per user per 10 minutes (varies by resource) |
| **SDK Versions** | .NET: `Microsoft.Graph` v5.x | JS: `@microsoft/microsoft-graph-client` v3.x |

> 🔴 **RETIRED:** Azure AD Graph (`graph.windows.net`) was fully retired August 31, 2025. Use Microsoft Graph exclusively.

### 4.2 Key API Domains & Endpoints

#### User & Identity
```
GET /me                             → Current user profile
GET /me/photo/$value                → Profile photo binary
GET /users/{id|UPN}                 → Specific user
GET /me/memberOf                    → Groups the current user belongs to
GET /me/transitiveMemberOf          → All nested group memberships
```

#### Mail (Outlook)
```
GET  /me/messages                   → List mailbox messages
GET  /me/messages/{id}              → Read specific message
POST /me/messages                   → Draft new message
POST /me/sendMail                   → Send email immediately
GET  /me/mailFolders                → List mail folders
POST /me/mailFolders/{id}/messages  → Draft in specific folder
```

#### Calendar
```
GET  /me/events                     → List calendar events
POST /me/events                     → Create calendar event
GET  /me/calendar/calendarView?startDateTime=...&endDateTime=...  → Time-bounded view
```

#### Files (OneDrive)
```
GET  /me/drive/root/children        → List root files
PUT  /me/drive/root:/{path}:/content → Upload file (< 250 MB, simple)
POST /me/drive/root:/{path}:/createUploadSession → Chunked upload (any size)
GET  /me/drive/items/{id}/content   → Download file
```

> 📌 **Update:** The simple upload limit was raised from **4 MB to 250 MB** for `PUT` uploads. Upload Sessions remain the recommended path for large files.

#### SharePoint (via Graph)
```
GET  /sites/{siteId}                                    → Site metadata
GET  /sites/{siteId}/lists                              → Lists in a site
GET  /sites/{siteId}/lists/{listId}/items               → List items
POST /sites/{siteId}/lists/{listId}/items               → Create list item
PATCH /sites/{siteId}/lists/{listId}/items/{itemId}     → Update list item
DELETE /sites/{siteId}/lists/{listId}/items/{itemId}    → Delete item
GET  /sites/{siteId}/lists/{listId}/items/delta         → Changed items since last sync
```

#### Teams
```
GET  /me/joinedTeams                                         → Teams the user belongs to
GET  /teams/{teamId}/channels                                → Channels in a team
GET  /teams/{teamId}/channels/{channelId}/messages           → Messages in a channel
POST /teams/{teamId}/channels/{channelId}/messages           → Send channel message
POST /chats/{chatId}/messages                                → Send direct/group chat message
GET  /teams/{teamId}/members                                 → Team member list
POST /teams/{teamId}/channels                                → Create new channel
GET  /teams/{teamId}/channels/{channelId}/tabs               → List tabs in channel
```

#### Subscriptions (Webhooks / Change Notifications)
```
POST /subscriptions              → Create a webhook subscription
GET  /subscriptions              → List active subscriptions
PATCH /subscriptions/{id}        → Renew subscription (before expiry)
DELETE /subscriptions/{id}       → Remove subscription
```

### 4.3 Graph SDK Usage (C# / .NET)

**Installation:**
```xml
<PackageReference Include="Microsoft.Graph" Version="5.*" />
<PackageReference Include="Azure.Identity" Version="1.*" />
```

**Client Setup (Client Credentials / Managed Identity):**
```csharp
// Using DefaultAzureCredential — works with Managed Identity in Azure, 
// dev machine credentials locally (az login), and environment variables
var credential   = new DefaultAzureCredential();
var graphClient  = new GraphServiceClient(credential,
    new[] { "https://graph.microsoft.com/.default" });

var user = await graphClient.Users[userId].GetAsync();
Console.WriteLine(user?.DisplayName);
```

**Client Setup (On-Behalf-Of):**
```csharp
var oboCredential = new OnBehalfOfCredential(
    tenantId, clientId, clientSecret, incomingUserToken);
var graphClient = new GraphServiceClient(oboCredential,
    new[] { "https://graph.microsoft.com/.default" });
```

### 4.4 Pagination

Graph paginates large result sets. Always handle the `@odata.nextLink` property using the built-in `PageIterator`:
```csharp
var messages = await graphClient.Me.Messages.GetAsync(config =>
{
    config.QueryParameters.Top    = 50;
    config.QueryParameters.Select = new[] { "id", "subject", "from", "receivedDateTime" };
    config.QueryParameters.Filter = "isRead eq false";
});

var pageIterator = PageIterator<Message, MessageCollectionResponse>
    .CreatePageIterator(graphClient, messages, (msg) =>
    {
        Console.WriteLine(msg.Subject);
        return true; // return false to stop early
    });

await pageIterator.IterateAsync();
```

### 4.5 Delta Queries (Incremental Change Tracking)

Instead of polling all records, Graph delta queries return only items changed since your last sync token. Highly recommended for sync-heavy workloads:
```
GET /me/messages/delta
GET /me/calendarView/delta?startDateTime=...&endDateTime=...
GET /sites/{siteId}/lists/{listId}/items/delta
GET /users/delta
```

On the first call you get all items + a `@odata.deltaLink`. Store it. On the next call, use that link instead of the full endpoint to get only changes.

### 4.6 Graph Webhooks (Push Notifications / Subscriptions)

Instead of polling, subscribe to receive push notifications when data changes:

**Create subscription:**
```http
POST /v1.0/subscriptions
{
  "changeType": "created,updated,deleted",
  "notificationUrl": "https://yourapp.com/api/notifications",
  "resource": "/me/messages",
  "expirationDateTime": "2026-09-01T00:00:00Z",
  "clientState": "your-secret-state"
}
```

**Your notification endpoint receives:**
```json
{
  "value": [{
    "subscriptionId": "...",
    "changeType": "created",
    "resource": "/me/messages/{id}",
    "resourceData": { "id": "{message-id}" },
    "clientState": "your-secret-state"
  }]
}
```

> ⚠️ **Key Rule:** Subscriptions expire (max 3 days for mail, 60 min for Teams messages). Your service must **PATCH `/subscriptions/{id}`** before expiry to renew them, or re-subscribe.

### 4.7 $batch Requests (Reduce Round-Trips)

Send up to **20 individual Graph requests in a single HTTP call**:
```http
POST https://graph.microsoft.com/v1.0/$batch
Content-Type: application/json

{
  "requests": [
    { "id": "1", "method": "GET", "url": "/me" },
    { "id": "2", "method": "GET", "url": "/me/messages?$top=5" },
    { "id": "3", "method": "GET", "url": "/me/drive/root/children" }
  ]
}
```

---

## 5. SharePoint Online APIs

### 5.1 Two API Surfaces

| API | Base URL | Recommendation |
|---|---|---|
| **Microsoft Graph** | `https://graph.microsoft.com/v1.0/sites/{siteId}/...` | ✅ Recommended for all new development |
| **SharePoint REST API (legacy)** | `https://{tenant}.sharepoint.com/sites/{site}/_api/...` | Only for SPFx Web Parts within SharePoint itself |

### 5.2 Resolving a Site ID

SharePoint site IDs are not human-readable. Resolve them from hostname and path:
```
GET https://graph.microsoft.com/v1.0/sites/{tenant}.sharepoint.com:/sites/{siteName}
→ Returns { "id": "contoso.sharepoint.com,{site-guid},{web-guid}" }
```

### 5.3 Working with SharePoint Lists

#### Creating a list item:
```http
POST /v1.0/sites/{siteId}/lists/{listId}/items
Authorization: Bearer {token}
Content-Type: application/json

{
  "fields": {
    "Title": "Budget Request Q4",
    "Status": "Pending",
    "Priority": "High",
    "RequestorEmail": "john@contoso.com"
  }
}
```

#### Querying with OData filters:
```
GET /sites/{siteId}/lists/{listId}/items
  ?$filter=fields/Status eq 'Pending'
  &$expand=fields
  &$select=id,fields
  &$top=50
```

#### Querying for schema (column definitions):
```
GET /sites/{siteId}/lists/{listId}/columns
```

#### Upserting column definitions (new — GA as of 2025):
```http
PATCH /v1.0/sites/{siteId}/lists/{listId}/columns/{columnId}
```

### 5.4 Document Libraries & File Operations

#### Upload files < 250 MB (simple):
```http
PUT /v1.0/sites/{siteId}/drives/{driveId}/root:/{folder}/{filename}:/content
Authorization: Bearer {token}
Content-Type: application/octet-stream

[binary content]
```

#### Upload large files via upload session (> 250 MB or unreliable connections):
```http
POST /v1.0/sites/{siteId}/drives/{driveId}/root:/{path}/{filename}:/createUploadSession
→ Returns { "uploadUrl": "https://..." }

PUT {uploadUrl}
Content-Range: bytes 0-{chunkSize-1}/{totalSize}
[chunk bytes — recommended chunk size: 5-10 MB]
```

#### Create folder:
```http
POST /v1.0/sites/{siteId}/drives/{driveId}/root/children
{
  "name": "REQ-00042",
  "folder": {},
  "@microsoft.graph.conflictBehavior": "rename"
}
```

#### List folder contents:
```
GET /v1.0/sites/{siteId}/drives/{driveId}/root:/{folder}:/children
```

### 5.5 Permissions Scopes for SharePoint

| Scope | Access Level |
|---|---|
| `Sites.Read.All` | Read all site collections |
| `Sites.ReadWrite.All` | Create, update, delete items and files (admin consent) |
| `Sites.Selected` | Scoped access to specific sites only (recommended for production apps) |
| `Files.ReadWrite.All` | Read and write OneDrive/SharePoint files (admin consent) |

> 💡 **Best Practice:** Use `Sites.Selected` instead of `Sites.ReadWrite.All` in production. It limits your app to only the specific SharePoint sites you need, significantly reducing the blast radius of a credential compromise. Configure via PowerShell or the Graph API.

---

## 6. Microsoft Teams Platform APIs

### 6.1 Teams Extensibility Surface Map

| Extension Type | What It Does | Tech |
|---|---|---|
| **Static Personal Tab** | Private web app tab per user | `@microsoft/teams-js` v2.x |
| **Channel Tab (Configurable)** | Shared tab in a team channel | Same |
| **Bot / Agent** | Conversational AI agent | Bot Framework SDK v4 / `@microsoft/teams-ai` |
| **Messaging Extension** | Search / action commands in compose bar | Bot Framework |
| **Meeting App** | Side panel / stage during meetings | teams-js + Meeting API |
| **Adaptive Card** | Rich interactive cards in messages | Adaptive Cards schema v1.5+ |
| **Incoming Webhook** | Simple one-way channel notifications | HTTP POST (no auth token needed) |
| **Teams AI Agent** | LLM-backed assistant with tools | `@microsoft/teams-ai` library |

### 6.2 Unified App Manifest (JSON) — Current Standard

> ⚠️ **Breaking Update:** The manifest has been **consolidated to a single unified JSON schema** that covers Teams, Outlook, Word, Excel, PowerPoint, and Copilot. The latest stable version is **1.30** (August 2026). The legacy XML Outlook add-in manifest is deprecated — migrate to this format.

```json
{
  "$schema": "https://developer.microsoft.com/en-us/json-schemas/teams/v1.30/MicrosoftTeams.schema.json",
  "manifestVersion": "1.30",
  "version": "1.0.0",
  "id": "{your-app-guid}",
  "developer": {
    "name": "Contoso",
    "websiteUrl": "https://contoso.com",
    "privacyUrl": "https://contoso.com/privacy",
    "termsOfUseUrl": "https://contoso.com/terms"
  },
  "name": { "short": "My App", "full": "My Full App Name" },
  "description": { "short": "Short description", "full": "Full description" },
  "icons": { "color": "color.png", "outline": "outline.png" },
  "accentColor": "#0078d4",
  "staticTabs": [
    {
      "entityId": "dashboard",
      "name": "My Dashboard",
      "contentUrl": "https://yourapp.com/#/dashboard?name={loginHint}",
      "websiteUrl": "https://yourapp.com/#/dashboard",
      "scopes": ["personal"]
    }
  ],
  "validDomains": ["yourapp.com"],
  "webApplicationInfo": {
    "id": "{YOUR_BACKEND_CLIENT_ID}",
    "resource": "api://{YOUR_BACKEND_CLIENT_ID}"
  }
}
```

### 6.3 Teams JS SDK v2.x — Key Patterns

```typescript
import { app, authentication, pages } from "@microsoft/teams-js";
// ✅ Tree-shakeable since v2.31: import only what you need

await app.initialize();

// Capability-based feature detection (ALWAYS check before using)
if (authentication.isSupported()) {
  const token = await authentication.getAuthToken();
  // Use token as Bearer on your backend
}

// Get rich context about the current user/channel/team
const context = await app.getContext();
console.log(context.user?.displayName);
console.log(context.user?.loginHint);      // user's email hint
console.log(context.channel?.id);          // current Teams channel
console.log(context.team?.groupId);        // M365 Group ID of the team
console.log(context.app?.host?.name);      // "Teams", "Outlook", "Office", etc.
```

> 📌 **SDK Update:** `@microsoft/teams-js` v2.55.0 is the latest stable version (August 2026). The old callback API (`successCallback`, `failureCallback`) is removed in v2.x. Use Promises exclusively.

### 6.4 Sending Messages to Teams Channels (Graph API)

#### Send a plain text message:
```http
POST /v1.0/teams/{teamId}/channels/{channelId}/messages
Authorization: Bearer {token}

{
  "body": {
    "contentType": "text",
    "content": "A new request has been submitted for your approval."
  }
}
```

#### Send an Adaptive Card message:
```http
POST /v1.0/teams/{teamId}/channels/{channelId}/messages

{
  "attachments": [
    {
      "contentType": "application/vnd.microsoft.card.adaptive",
      "content": {
        "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
        "type": "AdaptiveCard",
        "version": "1.5",
        "body": [
          { "type": "TextBlock", "text": "Approval Required", "size": "Large", "weight": "Bolder" },
          {
            "type": "FactSet",
            "facts": [
              { "title": "Request:", "value": "REQ-00012" },
              { "title": "Submitted by:", "value": "John Doe" },
              { "title": "Priority:", "value": "High" }
            ]
          }
        ],
        "actions": [
          {
            "type": "Action.OpenUrl",
            "title": "View Request",
            "url": "https://yourapp.com/#/request/12"
          }
        ]
      }
    }
  ]
}
```

> ⚠️ **Known Limitation:** `Action.Submit` in channel messages does **NOT** trigger bot callbacks unless the card is posted by a registered Bot. For interactive approval buttons in channels, deploy an Azure Bot or use `Action.OpenUrl` to redirect to your app.

### 6.5 Incoming Webhooks

The simplest way to post to a Teams channel without any Entra token:

1. In Teams: Channel → Apps → Search "Incoming Webhook" → Configure → Copy URL.
2. Send a POST:
```http
POST https://your-org.webhook.office.com/webhookb2/{long-id}
Content-Type: application/json

{
  "type": "message",
  "attachments": [
    {
      "contentType": "application/vnd.microsoft.card.adaptive",
      "content": { ... Adaptive Card JSON ... }
    }
  ]
}
```

> ✅ No Entra token required. Ideal for CI/CD alerts, monitoring hooks, and quick demos.  
> ⚠️ One-way only. No SSO, no user attribution, no reply tracking.

### 6.6 Bots — Conversational Automation

Bots are registered in Entra + the Azure Bot Service. Capabilities include:
- Respond to messages in chats / channels
- Proactively send messages without a user initiating contact
- Send Adaptive Cards with `Action.Submit` callbacks

**Proactive message (from saved conversation reference):**
```csharp
var reference = _store.GetConversationReference(userId);
await adapter.ContinueConversationAsync(
    _appId,
    reference,
    async (turnContext, ct) =>
    {
        await turnContext.SendActivityAsync(
            MessageFactory.Text("Your request was approved!"), ct);
    },
    cancellationToken);
```

### 6.7 Required API Permissions for Teams

| Scope | Type | Purpose |
|---|---|---|
| `Team.ReadBasic.All` | Delegated | List teams |
| `Channel.ReadBasic.All` | Delegated | List channels |
| `ChannelMessage.Send` | Delegated | Send to channels as current user |
| `ChannelMessage.Read.All` | Application | Read all channel messages (admin consent) |
| `ChatMessage.Send` | Delegated | Send direct/group chat messages |
| `TeamsAppInstallation.ReadWriteSelfForTeam` | Delegated | Install app in teams |
| `OnlineMeetings.ReadWrite` | Delegated | Create / manage meetings |

---

## 7. Outlook Add-in & Mail APIs

### 7.1 Outlook Add-in Architecture

Outlook Add-ins are HTML/CSS/JS apps running inside an Office-managed iframe embedded in Outlook Desktop (Win/Mac), Outlook on the Web (OWA), and Outlook Mobile. They use the **Office.js** SDK to communicate with the host:

```
Office.js → Outlook Host Process → Mail Item Context (subject, body, attachments, sender)
```

**Types of Outlook Add-ins:**

| Type | Context | Trigger |
|---|---|---|
| **Mail Read** | Reading pane | When any email is opened |
| **Mail Compose** | Compose window | When composing a new email |
| **Meeting Organizer** | Calendar event form | When composing a meeting |
| **Integrated Spam Reporting** | Phishing/spam report button | User clicks report button (new in Requirement Set 1.14) |

### 7.2 Manifest: Unified JSON (Replaces Legacy XML)

> 🔴 **Migration Required:** The legacy XML manifest format for Outlook add-ins is deprecated. New development **must** use the Unified JSON App Manifest (`manifestVersion: "1.30"`). The JSON format covers Teams + Outlook + Office apps in a single file managed via the M365 Admin Center's **Integrated Apps** portal.

### 7.3 Reading Mail Item Data (Office.js)

```javascript
Office.onReady(async () => {
  const item = Office.context.mailbox.item;

  // Subject (async getter — all Office.js reads are async)
  item.subject.getAsync(result => {
    if (result.status === Office.AsyncResultStatus.Succeeded) {
      console.log("Subject:", result.value);
    }
  });

  // Sender (synchronous in read mode)
  console.log("From:", item.from.displayName, item.from.emailAddress);

  // Body as HTML
  item.body.getAsync(Office.CoercionType.Html, result => {
    console.log("Body HTML:", result.value);
  });

  // Attachments metadata
  item.attachments.forEach(att => {
    console.log("Attachment:", att.name, att.size, att.attachmentType);
  });

  // Get attachment content (Requirement Set 1.8+)
  item.getAttachmentContentAsync(att.id, result => {
    const content = result.value.content; // base64 for files
  });
});
```

### 7.4 Sending Mail via Graph API (Backend)

```http
POST /v1.0/me/sendMail
Authorization: Bearer {delegated token}
Content-Type: application/json

{
  "message": {
    "subject": "Your request REQ-00042 has been approved",
    "body": {
      "contentType": "HTML",
      "content": "<h2>Approved</h2><p>Your request has been reviewed.</p>"
    },
    "toRecipients": [
      { "emailAddress": { "address": "john@contoso.com", "name": "John Doe" } }
    ],
    "attachments": [
      {
        "@odata.type": "#microsoft.graph.fileAttachment",
        "name": "approval.pdf",
        "contentType": "application/pdf",
        "contentBytes": "{base64-encoded-content}"
      }
    ]
  }
}
```

### 7.5 Graph Mail Permissions

| Scope | Access |
|---|---|
| `Mail.Read` | Read current user's emails |
| `Mail.ReadWrite` | Read, create, update, delete emails |
| `Mail.Send` | Send email as current user |
| `Mail.Read.All` | Admin: read all users' mail (application permission) |
| `Mail.Send.Shared` | Send from shared/delegated mailbox |

> 🔴 **RETIRED:** The Outlook REST API (`outlook.office.com/api/v2.0`) was retired November 2022. All mail integrations must use Microsoft Graph at `graph.microsoft.com/v1.0/me/messages`.

### 7.6 Requirement Sets (Cross-Client Compatibility)

Office.js is versioned by "Requirement Sets." Different Outlook clients support different sets:

| Requirement Set | Key Features Added |
|---|---|
| **Mailbox 1.8** | Get attachment content, internet headers, enhanced SSO |
| **Mailbox 1.10** | Spam reporting, event-based activation (auto-launch add-in on send/receive) |
| **Mailbox 1.12** | Integrated spam reporting button UI |
| **Mailbox 1.14** | Shared folder access, latest baseline for new add-ins |

Always guard with:
```javascript
if (Office.context.requirements.isSetSupported('Mailbox', '1.14')) {
  // Use 1.14 features
}
```

---

## 8. Microsoft 365 Copilot Integrations

This is the newest and fastest-growing extensibility surface in the M365 ecosystem.

### 8.1 What Is Copilot Extensibility?

Microsoft 365 Copilot (an AI layer on top of M365) can be extended so it reasons over your data or executes your workflows using natural language. There are three integration patterns:

| Pattern | What It Does | When To Use |
|---|---|---|
| **Copilot Connectors** (formerly Graph Connectors) | Index external data into Graph so Copilot can reason over it | External databases, CRM systems, legacy content |
| **Copilot Agents** (Declarative) | Custom-scoped Copilot instances with instructions and data sources | Focus Copilot on a specific domain without code |
| **Copilot Plugins / API Plugins** | Give Copilot the ability to call your APIs via natural language | When Copilot needs to take action (create/update/delete) |

### 8.2 Copilot Connectors (Index External Data)

Two types of connectors:

| Type | Mechanism | Best For |
|---|---|---|
| **Synced** | Crawl & index content into Graph | Static/slow-changing datasets (knowledge bases, wikis) |
| **Federated** (via MCP) | Real-time fetch using Model Context Protocol — no indexing | Sensitive/dynamic data that must stay in source system |

**Building a Synced Connector:**

**Step 1 — Create the connection:**
```http
POST /v1.0/external/connections
{
  "id": "contosocatalog",
  "name": "Contoso Product Catalog",
  "description": "Internal product database for Copilot"
}
```

**Step 2 — Define the schema:**
```http
POST /v1.0/external/connections/contosocatalog/schema
{
  "baseType": "microsoft.graph.externalItem",
  "properties": [
    { "name": "productName", "type": "String", "isSearchable": true, "labels": ["title"] },
    { "name": "price",       "type": "Double", "isFilterable": true },
    { "name": "description", "type": "String", "isSearchable": true, "isContent": true }
  ]
}
```

**Step 3 — Ingest items with ACL:**
```http
PUT /v1.0/external/connections/contosocatalog/items/prod001
{
  "acl": [
    { "type": "everyone", "value": "everyone", "accessType": "grant" }
  ],
  "properties": {
    "productName": "Contoso Widget Pro",
    "price": 99.99,
    "description": "The best widget for enterprise teams."
  },
  "content": {
    "value": "Extended content that Copilot can read and reason over.",
    "type": "text"
  }
}
```

> ⚠️ **License Required:** Copilot Connectors only surface content in Copilot for users with a **Microsoft 365 Copilot license** (add-on to E3/E5).

### 8.3 Copilot Agents (Declarative)

Declarative agents are no-code/low-code Copilot customizations defined in the app manifest. They scope Copilot's knowledge and persona to a specific domain:

```json
{
  "copilotAgents": {
    "declarativeAgents": [
      {
        "id": "supportAgent",
        "file": "declarative-agent.json"
      }
    ]
  }
}
```

`declarative-agent.json`:
```json
{
  "name": "Contoso Support Agent",
  "description": "Helps employees find HR policies and IT support answers",
  "instructions": "You are a helpful HR and IT support assistant. Only answer questions about company policies and IT issues.",
  "capabilities": [
    { "name": "WebSearch" },
    { "name": "OneDriveAndSharePoint",
      "items_by_url": [
        { "url": "https://contoso.sharepoint.com/sites/HR" }
      ]
    }
  ]
}
```

### 8.4 Required Permissions for Copilot Integrations

| Scope | Purpose |
|---|---|
| `ExternalConnection.ReadWrite.OwnedBy` | Manage your app's own connector |
| `ExternalItem.ReadWrite.OwnedBy` | Ingest items into your connector |
| `ExternalItem.Read.All` | Read items from all connectors (admin) |

---

## 9. Cross-Cutting Concerns

### 9.1 Throttling & Retry Strategies

Microsoft Graph enforces service-protection limits. `HTTP 429 Too Many Requests` includes a `Retry-After` header:

```csharp
private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
{
    for (int attempt = 0; attempt < 5; attempt++)
    {
        try { return await operation(); }
        catch (ODataError ex) when (ex.ResponseStatusCode == 429)
        {
            // ODataError in Graph SDK v5 (replaces ServiceException from v4)
            var delay = ex.Error?.Message?.Contains("Retry-After") == true
                ? TimeSpan.FromSeconds(30)
                : TimeSpan.FromSeconds(Math.Pow(2, attempt));
            await Task.Delay(delay);
        }
    }
    throw new Exception("Graph API max retries exceeded.");
}
```

> 📌 **SDK Update:** Graph SDK v5 replaced `ServiceException` with `ODataError`. Update your catch blocks if migrating from v4.

The Graph SDK also has built-in retry middleware:
```csharp
var handlers    = GraphClientFactory.CreateDefaultHandlers();
var httpClient  = GraphClientFactory.Create(handlers);
var graphClient = new GraphServiceClient(httpClient);
```

### 9.2 Token Caching

Access tokens expire after **1 hour** (refresh tokens last 90 days). Never request a new token on every API call — use the MSAL cache:

```csharp
// In Microsoft.Identity.Web / MSAL — token is cached automatically
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithClientSecret(secret)
    .WithTenantId(tenantId)
    .Build();

// MSAL will use the cached token if still valid, only fetching a new one when needed
var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
```

For distributed production environments (multiple app instances), use a shared external cache:
```csharp
// Microsoft.Identity.Web — use Redis or SQL distributed cache
services.AddMicrosoftIdentityWebApiAuthentication(configuration)
        .EnableTokenAcquisitionToCallDownstreamApi()
        .AddDistributedTokenCaches();  // backed by IDistributedCache (Redis, SQL, etc.)
```

### 9.3 Microsoft.Identity.Web (Recommended Integration Library)

For ASP.NET Core or Azure Functions, `Microsoft.Identity.Web` handles the full auth lifecycle:
```csharp
// Program.cs
services.AddMicrosoftIdentityWebApiAuthentication(Configuration, "AzureAd")
        .EnableTokenAcquisitionToCallDownstreamApi()
        .AddMicrosoftGraph(Configuration.GetSection("Graph"))
        .AddDistributedTokenCaches();
```

### 9.4 Managed Identity (Azure Deployment Best Practice)

When running in Azure (Functions, App Service, Container Apps), use **Managed Identity** instead of client secrets:
```csharp
// Zero secret storage — Azure handles identity automatically
var credential  = new DefaultAzureCredential(); // picks up Managed Identity in Azure
var graphClient = new GraphServiceClient(credential,
    new[] { "https://graph.microsoft.com/.default" });
```

### 9.5 Conditional Access & Zero Trust

Enterprise M365 tenants often have Conditional Access Policies that block access from:
- Unregistered/unmanaged devices
- Specific geographic locations
- Apps not marked as compliant

Even with a valid token, Graph calls may return `HTTP 403` with error code `AADSTS53003` if CA policies are too restrictive for your app registration. Work with the tenant admin to add a CA exclusion or mark the app as compliant.

---

## 10. Risks & Possible Blockers Reference

### 🔴 High-Impact Blockers

| Blocker | Description | Mitigation |
|---|---|---|
| **Admin Consent Not Granted** | IT admins must approve scopes like `Sites.ReadWrite.All`, `Mail.Read.All` | Identify all admin-consent scopes upfront. Arrange tenant admin approval before development starts. |
| **Sideloading Disabled** | Organization may block custom app sideloading in Teams | Use the Teams Admin Center to request upload permissions, or publish to the org app catalog. |
| **Tenant Conditional Access Blocking** | CA policies may block developer machines or unregistered devices | Have the tenant admin add a CA exclusion for your app registration or developer IP. |
| **Multi-Tenant vs. Single-Tenant** | Multi-tenant apps require each external tenant to consent separately | Plan tenant scope early. Use single-tenant for demos/dev. Plan admin-consent-grant flow for multi-tenant. |
| **SharePoint Permissions Missing** | App must have explicit permissions on the target site collection | Configure via PnP PowerShell or Sites.Selected scope before deploying. |
| **Azure AD Graph Still in Code** | Azure AD Graph (`graph.windows.net`) retired August 31, 2025 | Audit all code for `graph.windows.net` — replace with `graph.microsoft.com`. |
| **Copilot License Missing** | Connector data / Copilot agents only visible to users with M365 Copilot license | Verify license assignment before demoing Copilot features. |

### 🟡 Medium-Impact Risks

| Risk | Description | Mitigation |
|---|---|---|
| **Graph Beta API Breaking Changes** | `/beta` endpoint is not SLA-covered | Never use `/beta` in production. Only for prototyping. |
| **Token Scope Mismatch** | Token issued for `api://{ClientId}` cannot call Graph unless OBO is performed | Implement OBO on the backend before calling Graph on user's behalf. |
| **Teams Iframe CSP Restrictions** | Teams blocks iframes from unapproved domains | Add your domain to `validDomains` in the app manifest. |
| **Office.js Requirement Set Drift** | Desktop, OWA, and Mobile Outlook support different requirement sets | Guard all feature usage with `isSetSupported()`. Test on all target clients. |
| **Clock Skew on Token Validation** | Server clock drift can cause valid tokens to appear expired | Set `ClockSkew = TimeSpan.FromMinutes(5)` in validation parameters. |
| **Webhook Subscription Expiry** | Graph subscriptions expire — mail subscriptions max 3 days | Build a renewal job that PATCHes subscriptions before expiry. |
| **Throttling Under Load** | Burst Graph calls trigger 429 rate limits | Implement exponential backoff. Use `$batch` to bundle requests. |
| **Business Plan 300-Seat Cap** | Business Basic/Standard/Premium plans cap at 300 users | Plan migration path to Enterprise E-series if growth is expected. |

### 🟢 Low-Impact / Awareness Items

| Item | Notes |
|---|---|
| **Delegated vs. Application Permissions** | Delegated = user context; Application = app context. Some scopes exist only as one type. |
| **Guest User Limitations** | Guest users may have limited M365 licenses, restricting what Graph data is accessible for them. |
| **Outlook Mobile Limitations** | Several Office.js APIs and add-in form factors are not supported on Outlook Mobile. |
| **Teams Meeting App Surfaces** | Meeting stage and side-panel apps require additional `meetingSurfaces` configuration in the manifest. |
| **Sovereign Cloud Endpoints** | Gov/China clouds use different Graph endpoints (`graph.microsoft.us` for GCC, GCC-High). |
| **Unified Manifest Migration** | Legacy XML Outlook manifests are deprecated. Plan migration to the unified JSON manifest (v1.30). |
| **GDPR / Data Residency** | Reading mailboxes and SharePoint data is subject to data residency policies. Verify Azure region alignment. |
| **Teams AI Library** | For LLM-backed bots, evaluate `@microsoft/teams-ai` for built-in RAG, prompt management, and tool routing. |

---

## 11. Quick Reference Cheat Sheet

```
╔═════════════════════════════════════════════════════════════════════════════╗
║              M365 DEVELOPER QUICK REFERENCE — August 2026                  ║
╠═════════════════════════════════════════════════════════════════════════════╣
║ TOKEN ENDPOINT   https://login.microsoftonline.com/{tenant}/oauth2/v2.0    ║
║ GRAPH BASE       https://graph.microsoft.com/v1.0                          ║
║ JWKS (keys)      https://login.microsoftonline.com/common/discovery/        ║
║                  v2.0/keys                                                  ║
║ TOKEN LIFETIME   Access: 1 hour | Refresh: 90 days                         ║
║ RATE LIMIT       ~10,000 requests per user per 10 minutes (varies)         ║
║ MAX FILE (PUT)   250 MB | Use Upload Session for larger / unreliable nets   ║
║ MANIFEST VERSION 1.30 (Unified JSON — Teams + Outlook + Office)            ║
╠═════════════════════════════════════════════════════════════════════════════╣
║ KEY SCOPES                                                                  ║
║  User.Read                   Read your own profile                         ║
║  Mail.Send                   Send email as current user                    ║
║  Mail.ReadWrite              Read/write mail                               ║
║  Sites.Selected              Scoped SharePoint access (preferred)          ║
║  Sites.ReadWrite.All         All SharePoint read/write (admin consent)     ║
║  Files.ReadWrite.All         OneDrive/SharePoint files (admin consent)     ║
║  ChannelMessage.Send         Post to Teams channels (delegated)            ║
║  TeamMember.Read.All         Read team memberships (admin consent)         ║
║  ExternalItem.ReadWrite.OwnedBy  Copilot Connector data ingestion          ║
╠═════════════════════════════════════════════════════════════════════════════╣
║ KEY NUGET PACKAGES (.NET)                                                   ║
║  Microsoft.Graph v5.*              Graph SDK client (v5 = ODataError)      ║
║  Microsoft.Identity.Web v2.*       Full auth lifecycle for ASP.NET/Funcs   ║
║  Azure.Identity v1.*               DefaultAzureCredential / Managed ID     ║
║  Microsoft.IdentityModel.Tokens    JWT validation primitives               ║
╠═════════════════════════════════════════════════════════════════════════════╣
║ KEY NPM PACKAGES (JS/TS)                                                    ║
║  @microsoft/teams-js v2.55+        Teams Tab SDK (Promise-based)           ║
║  @microsoft/microsoft-graph-client v3.*  Graph SDK for browser/Node        ║
║  @azure/msal-browser v3.*          MSAL for single-page apps               ║
║  @azure/msal-node v2.*             MSAL for server-side Node apps          ║
║  @microsoft/teams-ai               Teams AI Library for LLM bots           ║
║  office-js                         Office Add-in runtime (CDN only)        ║
╠═════════════════════════════════════════════════════════════════════════════╣
║ RETIRED / DO NOT USE                                                        ║
║  graph.windows.net (Azure AD Graph)     Retired August 31, 2025 ☠         ║
║  outlook.office.com/api/v2.0            Retired November 2022 ☠           ║
║  teams-js v1.x callback APIs            Removed in v2.x ☠                 ║
╚═════════════════════════════════════════════════════════════════════════════╝
```

---

## 12. Pricing Tiers for M365 Apps

Monetizing an M365 app means wiring your feature gating logic to Microsoft's commercial billing infrastructure. This section covers every path — from marketplace transactable offers to self-managed licensing — and how to implement tier enforcement inside your app.

### 12.1 The Two Monetization Paths

| Path | Who Handles Billing | Best For |
|---|---|---|
| **Microsoft Marketplace Transactable Offer** | Microsoft bills and pays out to you | ISVs selling to enterprise buyers via AppSource / Teams Store |
| **Self-Managed Billing** | You bill directly (Stripe, Paddle, etc.) | Startups, B2C products, or apps not yet ready for marketplace |

Both paths are valid and can coexist. Many ISVs start with self-managed billing and later add a transactable marketplace offer to unlock enterprise procurement (MACC spend, Azure invoice consolidation).

---

### 12.2 Pricing Models Available in the Microsoft Marketplace

All plans within a single SaaS offer must use the **same** pricing model. You cannot mix models within one offer — create separate offers if you need different structures.

| Model | How It Works | Metered Billing Supported? |
|---|---|---|
| **Per User / Per Seat** | Price multiplied by number of assigned users | ❌ No — per-user plans cannot use metered billing |
| **Flat Rate** | Fixed monthly or annual price regardless of users | ✅ Yes — can add custom usage dimensions on top |
| **Flat Rate + Metered** | Base flat fee plus overage charges per consumption unit | ✅ Recommended for usage-heavy apps |

> ⚠️ **Irreversible:** Once an offer is published, **you cannot change its pricing model**. Choosing Per-User locks you out of metered billing forever on that offer. Plan carefully.

#### Billing Frequencies
- **Monthly** — billed each month, can be cancelled monthly
- **Annual** — billed upfront yearly (typically discounted 15–20%)
- **Multi-year** — 2–3 year contracts route through Azure Marketplace

#### Publisher Revenue Share
- Standard marketplace fee: **3% agency fee** for transactable offers (reduced from 20% to incentivize marketplace listings)
- Payout is made monthly via Partner Center after a 30-day settlement window

---

### 12.3 The Microsoft Marketplace SaaS Offer Anatomy

```
┌───────────────────────────────────────────────┐
│              Marketplace Offer                │
│  ┌─────────────────────────────────────────┐  │
│  │  Plan: Free (0 seats, limited features) │  │
│  ├─────────────────────────────────────────┤  │
│  │  Plan: Starter — $19/user/month         │  │
│  │    • Up to 10 users                     │  │
│  │    • Core features only                 │  │
│  ├─────────────────────────────────────────┤  │
│  │  Plan: Professional — $49/user/month    │  │
│  │    • Unlimited users                    │  │
│  │    • All features + API access          │  │
│  ├─────────────────────────────────────────┤  │
│  │  Plan: Enterprise — Custom (Private)    │  │
│  │    • SLA, SSO, dedicated support        │  │
│  └─────────────────────────────────────────┘  │
└───────────────────────────────────────────────┘
```

Each **Plan** (also called a SKU) has:
- A unique `planId` (machine-readable identifier)
- A display name and description
- Price per unit and billing frequency
- Optional: feature flags you enforce in your app based on the active `planId`

---

### 12.4 Technical Integration: The SaaS Fulfillment API v2

This is the API you must implement to automate subscription lifecycle management when customers purchase via the marketplace.

**Base URL:** `https://marketplaceapi.microsoft.com/api/saas`  
**Auth:** Bearer token from Entra ID (client credentials, your app's own identity)

#### Full Purchase Flow

```
Customer clicks "Buy" on AppSource / Teams Store
      ↓
Microsoft redirects to YOUR Landing Page URL
  GET https://yourapp.com/landing?token={marketplace-token}
      ↓
[Step 1] Your backend resolves the token → gets subscriptionId + planId
      ↓
[Step 2] Show user a confirmation UI (plan details, account setup)
      ↓
[Step 3] Activate the subscription → Microsoft starts billing
      ↓
Provision the tenant in your system at the purchased plan tier
```

#### Step 1 — Resolve the Marketplace Token
```http
POST https://marketplaceapi.microsoft.com/api/saas/subscriptions/resolve
     ?api-version=2018-08-31
Authorization: Bearer {your-app-entra-token}
x-ms-marketplace-token: {token-from-query-string}
Content-Type: application/json
```

Returns:
```json
{
  "subscriptionId": "a1b2c3d4-...",
  "subscriptionName": "Contoso Corp — Professional Plan",
  "offerId": "my-saas-offer",
  "planId": "professional",
  "quantity": 25,
  "buyer": {
    "email": "admin@contoso.com",
    "tenantId": "contoso-tenant-guid"
  }
}
```

> ⚠️ The marketplace token is valid for **24 hours only**. Resolve it immediately upon the customer landing on your page.

#### Step 2 — Activate the Subscription
```http
POST https://marketplaceapi.microsoft.com/api/saas/subscriptions/{subscriptionId}/activate
     ?api-version=2018-08-31
Authorization: Bearer {your-app-entra-token}
Content-Type: application/json

{
  "planId": "professional",
  "quantity": 25
}
```

Once activated, Microsoft begins the billing cycle.

#### Lifecycle Webhook Events (Required)

You **must** expose a webhook URL in Partner Center to receive subscription lifecycle events:

| Event | Trigger | Required Action |
|---|---|---|
| `ChangePlan` | Customer upgrades/downgrades their plan | Update features in your app to match new `planId` |
| `ChangeQuantity` | Customer adjusts number of seats | Update seat counts in your licensing DB |
| `Suspend` | Payment failed or subscription suspended | Gracefully restrict access |
| `Reinstate` | Suspension resolved | Restore access |
| `Unsubscribe` | Subscription cancelled | Deprovision / export data |

```http
POST https://yourapp.com/api/marketplace/webhook
Content-Type: application/json

{
  "action": "ChangePlan",
  "subscriptionId": "a1b2c3d4-...",
  "offerId": "my-saas-offer",
  "planId": "enterprise",
  "timeStamp": "2026-08-12T10:00:00Z"
}
```

After processing the event, acknowledge it:
```http
PATCH https://marketplaceapi.microsoft.com/api/saas/subscriptions/{subscriptionId}
      /operations/{operationId}?api-version=2018-08-31

{ "status": "Success" }
```

---

### 12.5 Metered Billing — Charge for Usage

Use the **Marketplace Metering Service API** to report consumption of custom dimensions (e.g., API calls, documents processed, storage GB).

**Only available with Flat Rate plans.** Define dimensions in Partner Center first.

#### Report Usage:
```http
POST https://marketplaceapi.microsoft.com/api/usageEvent
     ?api-version=2018-08-31
Authorization: Bearer {your-app-entra-token}
Content-Type: application/json

{
  "resourceId": "{subscriptionId}",
  "quantity": 150,
  "dimension": "api_calls",
  "effectiveStartTime": "2026-08-12T00:00:00Z",
  "planId": "professional"
}
```

#### Batch Report (up to 25 events):
```http
POST https://marketplaceapi.microsoft.com/api/batchUsageEvent
     ?api-version=2018-08-31

{
  "request": [
    { "resourceId": "sub1", "quantity": 500, "dimension": "api_calls", ... },
    { "resourceId": "sub2", "quantity": 12,  "dimension": "storage_gb", ... }
  ]
}
```

> ⚠️ **Duplicate Protection:** Microsoft deduplicates on `(subscriptionId, dimension, effectiveStartTime)`. Do not submit the same combination twice.

---

### 12.6 Teams In-App Purchases

Allow users to upgrade their tier without leaving Teams:

#### Prerequisites
- App published in Teams Store with a linked transactable SaaS offer
- Manifest includes `InAppPurchase.Allow.User` RSC permission
- Works in **personal app context only** (not inside channels or meetings)

#### Trigger the Purchase Dialog:
```typescript
import { monetization } from "@microsoft/teams-js";

if (monetization.isSupported()) {
  try {
    await monetization.openPurchaseExperience();
    // User completed purchase — re-check their plan from your backend
    const plan = await fetchUserPlan();
    setCurrentTier(plan.planId);
  } catch (err) {
    // User cancelled or purchase failed
    console.log("Purchase not completed:", err);
  }
}
```

> ⚠️ **Mobile Policy:** You must **not** include purchase links in your mobile/tablet Teams UI. You can indicate that a feature requires a paid plan, but cannot show a direct purchase button on mobile.

---

### 12.7 Enforcing Tiers Inside Your App

Once you know a tenant's active `planId`, enforce feature access at multiple layers:

#### Backend Enforcement (API Layer — Most Critical)
```csharp
// Middleware or filter — runs on every API request
public class PlanEnforcementMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.User.FindFirst("tid")?.Value;
        var plan     = await _licenseService.GetPlanAsync(tenantId);

        // Attach plan to the request context for controllers to read
        context.Items["Plan"] = plan;

        // Block deprecated/suspended tenants at the gate
        if (plan.Status == "Suspended")
        {
            context.Response.StatusCode = 402; // Payment Required
            await context.Response.WriteAsJsonAsync(new
            {
                error = "subscription_suspended",
                message = "Your subscription is suspended. Please update your payment."
            });
            return;
        }

        await _next(context);
    }
}
```

#### Feature-Level Gate (Controller / Handler):
```csharp
[HttpPost("api/requests/{id}/export")]
public async Task<IActionResult> ExportToPdf(int id)
{
    var plan = HttpContext.Items["Plan"] as SubscriptionPlan;

    if (plan?.PlanId is not ("professional" or "enterprise"))
    {
        return StatusCode(403, new
        {
            error   = "feature_not_available",
            message = "PDF export is available on Professional and Enterprise plans.",
            upgradeUrl = "https://yourapp.com/upgrade"
        });
    }

    // ... proceed with export
}
```

#### Frontend Enforcement (UX Layer — Soft Gates):
```typescript
const PLAN_FEATURES: Record<string, string[]> = {
  free:         ["view", "create"],
  starter:      ["view", "create", "approve", "comment"],
  professional: ["view", "create", "approve", "comment", "export", "api"],
  enterprise:   ["*"],  // all features
};

function canAccess(feature: string, planId: string): boolean {
  const features = PLAN_FEATURES[planId] ?? [];
  return features.includes("*") || features.includes(feature);
}

// In a React component:
{canAccess("export", currentPlan) ? (
  <Button onClick={handleExport}>Export to PDF</Button>
) : (
  <UpgradePrompt feature="PDF Export" requiredPlan="Professional" />
)}
```

> ⚠️ **Never rely solely on frontend gates.** The backend must always re-validate the plan. Frontend gating is UX only — a determined user can bypass it.

---

### 12.8 Licensing Database Schema

A minimal schema to track subscriptions from the marketplace webhook:

```sql
CREATE TABLE Subscriptions (
    SubscriptionId   UNIQUEIDENTIFIER PRIMARY KEY,   -- from marketplace
    TenantId         NVARCHAR(100)    NOT NULL,       -- buyer's Entra tenant ID
    PlanId           NVARCHAR(50)     NOT NULL,       -- "starter", "professional", etc.
    Quantity         INT              NOT NULL,        -- seat count (for per-user plans)
    Status           NVARCHAR(20)     NOT NULL,        -- "Active", "Suspended", "Cancelled"
    BillingFrequency NVARCHAR(10)     NOT NULL,        -- "Monthly", "Annual"
    StartDate        DATETIME2        NOT NULL,
    RenewalDate      DATETIME2        NOT NULL,
    UpdatedAt        DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE UsageEvents (
    Id              BIGINT        IDENTITY PRIMARY KEY,
    SubscriptionId  UNIQUEIDENTIFIER NOT NULL REFERENCES Subscriptions(SubscriptionId),
    Dimension       NVARCHAR(50)  NOT NULL,    -- "api_calls", "storage_gb"
    Quantity        DECIMAL(18,4) NOT NULL,
    ReportedAt      DATETIME2     NOT NULL,
    MarketplaceStatus NVARCHAR(20) NOT NULL    -- "Accepted", "Rejected", "Duplicate"
);
```

---

### 12.9 Private Offers (Enterprise Deals)

For enterprise customers who need custom pricing, terms, or SLAs, use **Private Plans** in Partner Center:

- Create a plan visible **only to specified Entra tenant IDs**
- Set custom per-unit pricing, minimum commitment, or payment terms
- Grant access via the customer's Azure billing account
- The technical integration is identical — same Fulfillment API, same webhook events

Private plans are invisible in the public marketplace catalog and are shared with customers via a direct link.

---

### 12.10 Free Trials

Configure trials directly in Partner Center:

| Trial Type | How It Works |
|---|---|
| **Free Trial (Partner-managed)** | You control trial duration; marketplace doesn't bill during trial |
| **Test Drive** | Pre-configured demo environment, no subscription creation |

Trial subscriptions go through the same Fulfillment API flow. The `planId` returned will be your trial plan ID. On conversion (user upgrades), a `ChangePlan` webhook fires with the paid `planId`.

---

### 12.11 Choosing Your Pricing Strategy — Decision Framework

```
Q1: Will enterprise buyers need to use Azure committed spend (MACC)?
  └─ YES → Transactable Marketplace SaaS Offer (required for MACC eligibility)
  └─ NO  → Either path works

Q2: Do you charge per seat / per user?
  └─ YES → Per-User plan model
  └─ NO  → Flat Rate (add metered dimensions for usage overages)

Q3: Do you need to charge for usage (API calls, storage, etc.)?
  └─ YES → Must use Flat Rate + Metered Billing (NOT Per-User)
  └─ NO  → Either Per-User or Flat Rate works

Q4: Do you need custom enterprise pricing?
  └─ YES → Transactable offer with Private Plans
  └─ NO  → Public plans in marketplace

Q5: Is your app in the Teams Store?
  └─ YES → Link your SaaS offer to the app for in-app purchase via monetization.openPurchaseExperience()
  └─ NO  → Link from your app to a dedicated upgrade/pricing page
```

---

### 12.12 Open-Source Accelerator (Skip Weeks of Boilerplate)

Microsoft maintains a reference implementation for the entire SaaS marketplace integration:

> **[Commercial Marketplace SaaS Accelerator](https://github.com/Azure/Commercial-Marketplace-SaaS-Accelerator)**

Includes:
- ✅ Landing page implementation (ASP.NET Core)
- ✅ SaaS Fulfillment API v2 client
- ✅ Webhook handler for all lifecycle events
- ✅ Admin portal for managing subscriptions
- ✅ Metering Service integration scaffolding
- ✅ ARM template for one-click Azure deployment

Also available for local testing:
> **[SaaS API Emulator](https://github.com/microsoft/Commercial-Marketplace-SaaS-API-Emulator)** — Simulate marketplace tokens and purchase flows without a real Partner Center account.

---

*Last Updated: August 2026 | Graph v1.0 | Teams JS SDK v2.55 | Office.js Mailbox 1.14 | Microsoft.Graph NuGet v5 | Unified Manifest v1.30*
