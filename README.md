# Microsoft 365 Request & Document Manager Demo Application

This repository contains the source code and deployment specifications for the **Microsoft 365 Request & Document Manager** demo application. It is a technical proof of concept (POC) demonstrating a unified M365 app architecture running natively across **Microsoft Teams** and **Microsoft Outlook**, with data and documents securely stored in **SharePoint Online** using **Microsoft Graph** and **C# Azure Functions** (.NET 8).

---

## 🗺️ Project Architecture Overview

The application is structured to show a clean separation of concerns, ensuring that the frontend never directly exposes SharePoint secrets or sensitive client credentials.

```
                         MICROSOFT 365 SHELL
                                 │
             ┌───────────────────┼───────────────────┐
             ▼                   ▼                   ▼
        TEAMS TAB          OUTLOOK ADD-IN      MICROSOFT 365 WEB
             │                   │
             └─────────┬─────────┘
                       ▼
               React / TypeScript
             Shared Web Experience
                       │
                     HTTPS (Bearer Token JWT)
                       ▼
             ┌───────────────────┐
             │  Azure Functions  │
             │     .NET 8        │
             │                   │
             │  Validate Token   │
             │  Exchange (OBO)   │
             │  Business Logic   │
             └─────────┬─────────┘
                       ▼
                Microsoft Graph
                       │
             ┌─────────┴─────────┐
             ▼                   ▼
      SharePoint Lists    Document Library
      (Requests, etc.)    (Isolated folders)
```

---

## 📂 Documentation Index

Detailed setup and reference files are located in the `docs/` folder:

1. **[architecture.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/architecture.md)**: Technical design invariants, layers, and token flows.
2. **[setup.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/setup.md)**: Master configuration checklist from scratch.
3. **[local-development.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/local-development.md)**: Local debugging guidelines, dev tunnels, and settings.
4. **[azure-deployment.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/azure-deployment.md)**: Deploing static assets and C# functions to Azure Consumption tier.
5. **[entra-id.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/entra-id.md)**: Entra ID app registrations, client IDs, redirects, and SSO config.
6. **[microsoft-graph.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/microsoft-graph.md)**: Graph API scopes, permissions, and service contracts.
7. **[sharepoint-setup.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/sharepoint-setup.md)**: Provisioning lists, columns, lookup fields, and document folders.
8. **[teams-setup.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/teams-setup.md)**: Manifest schema 1.16, app package packaging, and sideloading.
9. **[outlook-setup.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/outlook-setup.md)**: Office.js mailbox integration, manifest, and sideloading.
10. **[permissions.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/permissions.md)**: Privilege matrices, security groups, and role mappings.
11. **[security.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/security.md)**: CORS, JWT validation, file upload sanitization, and secrets management.
12. **[testing.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/testing.md)**: Mocking strategy, unit tests, and integration E2E checklist.
13. **[marketplace.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/marketplace.md)**: AppSource/Teams Store validation criteria and submission guide.
14. **[troubleshooting.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/troubleshooting.md)**: Common auth, Graph throttling, and local tunnel failure resolutions.

---

## 📋 Agile Planning & Specifications

The Agile methodology specifications and backlog items are stored in the core planning folder:

* **[Product Requirements Document (PRD)](file:///c:/Solvefy%20Projects/M365%20DemoApp/_bmad-output/planning-artifacts/prd.md)**: Core features, data models, state machine rules, and user journeys.
* **[Architecture Spine](file:///c:/Solvefy%20Projects/M365%20DemoApp/_bmad-output/planning-artifacts/ARCHITECTURE-SPINE.md)**: System design invariants (AD-1 to AD-5), technical stack, and namespace boundaries.
* **[Agile Epics & Stories Breakdown](file:///c:/Solvefy%20Projects/M365%20DemoApp/_bmad-output/planning-artifacts/epics.md)**: Complete sprint backlog (3 Epics, 11 User Stories) with Given/When/Then acceptance criteria.
* **[Live Demo Script Guide](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/demo-guide.md)**: Step-by-step scripts for demonstrating all CRUD features, security gates, and Outlook extensions.
* **[M365 Tech Notes Reference](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/m365-integration-notes.md)**: Architecture details of OAuth 2.0 SSO, Microsoft Graph call routing, SharePoint file libraries, and Teams channel notifications.

---

## ⚡ Tech Stack Quick Start

### Frontend
- **Framework:** React 18, TypeScript, Vite
- **UI Toolkit:** Fluent UI React v9
- **M365 Integration:** `@microsoft/teams-js` v2.x, Office.js

### Backend
- **Framework:** .NET 8 (LTS)
- **Host:** Azure Functions v4 (Isolated Worker Model)
- **Integration:** Microsoft Graph .NET SDK v5.x
