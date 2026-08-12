---
title: Microsoft 365 Request & Document Manager
status: final
created: 2026-08-12
updated: 2026-08-12
---

# PRD: Microsoft 365 Request & Document Manager

## 0. Document Purpose
This Product Requirements Document (PRD) defines the functional requirements, data schemas, security bounds, and integration points for the **Microsoft 365 Request & Document Manager** demo application. It is written to serve as the single source of truth for the system architect, frontend developer, backend developer, and QA engineer to execute this proof of concept (POC).

This document details the Teams dashboard experience, the Outlook email add-in, the C# Azure Functions REST API, and the SharePoint database layer.

---

## 1. Vision
The **Microsoft 365 Request & Document Manager** is a technical demonstration showcasing how a line-of-business application can be natively embedded within Microsoft 365 (Teams, Outlook, and Office.com). By utilizing Microsoft Entra ID for Single Sign-On (SSO) and Microsoft Graph to read/write data in SharePoint Online, the application keeps business logic in secure Azure Functions while storing application data and files directly in the customer's existing M365 tenant. This architecture guarantees data residency, low operation costs, and seamless user experiences on Teams and Outlook.

---

## 2. Target User

### 2.1 Jobs To Be Done
- **Request Submission (Employee):** As an employee, I want to submit requests and attach supporting files from my primary collaboration tool (Teams or Outlook) so that I don't lose time logging into external web portals.
- **Request Approval (Manager):** As an authorized manager/approver, I want to review pending requests, check comments, open attachments, and approve/reject them directly from my Teams workspace.
- **Developer/Architect Reference:** As an M365 architect, I want to see a secure reference architecture showing how to implement Delegated permissions and token validation between React and .NET Azure Functions.

### 2.2 Non-Users (v1)
- **External Tenants:** Users outside the host tenant are out of scope. The POC will strictly target single-tenant enterprise deployments.
- **Anonymous Users:** Unauthenticated access is blocked. Every action must have an identified Entra ID identity.

### 2.3 Key User Journeys

#### UJ-1. Creating and Tracking a Request from Teams (Priya)
- **Persona + context:** Priya, a team lead, needs to request budget approval for a laptop.
- **Entry state:** Authenticated automatically via Teams SSO. She opens the Request Manager personal tab.
- **Path:** 
  1. Priya sees the Dashboard showing her request counts.
  2. She clicks "Create Request" and fills in: Title ("Developer Laptop Upgrade"), Description, Category ("Hardware"), and Priority ("High").
  3. She clicks "Add Attachment" and uploads `invoice.pdf`.
  4. She clicks "Submit".
- **Climax:** The app calls the Azure Function `POST /api/requests`. The function creates a SharePoint item in the `Requests` list, creates folder `RequestDocuments/REQ-00001/` in the SharePoint Document Library, uploads the file, and returns `REQ-00001`. The UI shows a success banner with the request number.
- **Resolution:** Priya is returned to her "My Requests" list where `REQ-00001` is marked as "Submitted".
- **Edge case:** If Priya uploads a non-supported file (e.g., `.exe` or `.zip`), the UI immediately flags the error on-screen and disables the submit button.

#### UJ-2. Reviewing and Approving a Request (Winston)
- **Persona + context:** Winston, a department manager, is responsible for budget approvals.
- **Entry state:** Authenticated via Teams SSO. He sees a badge indicating 1 pending approval.
- **Path:**
  1. Winston opens the "Pending Approvals" view in the tab.
  2. He selects `REQ-00001`. He reads Priya's description and clicks the attachment link to open `invoice.pdf`.
  3. He types in the comment box: "Approved. Proceed with standard procurement."
  4. He clicks "Approve".
- **Climax:** The backend verifies Winston's membership in the Approver security group, updates the status of the SharePoint list item to "Approved," writes an entry to the `RequestApprovals` list, and logs the comment.
- **Resolution:** The request moves to Winston's "Approved Requests" history, and Priya's dashboard updates to show her request is "Approved."

#### UJ-3. Logging a Request from Outlook Email (Priya)
- **Persona + context:** Priya receives an email invoice from a software vendor.
- **Entry state:** Authenticated in Outlook on the desktop/web.
- **Path:**
  1. Priya opens the email.
  2. She opens the Request Manager Outlook Add-in from the email pane.
  3. The add-in pre-fills the Title with the email subject and Description with the email sender + body snippet.
  4. Priya selects the "Software License" category and clicks "Create Request from Email."
- **Climax:** The add-in calls `POST /api/outlook/create-request`, passing the email context and attachment metadata. The Azure Function creates the SharePoint items and returns success.
- **Resolution:** Priya receives an in-add-in confirmation with the new request number.

---

## 3. Glossary
- **Request** — The core transaction record representing a business request (e.g. purchases, access requests).
- **Request Number** — A unique, human-readable identifier formatted as `REQ-` followed by a padded 5-digit number (e.g. `REQ-00001`).
- **Request Category** — The classification of the request (e.g. Hardware, Software, Expense).
- **Request Comment** — A text note appended to a request by the submitter or an approver.
- **Request Approval** — A record of the approval action taken by an authorized user, logging comments and timestamp.
- **Request Document** — A file uploaded in support of a request, stored in a request-specific subfolder in the Document Library.
- **Approver** — A tenant user belonging to the designated Entra ID / SharePoint Approver role group.

---

## 4. Features

### 4.1 Teams Dashboard & Personal Tab
**Description:** A tabbed React application running in Teams containing Dashboard, My Requests, Create Request, and Approvals views.

**Functional Requirements:**
- **FR-1: Teams SSO Login:** The client must use the `@microsoft/teams-js` SDK to retrieve an authentication token silently.
  - *Consequences:* If token retrieval fails, the app must display a login prompt redirecting the user to Entra ID.
- **FR-2: Dashboard Metrics:** The system must query `GET /api/requests` and display cards for Total Requests, Pending, Approved, and Rejected.
- **FR-3: Form Submission:** The create request form must allow users to supply a Title, Description, Category, Priority, and optional File.
  - *Consequences:* Validation must run: Title is required (max 100 chars), Description is required (max 1000 chars), Category is required, Priority is required (Low, Medium, High).
- **FR-4: Client-Side File Validation:** The file input must restrict uploads to `.pdf`, `.docx`, `.png`, and `.jpg` with a maximum size of 10MB.
- **FR-5: Detailed View:** Selecting any request must load the full details, including a list of comments and downloadable files.

---

### 4.2 C# Azure Functions REST API
**Description:** A backend API written in C# on .NET 8 that encapsulates the business logic and communicates with Microsoft Graph.

**Functional Requirements:**
- **FR-6: Bearer Token Validation:** The Azure Function must validate the Entra ID JWT token passed in the Authorization header.
  - *Consequences:* If invalid, expired, or missing, the API must return `401 Unauthorized`.
- **FR-7: Idempotency:** The backend must require a `Client-Request-Id` header for POST requests to prevent double-creation.
  - *Consequences:* If a request with the same ID was processed within the last 5 minutes, return the cached `REQ-` details.
- **FR-8: Graph API Directory & SharePoint Integration:** The backend must connect to Microsoft Graph using the validated user token (On-Behalf-Of flow) to run queries on SharePoint.
  - *Consequences:* The service must query SharePoint lists for `Requests`, `RequestCategories`, etc., mapping responses to DTOs.
- **FR-9: Document Folder Isolation:** During file upload, the backend must create a folder inside the `RequestDocuments` library using the `Request Number` (e.g. `REQ-00001/`) and save the file inside it.
  - *Consequences:* Direct public URL access to the file must not be exposed; files must be read via the secure Graph endpoint.

---

### 4.3 Outlook Email Add-in
**Description:** An Office JavaScript add-in that opens a task pane next to an active email.

**Functional Requirements:**
- **FR-10: Email Context Extraction:** The add-in must use `Office.context.mailbox` to read the active email subject, sender email, and body snippet.
- **FR-11: Submit Email Request:** The add-in must call `POST /api/outlook/create-request` to create a request directly from the email data.

---

## 5. Data Schemas (SharePoint Lists)

### 5.1 Requests List
| Field Name | Data Type | Description |
| :--- | :--- | :--- |
| **ID** | Counter / Integer | Primary ID in SharePoint |
| **RequestNumber** | Text | e.g. `REQ-00001` |
| **Title** | Text | Title of the request |
| **Description** | Note (HTML/Text) | Detailed description |
| **Category** | Lookup / Choice | Maps to `RequestCategories` |
| **Priority** | Choice | `Low`, `Medium`, `High` |
| **Status** | Choice | `Draft`, `Submitted`, `Pending Approval`, `Approved`, `Rejected`, `Completed` |
| **SubmittedBy** | Text | Display name of submitter |
| **SubmittedByEmail** | Text | Email of submitter |
| **SubmittedDate** | Date/Time | Submission timestamp |
| **AssignedTo** | Text (Lookup) | Assigned approver |
| **ApprovedBy** | Text | Approver name |
| **ApprovedDate** | Date/Time | Approval timestamp |
| **RejectedBy** | Text | Rejecter name |
| **RejectedDate** | Date/Time | Rejection timestamp |

### 5.2 RequestCategories List
| Field Name | Data Type | Description |
| :--- | :--- | :--- |
| **ID** | Counter / Integer | Primary ID |
| **Name** | Text | e.g., `Hardware`, `Software`, `Expense` |
| **Description** | Text | Category explanation |
| **IsActive** | Yes/No (Boolean) | Active indicator |

### 5.3 RequestComments List
| Field Name | Data Type | Description |
| :--- | :--- | :--- |
| **ID** | Counter / Integer | Primary ID |
| **RequestId** | Integer | ID matching `Requests` |
| **Comment** | Note | Comment text |
| **CommentedBy** | Text | Author name |
| **CommentedDate** | Date/Time | Timestamp |

### 5.4 RequestApprovals List
| Field Name | Data Type | Description |
| :--- | :--- | :--- |
| **ID** | Counter / Integer | Primary ID |
| **RequestId** | Integer | ID matching `Requests` |
| **Approver** | Text | Approver name |
| **Status** | Choice | `Approved` or `Rejected` |
| **Comments** | Note | Approver notes |
| **ActionDate** | Date/Time | Decision timestamp |

---

## 6. Request Status Workflow (State Machine)
The state transitions are governed strictly by the Azure Functions backend:

```
  [ Draft ]
      │
      ▼ (Submit Action)
[ Submitted ] ────► [ Rejected ]
      │ (If Approver assigned)
      ▼
[ Pending Approval ] ────► [ Rejected ]
      │ (Approve Action)
      ▼
  [ Approved ]
      │
      ▼ (Fulfillment Action)
 [ Completed ]
```

**State Validation Rules:**
1. **Submit Constraints:** Only requests in `Draft` state can be submitted.
2. **Approval Constraints:** Only requests in `Submitted` or `Pending Approval` states can be approved or rejected.
3. **Immutability:** Once a request is marked `Completed` or `Rejected`, no fields can be updated.
4. **Identity Enforcement:** The backend matches the caller's validated token identity against the `Requests` `AssignedTo` or the authorized `Approver` security group before allowing status updates.

---

## 7. Non-Goals (Explicit)
- **No Multi-Tenancy:** The demo is built for single-tenant installation. Multi-tenant SaaS orchestration is deferred.
- **No Complex Routing Engine:** Request assignment is direct. There is no automated routing matrix based on request value.
- **No Live Graph Webhooks:** We will not configure Graph subscription webhooks for change notifications.
- **No Public API Access:** The Azure Function CORS will block all requests except the configured frontend origin.

---

## 8. MVP Scope

### 8.1 In Scope
- Single-page application built on Vite + React + TypeScript + Fluent UI.
- C# Azure Functions using isolated worker model on .NET 8.
- Authenticated via MSAL.js in the browser and JWT Validation on the API backend.
- Storage in SharePoint Lists and Document Libraries.
- Teams personal tab manifest and Outlook desktop/web add-in XML manifest.

### 8.2 Out of Scope for MVP
- Power Automate flow triggers.
- Push notifications/SMS alerts.
- Dedicated SQL Database for auditing (all audit trails are written to the `RequestApprovals` list).

---

## 9. Success Metrics
- **SM-1 (Operational):** 100% of submitted requests successfully generate SharePoint list entries and document folder trees.
- **SM-2 (Performance):** Average API latency for `POST /api/requests` (creating items and folders) remains under 1.5 seconds.
- **SM-C1 (Counter-Metric):** Do not optimize document loading times by caching files locally on the frontend or backend; all files must be retrieved in real-time from SharePoint to ensure data privacy.

---

## 10. Open Questions
1. **Approver Role Verification:** Should we check Approver roles using an Entra ID security group (retrieved via Graph `/groups`), or a SharePoint List containing authorized emails?
   * *Recommendation:* An Entra ID Security Group (e.g. "M365 Request Approvers") is cleaner and aligns with enterprise directory practices.
2. **Document Library Paths:** Is the name `RequestDocuments` hardcoded, or read from `local.settings.json`?
   * *Recommendation:* Keep it configurable via backend app settings (`SharePointDocumentsLibraryName`).

---

## 11. Assumptions Index
- `[ASSUMPTION: Developer has access to an M365 tenant with tenant-admin rights to grant Graph delegated permissions during setup.]`
- `[ASSUMPTION: The Azure Function will be deployed to a Consumption Plan, minimizing costs under the free grant limit.]`
- `[ASSUMPTION: The frontend React app will be served via Azure Static Web Apps (Free tier).]`
