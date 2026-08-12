# Microsoft 365 Developer Ecosystem — Comprehensive Technical Reference

> **Scope:** General-purpose reference for any development team building integrations with Microsoft 365 APIs.  
> **Coverage:** Microsoft Graph, Entra ID authentication, SharePoint, Teams, Outlook, implementation patterns, known risks, and production blockers.

---

## Table of Contents

1. [The Microsoft 365 Platform Overview](#1-the-microsoft-365-platform-overview)
2. [Microsoft Entra ID & Authentication](#2-microsoft-entra-id--authentication)
3. [Microsoft Graph API](#3-microsoft-graph-api)
4. [SharePoint Online APIs](#4-sharepoint-online-apis)
5. [Microsoft Teams Platform APIs](#5-microsoft-teams-platform-apis)
6. [Outlook Add-in & Mail APIs](#6-outlook-add-in--mail-apis)
7. [Cross-Cutting Concerns](#7-cross-cutting-concerns)
8. [Risks & Possible Blockers Reference](#8-risks--possible-blockers-reference)
9. [Quick Reference Cheat Sheet](#9-quick-reference-cheat-sheet)

---

## 1. The Microsoft 365 Platform Overview

### 1.1 What Is the M365 Platform?
Microsoft 365 (formerly Office 365) is a cloud-based productivity ecosystem that provides hosted services for communication, collaboration, and content. From a developer's perspective, it exposes a set of APIs, SDKs, and extensibility frameworks through which you can build apps that:

- Embed natively inside Teams, Outlook, Word, Excel, or SharePoint
- Read and write data stored in users' mailboxes, calendars, drives, and chats
- Send notifications and trigger workflows using Microsoft-managed infrastructure

### 1.2 Developer Entry Points

| Surface | Description | Primary SDK |
|---|---|---|
| **Microsoft Graph** | Unified REST API gateway for all M365 data | Graph .NET SDK / JS SDK / REST |
| **Teams Apps** | Tabs, Bots, Messaging Extensions, Meeting Apps | `@microsoft/teams-js`, Bot Framework |
| **Outlook Add-ins** | Task panes and mail compose/read extensions | Office.js |
| **SharePoint Framework (SPFx)** | Custom Web Parts running inside SharePoint | SPFx Yeoman toolkit |
| **Power Platform Connectors** | Low-code integrations for Power Apps / Automate | Custom Connector SDK |

### 1.3 Fundamental Architecture Principle

All M365 APIs sit **behind Microsoft Entra ID**. Every API call requires a bearer token issued by Entra ID. There is no API-key-based access to Microsoft Graph — identity is always the gatekeeper.

```
Your App  →  Entra ID (OAuth 2.0)  →  Microsoft Graph  →  Teams / SharePoint / Outlook / OneDrive
```

---

## 2. Microsoft Entra ID & Authentication

### 2.1 App Registration (Pre-requisite for Everything)

Every integration requires an **App Registration** in the Microsoft Entra portal (`portal.azure.com → Entra ID → App Registrations`):

| Field | Description |
|---|---|
| **Application (client) ID** | Identifies your app to Entra |
| **Tenant ID** | Identifies the M365 organization |
| **Client Secret / Certificate** | Used for daemon/server flows |
| **Redirect URIs** | Where Entra sends auth codes back |
| **API Permissions** | The Graph scopes your app can request |
| **Expose an API** | Required if you build your own backend that frontend tokens target |

### 2.2 OAuth 2.0 Grant Types

#### 2.2.1 Authorization Code Flow (Web Apps with User Context)
Best for web apps where users interactively log in:
```
1. Redirect user to: https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize
   ?client_id={CLIENT_ID}
   &response_type=code
   &redirect_uri={YOUR_REDIRECT_URI}
   &scope=openid profile email User.Read
   &state={random_state}

2. Entra redirects back with: ?code={AUTH_CODE}&state={state}

3. Exchange code for tokens via POST to:
   https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token
   Body: grant_type=authorization_code&code={AUTH_CODE}&client_id=...&client_secret=...
```

#### 2.2.2 Client Credentials Flow (Server-to-Server / Daemon Apps)
For background jobs and services with no user context (runs as the app's own identity):
```http
POST https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id={CLIENT_ID}
&client_secret={CLIENT_SECRET}
&scope=https://graph.microsoft.com/.default
```

> ⚠️ **Risk:** Client Credentials tokens are **Application permissions** — they do not act as a specific user. This is powerful but carries compliance risk. Use delegated flows wherever possible to preserve user-level auditing.

#### 2.2.3 On-Behalf-Of (OBO) Flow (API Chaining)
Used when your backend API receives a token from the frontend and needs to call Graph on behalf of that user:
```http
POST https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token

grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer
&client_id={BACKEND_CLIENT_ID}
&client_secret={BACKEND_CLIENT_SECRET}
&assertion={INCOMING_USER_TOKEN}
&requested_token_use=on_behalf_of
&scope=https://graph.microsoft.com/Sites.ReadWrite.All
```

#### 2.2.4 Teams Silent SSO (Tab Authentication)
Teams Tabs use a specialized silent SSO that exchanges the Teams session for an Entra token:
```typescript
import * as microsoftTeams from "@microsoft/teams-js";

await microsoftTeams.app.initialize();
const authToken = await microsoftTeams.authentication.getAuthToken();
// authToken is a JWT signed by Entra. Send as Bearer header to your backend.
```
The token audience (`aud` claim) will match the App URI configured in **Expose an API** (`api://{CLIENT_ID}`).

### 2.3 Token Validation on the Backend

When your API receives a Bearer JWT, validate it with the following checks before trusting any claims:

```csharp
// C# example using System.IdentityModel.Tokens.Jwt
var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
    $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration",
    new OpenIdConnectConfigurationRetriever());

var config = await configManager.GetConfigurationAsync();

var validationParameters = new TokenValidationParameters
{
    ValidateAudience  = true,
    ValidAudience     = $"api://{clientId}",         // Must match your Expose-an-API URI
    ValidateIssuer    = true,
    ValidIssuer       = $"https://login.microsoftonline.com/{tenantId}/v2.0",
    ValidateIssuerSigningKey = true,
    IssuerSigningKeys = config.SigningKeys,            // Fetched from Entra's public endpoint
    ValidateLifetime  = true,
    ClockSkew         = TimeSpan.FromMinutes(5)       // Allow slight clock drift
};

var handler = new JwtSecurityTokenHandler();
var principal = handler.ValidateToken(incomingToken, validationParameters, out _);
var userEmail = principal.FindFirst("preferred_username")?.Value;
```

### 2.4 Admin Consent vs. User Consent

| Type | When Required | Scope Examples |
|---|---|---|
| **User Consent** | App requests only user-specific data | `User.Read`, `Mail.Read`, `Calendars.Read` |
| **Admin Consent** | App requests tenant-wide data or app permissions | `Mail.Read.All`, `Sites.ReadWrite.All`, `TeamMember.Read.All` |

**Blocker:** Many production M365 tenants have **Consent Policies** that block user-consent for any app not pre-approved by the IT admin. Always identify which scopes need admin consent early in your project.

---

## 3. Microsoft Graph API

### 3.1 API Basics

| Property | Value |
|---|---|
| **Base URL** | `https://graph.microsoft.com/v1.0` (stable) |
| **Beta URL** | `https://graph.microsoft.com/beta` (preview, may break) |
| **Auth Header** | `Authorization: Bearer {access_token}` |
| **Content Type** | `application/json` |
| **Rate Limits** | Per-user: 10,000 requests/10 min; App-level varies per endpoint |

### 3.2 Key API Domains & Endpoints

#### User & Identity
```
GET /me                            → Current user profile
GET /me/photo/$value               → Profile photo binary
GET /users/{id or UPN}             → Specific user
GET /me/memberOf                   → Groups the user belongs to
```

#### Mail (Outlook)
```
GET  /me/messages                  → List mailbox messages
GET  /me/messages/{id}             → Read specific message
POST /me/messages                  → Draft new message
POST /me/sendMail                  → Send email immediately
GET  /me/mailFolders               → List mail folders
```

#### Calendar
```
GET  /me/events                    → List calendar events
POST /me/events                    → Create calendar event
GET  /me/calendar/calendarView     → Time-bounded view of events
```

#### Files (OneDrive)
```
GET  /me/drive/root/children       → List root files
GET  /me/drive/items/{id}          → Get specific item
PUT  /me/drive/root:/{path}:/content → Upload file (< 4MB, simple upload)
POST /me/drive/root:/{path}:/createUploadSession → Large file upload
```

#### SharePoint (via Graph)
```
GET  /sites/{siteId}                          → Site metadata
GET  /sites/{siteId}/lists                    → Lists in a site
GET  /sites/{siteId}/lists/{listId}/items     → List items
POST /sites/{siteId}/lists/{listId}/items     → Create list item
PATCH /sites/{siteId}/lists/{listId}/items/{itemId} → Update list item
DELETE /sites/{siteId}/lists/{listId}/items/{itemId} → Delete item
```

#### Teams
```
GET  /me/joinedTeams               → Teams the user belongs to
GET  /teams/{teamId}/channels      → Channels in a team
POST /teams/{teamId}/channels/{channelId}/messages → Send channel message
POST /chats/{chatId}/messages      → Send direct/group chat message
GET  /teams/{teamId}/members       → Team member list
```

### 3.3 Graph SDK Usage (C# / .NET)

**Installation:**
```xml
<PackageReference Include="Microsoft.Graph" Version="5.*" />
<PackageReference Include="Azure.Identity" Version="1.*" />
```

**Client Setup (Client Credentials):**
```csharp
var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
var graphClient = new GraphServiceClient(credential);

// Fetch a user's display name
var user = await graphClient.Users[userId].GetAsync();
Console.WriteLine(user.DisplayName);
```

**Client Setup (On-Behalf-Of):**
```csharp
var oboCredential = new OnBehalfOfCredential(
    tenantId, clientId, clientSecret, incomingUserToken);
var graphClient = new GraphServiceClient(oboCredential,
    new[] { "https://graph.microsoft.com/.default" });
```

### 3.4 Pagination

Graph paginates large result sets. Always handle the `@odata.nextLink` property:
```csharp
var messages = await graphClient.Me.Messages.GetAsync(config =>
{
    config.QueryParameters.Top = 50;
    config.QueryParameters.Select = new[] { "id", "subject", "from" };
});

var pageIterator = PageIterator<Message, MessageCollectionResponse>
    .CreatePageIterator(graphClient, messages, (msg) =>
    {
        Console.WriteLine(msg.Subject);
        return true; // continue iterating
    });

await pageIterator.IterateAsync();
```

### 3.5 Delta Queries (Change Tracking)

Instead of polling all records, Graph supports delta queries that return only changed items since the last sync. Highly recommended for performance:
```
GET /me/messages/delta
GET /me/calendarView/delta?startDateTime=...&endDateTime=...
GET /sites/{siteId}/lists/{listId}/items/delta
```

---

## 4. SharePoint Online APIs

### 4.1 Two API Surfaces

| API | Base URL | Notes |
|---|---|---|
| **Microsoft Graph** | `https://graph.microsoft.com/v1.0/sites/{siteId}/...` | Recommended; consistent auth model |
| **SharePoint REST API (legacy)** | `https://{tenant}.sharepoint.com/sites/{site}/_api/...` | Older; requires SharePoint-specific auth cookie |

Prefer Graph for new development. Use SharePoint REST only for SPFx Web Parts running within SharePoint itself (SPFx has access to the SharePoint context token natively).

### 4.2 Resolving a Site ID

SharePoint site IDs are not human-readable. Resolve them from hostname and path:
```
GET https://graph.microsoft.com/v1.0/sites/{tenant}.sharepoint.com:/sites/{siteName}
→ Returns { "id": "solvefy.sharepoint.com,{site-guid},{web-guid}" }
```

### 4.3 Working with SharePoint Lists

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

#### Querying with filters:
```
GET /sites/{siteId}/lists/{listId}/items?$filter=fields/Status eq 'Pending'&$expand=fields
```

#### Columns are defined in the List schema. To get the schema:
```
GET /sites/{siteId}/lists/{listId}/columns
```

### 4.4 Document Libraries & File Operations

#### Upload small files (< 4 MB) to a specific path:
```http
PUT /v1.0/sites/{siteId}/drives/{driveId}/root:/{folder}/{filename}:/content
Authorization: Bearer {token}
Content-Type: application/octet-stream

[binary content]
```

#### Upload large files via upload session:
```http
POST /v1.0/sites/{siteId}/drives/{driveId}/root:/{path}/{filename}:/createUploadSession
→ Returns { "uploadUrl": "https://..." }

PUT {uploadUrl}
Content-Range: bytes 0-{chunkSize-1}/{totalSize}
[chunk bytes]
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

### 4.5 Permissions Scopes for SharePoint

| Scope | Access Level |
|---|---|
| `Sites.Read.All` | Read all site collections and their content |
| `Sites.ReadWrite.All` | Create, update, delete items and files |
| `Files.ReadWrite.All` | Read and write OneDrive/SharePoint files |

> ⚠️ **Risk:** `Sites.ReadWrite.All` is a highly privileged scope and always requires admin consent. Consider using more targeted scopes if the SharePoint REST API is used within SPFx (uses the SP context implicitly).

---

## 5. Microsoft Teams Platform APIs

### 5.1 Teams Extensibility Surface Map

| Extension Type | What It Does | Tech |
|---|---|---|
| **Static/Configurable Tabs** | Embeds a web app inside Teams | `@microsoft/teams-js` |
| **Personal Tab** | Private tab for each user | Same |
| **Bot** | Conversational AI/automation agent | Bot Framework SDK |
| **Messaging Extension** | Search / action commands in compose bar | Bot Framework |
| **Meeting Apps** | Side panels / stage apps during meetings | teams-js + Meeting API |
| **Adaptive Card** | Rich interactive cards in messages | Adaptive Cards JSON schema |
| **Incoming Webhooks** | Simple one-way message posting to a channel | HTTP POST to webhook URL |

### 5.2 Teams Tabs — Tab App Lifecycle

Teams Tabs are web pages loaded in an iframe inside Teams. The tab app must use the Teams SDK to communicate with the host shell:

```typescript
import * as microsoftTeams from "@microsoft/teams-js";

await microsoftTeams.app.initialize();

// Get context: who is logged in, what team, what channel
const context = await microsoftTeams.app.getContext();
console.log(context.user?.displayName);
console.log(context.channel?.id);

// Get auth token for SSO
const token = await microsoftTeams.authentication.getAuthToken();
```

**App Manifest Structure (manifest.json):**
```json
{
  "manifestVersion": "1.16",
  "id": "{your-app-guid}",
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

### 5.3 Sending Messages to Teams Channels (Graph API)

#### Send a plain text message:
```http
POST /v1.0/teams/{teamId}/channels/{channelId}/messages
Authorization: Bearer {token}
Content-Type: application/json

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
        "version": "1.4",
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

> ⚠️ **Known Limitation:** `Action.Submit` in channel messages does NOT trigger bot webhook callbacks unless the card is sent by a Bot. Use `Action.OpenUrl` for channel-posted cards pointing to your app. For interactive button callbacks, deploy a Bot.

### 5.4 Incoming Webhooks (Quick & Simple Notifications)

Incoming Webhooks are the simplest way to post messages to a Teams channel without a registered app or Graph token:

1. In Teams: Open a channel → Apps → Search "Incoming Webhook" → Configure → Copy URL.
2. POST to the Webhook URL:
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

> ✅ **Advantage:** No Entra token required. Ideal for CI/CD alerts, monitoring hooks, and quick demos.  
> ⚠️ **Limitation:** One-way only. No reply tracking, no SSO, no user attribution.

### 5.5 Bots — Conversational Automation

Bots are registered as Entra app registrations with the Azure Bot Service. They enable:
- Respond to messages in chats / channels
- Proactively send messages without a user initiating contact
- Send cards with interactive `Action.Submit` callbacks

**Technology:** Azure Bot Framework SDK (C# or Node.js)

**Proactive message send flow:**
```csharp
// From saved conversation reference:
var reference = _store.GetConversationReference(userId);
await adapter.ContinueConversationAsync(
    _appId,
    reference,
    async (turnContext, cancellationToken) =>
    {
        await turnContext.SendActivityAsync(MessageFactory.Text("Your request was approved!"));
    },
    cancellationToken);
```

### 5.6 Required API Permissions for Teams

| Scope | Type | Purpose |
|---|---|---|
| `Team.ReadBasic.All` | Delegated | List teams |
| `Channel.ReadBasic.All` | Delegated | List channels |
| `ChannelMessage.Send` | Delegated | Send to channels as current user |
| `ChannelMessage.Read.All` | Application | Read all channel messages |
| `ChatMessage.Send` | Delegated | Send DMs |
| `TeamsAppInstallation.ReadWriteSelfForTeam` | Delegated | Install app in teams |

---

## 6. Outlook Add-in & Mail APIs

### 6.1 Outlook Add-in Architecture

Outlook Add-ins are built with standard HTML, CSS, and JavaScript running inside an Office-managed iframe embedded in Outlook Desktop (Win/Mac), Web (OWA), or Mobile. They use the **Office.js** SDK to communicate with the host:

```
office.js → Outlook Host Process → Mail Item Context
```

**Types of Outlook Add-ins:**

| Type | Context | Trigger |
|---|---|---|
| **Mail Read** | Reading pane | When any email is opened |
| **Mail Compose** | Compose window | When composing a new email |
| **Meeting Organizer/Attendee** | Calendar event form | When composing/viewing a meeting |

### 6.2 Add-in Manifest (manifest.xml)

Outlook add-ins are described by a legacy XML manifest:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<OfficeApp xmlns="http://schemas.microsoft.com/office/appforoffice/1.1"
           xsi:type="MailApp" ...>
  <Id>your-add-in-guid</Id>
  <ProviderName>Contoso</ProviderName>
  <DefaultLocale>en-US</DefaultLocale>
  <DisplayName DefaultValue="Request Manager" />
  <Description DefaultValue="Submit M365 requests directly from Outlook" />
  <Hosts>
    <Host Name="Mailbox" />
  </Hosts>
  <FormSettings>
    <Form xsi:type="ItemRead">
      <DesktopSettings>
        <SourceLocation DefaultValue="https://yourapp.com/addin.html" />
        <RequestedHeight>300</RequestedHeight>
      </DesktopSettings>
    </Form>
  </FormSettings>
  <Permissions>ReadWriteItem</Permissions>
  <Rule xsi:type="RuleCollection" Mode="Or">
    <Rule xsi:type="ItemIs" ItemType="Message" FormType="Read" />
  </Rule>
</OfficeApp>
```

> 📌 **New:** Microsoft is transitioning to a **Unified App Manifest** (JSON-based, same as Teams) that supports both Teams and Outlook. Prefer the JSON manifest for new projects targeting both surfaces.

### 6.3 Reading Mail Item Data (Office.js)

```javascript
Office.onReady(() => {
  const item = Office.context.mailbox.item;

  // Subject
  item.subject.getAsync(result => {
    if (result.status === Office.AsyncResultStatus.Succeeded) {
      console.log("Subject:", result.value);
    }
  });

  // Sender
  console.log("From:", item.from.displayName, item.from.emailAddress);

  // Body (HTML)
  item.body.getAsync(Office.CoercionType.Html, result => {
    console.log("Body HTML:", result.value);
  });

  // Attachments
  item.attachments.forEach(att => {
    console.log("Attachment:", att.name, att.size);
  });
});
```

### 6.4 Sending Mail via Graph API (Backend)

```http
POST /v1.0/me/sendMail
Authorization: Bearer {delegated token}
Content-Type: application/json

{
  "message": {
    "subject": "Your request REQ-00042 has been approved",
    "body": {
      "contentType": "HTML",
      "content": "<h2>Approved</h2><p>Your request has been reviewed and approved.</p>"
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

### 6.5 Graph Mail Permissions

| Scope | Access |
|---|---|
| `Mail.Read` | Read current user's emails |
| `Mail.ReadWrite` | Read, create, update, delete emails |
| `Mail.Send` | Send email as current user |
| `Mail.Read.All` | Admin: read all users' mail |
| `Mail.Send.Shared` | Send from shared mailbox |

### 6.6 Outlook REST API vs Graph API

Microsoft has **fully deprecated the Outlook REST API** (`outlook.office.com/api/v2.0`) as of November 2022. All mail integrations must now use Microsoft Graph (`graph.microsoft.com/v1.0/me/messages`).

---

## 7. Cross-Cutting Concerns

### 7.1 Throttling & Retry Strategies

Microsoft Graph enforces service-protection limits. Responses with `HTTP 429 Too Many Requests` include a `Retry-After` header indicating how long to wait.

**Retry Pattern:**
```csharp
private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
{
    int retryCount = 0;
    while (retryCount < 5)
    {
        try { return await operation(); }
        catch (ServiceException ex) when ((int)ex.ResponseStatusCode == 429)
        {
            var delay = ex.ResponseHeaders?.RetryAfter?.Delta ?? TimeSpan.FromSeconds(30);
            await Task.Delay(delay);
            retryCount++;
        }
    }
    throw new Exception("Max retries exceeded.");
}
```

The Graph SDK also has built-in retry middleware:
```csharp
var httpClient = GraphClientFactory.Create(new RetryHandler());
var graphClient = new GraphServiceClient(httpClient);
```

### 7.2 Token Caching

Tokens expire after **1 hour** (access tokens). Do not request a new token on every API call. Use the MSAL token cache:
```csharp
// MSAL in-memory cache (dev)
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithClientSecret(secret)
    .WithTenantId(tenantId)
    .Build();

var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
// MSAL automatically uses cached token if valid
```

For production, use a **distributed cache** (Redis, SQL) to share the token cache across multiple app instances:
```csharp
services.AddDistributedMemoryCache();
app.AddInMemoryTokenCaches(); // from Microsoft.Identity.Web
```

### 7.3 Microsoft.Identity.Web (Recommended Integration Library)

For ASP.NET Core or Azure Functions, use `Microsoft.Identity.Web` to handle the full auth flow:
```csharp
// Program.cs
services.AddMicrosoftIdentityWebApiAuthentication(Configuration, "AzureAd")
        .EnableTokenAcquisitionToCallDownstreamApi()
        .AddMicrosoftGraph(Configuration.GetSection("Graph"))
        .AddInMemoryTokenCaches();
```

### 7.4 Conditional Access & Zero Trust

Enterprise M365 tenants often have Conditional Access Policies that can block access from:
- Unregistered/unmanaged devices
- Specific geographic locations
- Apps not marked as compliant

Even with a valid token, Graph calls may return `HTTP 403` with a `AADSTS53003` error code if the tenant CA policies are too restrictive for your app registration.

---

## 8. Risks & Possible Blockers Reference

### 🔴 High-Impact Blockers

| Blocker | Description | Mitigation |
|---|---|---|
| **Admin Consent Not Granted** | IT admins must approve permissions like `Sites.ReadWrite.All`, `Mail.Read.All` | Identify all required permissions upfront. Request admin consent before development begins. Use the Graph Explorer tool to verify scopes. |
| **Sideloading Disabled by IT Policy** | Organization may block custom app sideloading for Teams | Use the Teams Admin Center to request app upload permissions, or publish to org app catalog instead of sideloading |
| **Tenant Conditional Access Blocking** | CA policies may block access from developer machines | Have the tenant admin add an exclusion for the development app registration |
| **Multi-Tenant vs. Single-Tenant App** | Multi-tenant apps require each external tenant to consent separately | Plan tenant scope early. For demos, use single-tenant (your own M365 dev tenant). |
| **SharePoint Site Permissions** | The service account or application must have Site Collection Admin or explicit list-level permissions | Configure permissions via PnP PowerShell or SharePoint Admin Center before deploying |

### 🟡 Medium-Impact Risks

| Risk | Description | Mitigation |
|---|---|---|
| **Graph Beta API Breaking Changes** | `/beta` endpoint is not SLA-covered and may change without notice | Never use beta endpoints in production. Only use for prototyping. |
| **Token Scope Mismatch** | Token issued for `api://{ClientId}` cannot call Graph unless OBO is implemented | Ensure backend performs OBO token swap before calling Graph on user's behalf |
| **Teams Iframe CSP Restrictions** | Teams blocks iframes from unapproved domains | Always add your frontend domain to `validDomains` in manifest.json |
| **Office.js Version Drift** | Different Outlook clients (Desktop, OWA, Mobile) support different requirement sets | Test on all target clients. Use `isSetSupported('Mailbox', '1.8')` guard checks |
| **Clock Skew on Token Validation** | Server clocks drifting cause valid tokens to appear expired | Set `ClockSkew = TimeSpan.FromMinutes(5)` in validation parameters |
| **Large File Upload Failures** | Single-request file uploads fail for files > 4 MB | Always use Upload Session API for files larger than 4 MB |
| **Throttling Under Load** | Bursting many Graph calls (e.g., bulk imports) can trigger service limits | Implement exponential backoff. Batch requests using `$batch` endpoint. |

### 🟢 Low-Impact / Awareness Items

| Item | Notes |
|---|---|
| **Delegated vs. Application Scopes** | Delegated = user context; Application = app context. Some scopes only exist as one type — check the Graph permissions reference |
| **Guest Users** | Guest users in a tenant may not have full M365 licenses, limiting their Graph data access |
| **Outlook Mobile Limitations** | Some Office.js APIs and add-in form factors are not supported on Outlook Mobile |
| **Teams Meeting Apps** | Meeting stage / side panel apps require additional manifest `meetingSurfaces` configuration |
| **Sovereign Cloud Endpoints** | Government/China clouds use different Graph endpoints (e.g., `graph.microsoft.us`). Account for this if targeting GCC or GCC-High |
| **Unified Manifest (New)** | Teams and Outlook are converging to a single JSON manifest. Plan migration from legacy XML Outlook manifests to stay current |
| **GDPR / Data Residency** | Reading user mailboxes and SharePoint data is subject to data residency policies. Verify where your Azure resources are hosted |

---

## 9. Quick Reference Cheat Sheet

```
╔══════════════════════════════════════════════════════════════════════════╗
║                   M365 API QUICK REFERENCE                              ║
╠══════════════════════════════════════════════════════════════════════════╣
║ TOKEN ENDPOINT   https://login.microsoftonline.com/{tenant}/oauth2/v2.0 ║
║ GRAPH BASE       https://graph.microsoft.com/v1.0                       ║
║ JWKS (keys)      https://login.microsoftonline.com/common/discovery/     ║
║                  v2.0/keys                                              ║
║ TOKEN LIFETIME   Access Tokens: 1 hour | Refresh Tokens: 90 days        ║
║ RATE LIMIT       10,000 requests per app per 10 minutes per user         ║
║ MAX FILE (simple) 4 MB                                                  ║
╠══════════════════════════════════════════════════════════════════════════╣
║ KEY SCOPES                                                              ║
║  User.Read             Read your own profile                            ║
║  Mail.Send             Send email as current user                       ║
║  Mail.ReadWrite        Read/write mail                                  ║
║  Sites.ReadWrite.All   SharePoint read/write (admin consent)            ║
║  Files.ReadWrite.All   OneDrive/SharePoint files (admin consent)        ║
║  ChannelMessage.Send   Post to Teams channels                           ║
║  TeamMember.Read.All   Read team memberships (admin consent)            ║
╠══════════════════════════════════════════════════════════════════════════╣
║ KEY NUGET PACKAGES (.NET)                                               ║
║  Microsoft.Graph                 Graph SDK client                       ║
║  Microsoft.Identity.Web          Auth integration for ASP.NET/Functions ║
║  Azure.Identity                  DefaultAzureCredential & managed ID    ║
║  Microsoft.IdentityModel.Tokens  JWT token validation primitives        ║
╠══════════════════════════════════════════════════════════════════════════╣
║ KEY NPM PACKAGES (JS/TS)                                                ║
║  @microsoft/teams-js             Teams Tab SDK                          ║
║  @microsoft/microsoft-graph-client  Graph SDK for browser/Node          ║
║  @azure/msal-browser             MSAL for single-page apps              ║
║  @azure/msal-node                MSAL for server-side Node apps         ║
║  office-js                       Office Add-in runtime (CDN only)       ║
╚══════════════════════════════════════════════════════════════════════════╝
```

---

*Last Updated: 2026-08 | Covers Graph API v1.0, Teams JS SDK 2.x, Office.js 1.x, Microsoft.Identity.Web 2.x*
