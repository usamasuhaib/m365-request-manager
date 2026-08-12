---
name: M365 DemoApp Architecture Spine
type: architecture-spine
purpose: build-substrate
altitude: initiative
paradigm: layered-hexagonal
scope: Core backend APIs (Azure Functions C#), React/Vite frontend, and M365/SharePoint integration.
status: final
created: 2026-08-12
updated: 2026-08-12
binds:
  - FR-1
  - FR-5
  - FR-6
  - FR-7
  - FR-8
  - FR-9
  - FR-10
  - FR-11
sources:
  - prd.md
companions: []
---

# Architecture Spine — M365 DemoApp

## Design Paradigm
The application uses a **Layered Hexagonal (Ports & Adapters)** design paradigm to isolate the core business logic and state machine from the Microsoft Graph SDK and the HTTP presentation layer.

- **Presentation Layer (Adapters):** Azure HTTP-Triggered Functions
- **Application Services (Core Ports):** Services defining request validation, status transitions, and file uploads.
- **Infrastructure Layer (Adapters):** Microsoft Graph Service calling SharePoint Lists and Libraries.

---

## Invariants & Rules

### AD-1 — Shared Data Isolation
- **Binds:** FR-8, FR-9
- **Prevents:** Direct browser-based SharePoint API calls exposing credentials or bypassing state machine transitions.
- **Rule:** The React frontend must never execute Graph or SharePoint REST calls directly. All data access and file uploads must route through the secure Azure Functions API.

### AD-2 — OAuth 2.0 On-Behalf-Of (OBO) Flow
- **Binds:** FR-1, FR-6, FR-8
- **Prevents:** Privilege escalation, token spoofing, and anonymous database writes.
- **Rule:** The Azure Functions backend must validate the incoming Entra ID JWT user access token and exchange it for a Microsoft Graph token using the OAuth 2.0 On-Behalf-Of flow. The backend must access SharePoint using this delegated user identity, ensuring native auditing and user-level permissions.

### AD-3 — Relational Integrity in SharePoint Lists
- **Binds:** FR-8
- **Prevents:** Orphaned database rows and relational breakdown in SharePoint.
- **Rule:** Since SharePoint Lists lack referential integrity constraints (foreign keys), the application service layer must enforce relations programmatically. It must validate that a category exists before assigning it to a Request, and must delete comments/approvals from separate lists if a Request is deleted.

### AD-4 — API Request Idempotency
- **Binds:** FR-7
- **Prevents:** Double-submission of requests during network retries or browser click storms.
- **Rule:** The POST `/api/requests` endpoint must require a `Client-Request-Id` UUID header. The backend must cache successful creation outcomes against this UUID for 5 minutes. If a duplicate is received, the backend must return the cached response instead of creating a new item in SharePoint.

### AD-5 — File Isolation and Access Control
- **Binds:** FR-4, FR-9
- **Prevents:** Cross-request document exposure and folder naming conflicts.
- **Rule:** Each request must store its files inside a folder named after the request number (`REQ-00001`). Direct URLs to files in SharePoint must not be exposed to the client; files must be downloaded via a proxy endpoint `GET /api/requests/{id}/documents/{docId}` that validates user permissions.

---

## Consistency Conventions

| Concern | Convention |
| --- | --- |
| Naming | APIs named `/api/requests`, services named `IRequestService`, repositories named `ISharePointRepository`. |
| Data & formats | Request numbers formatted as `REQ-00000`. Dates returned in ISO 8601 UTC formats. |
| State & cross-cutting | Token validation via JWT middleware; errors returned as structured JSON: `{ "success": false, "message": "error description" }`. |

---

## Stack

| Name | Version |
| --- | --- |
| .NET | 8.0 (LTS) |
| Azure Functions | v4 (Isolated Worker Model) |
| Microsoft Graph .NET SDK | v5.x |
| React | 18.x |
| Vite | 5.x |
| Fluent UI React | 9.x |

---

## Structural Seed

```text
{root}/
  src/
    RequestManager.Functions/     # .NET 8 Azure Functions
      Functions/                  # HTTP Trigger endpoints
      Services/                   # Core business logic (Ports)
      Infrastructure/             # Graph / SharePoint repositories (Adapters)
      Models/                     # Domain & DTO records
      Program.cs
    RequestManager.Frontend/      # React + Vite frontend
      src/
        components/               # Fluent UI components
        pages/                    # Dashboard, details, creation
        services/                 # API client & auth
  manifest/
    manifest.json                 # Teams/Outlook unified app package
```

---

## Deferred
- Multi-region failover configuration (deferred; single-region Consumption plan fits demo needs).
- Automated CI/CD deployment pipelines (deferred; local script-based deployments are sufficient).
