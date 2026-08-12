---
stepsCompleted:
  - Step 1: Validate Prerequisites and Extract Requirements
  - Step 2: Design Epic List
  - Step 3: Generate Epics and Stories
  - Step 4: Final Validation
inputDocuments:
  - prd.md
  - ARCHITECTURE-SPINE.md
---

# M365 DemoApp - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for M365 DemoApp, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements
- **FR-1: Dashboard Metrics** — The system must display summary cards for Total Requests, Pending Requests, Approved Requests, and Rejected Requests.
- **FR-2: Recent Requests Table** — The system must show a table of the 5 most recent requests submitted by the logged-in user.
- **FR-3: Form Validation** — The request form must require Title (max 100 chars), Description (max 1000 chars), Category, and Priority.
- **FR-4: Client-Side File Validation** — The file input must restrict uploads to `.pdf`, `.docx`, `.png`, and `.jpg` with a maximum size of 10MB.
- **FR-5: Role-based Approval** — Only users in the designated Approvers list/group can approve or reject requests.
- **FR-6: Bearer Token Validation** — The Azure Function must validate the Entra ID JWT token passed in the Authorization header.
- **FR-7: Idempotency** — The backend must require a `Client-Request-Id` header for POST requests to prevent double-creation.
- **FR-8: Graph API Directory & SharePoint Integration** — The backend must connect to Microsoft Graph using the validated user token (On-Behalf-Of flow) to run queries on SharePoint Lists.
- **FR-9: Document Folder Isolation** — During file upload, the backend must create a folder inside the `RequestDocuments` library using the `Request Number` (e.g. `REQ-00001/`) and save the file inside it.
- **FR-10: Email Context Extraction** — The add-in must use `Office.context.mailbox` to read the active email subject, sender email, and body snippet.
- **FR-11: Submit Email Request** — The add-in must call `POST /api/outlook/create-request` to create a request directly from the email data.

### NonFunctional Requirements
- **NFR-1 (Integrity):** 100% of request creations and approvals successfully write back to SharePoint.
- **NFR-2 (Performance):** SSO login completes in under 2 seconds. Average API latency for `POST /api/requests` remains under 1.5 seconds.
- **NFR-3 (Privacy):** All request documents must be retrieved in real-time from SharePoint; files must not be cached locally on the client or backend.
- **NFR-4 (Cost):** The solution must deploy to Azure Functions Consumption tier and Azure Static Web Apps (Free tier) to minimize costs.

### Additional Requirements
- **AD-1 (Shared Data Isolation):** The React frontend must never execute Graph or SharePoint REST calls directly. All data access must route through the Azure Functions API.
- **AD-2 (OAuth 2.0 On-Behalf-Of Flow):** The Azure Functions backend must validate the incoming Entra ID JWT user access token and exchange it for a Graph token using the OBO flow.
- **AD-3 (Relational Integrity in SharePoint):** The backend service layer must programmatically validate list relationships and handle cascade operations.
- **AD-4 (API Request Idempotency):** Cache successful creation outcomes against `Client-Request-Id` UUID for 5 minutes and return cached response on duplicates.
- **AD-5 (File Isolation and Access Control):** Proxy file downloads through a secure endpoint `GET /api/requests/{id}/documents/{docId}` that validates user permissions.

### UX Design Requirements
*(None provided as a standalone UX Design document does not exist yet. Screen behaviors are governed by FR-1 through FR-5.)*

### FR Coverage Map

- **FR-1 (Dashboard Metrics):** Epic 1 - Request Management Core
- **FR-2 (Recent Requests Table):** Epic 1 - Request Management Core
- **FR-3 (Form Validation):** Epic 1 - Request Management Core
- **FR-4 (Client-Side File Validation):** Epic 2 - Document Management
- **FR-5 (Role-based Approval):** Epic 1 - Request Management Core
- **FR-6 (Bearer Token Validation):** Epic 1 - Request Management Core
- **FR-7 (Idempotency):** Epic 1 - Request Management Core
- **FR-8 (Graph API & SharePoint Integration):** Epic 1 - Request Management Core
- **FR-9 (Document Folder Isolation):** Epic 2 - Document Management
- **FR-10 (Email Context Extraction):** Epic 3 - Outlook Extensibility
- **FR-11 (Submit Email Request):** Epic 3 - Outlook Extensibility

## Epic List

### Epic 1: Request Management Core
Employees can securely authenticate via SSO and track their requests on a unified Teams Dashboard. Managers can review request metadata and approve or reject submissions, with all state changes backed up in SharePoint Lists.
**FRs covered:** FR-1, FR-2, FR-3, FR-5, FR-6, FR-7, FR-8

### Epic 2: Document Management
Users can attach supporting files during request creation, and approvers can view or download those files securely. Files are isolated inside request-specific SharePoint subfolders and proxied securely through the Azure Function backend.
**FRs covered:** FR-4, FR-9

### Epic 3: Outlook Extensibility
Employees can view emails inside Outlook and log new requests directly from their inbox without context-switching, extracting the sender, subject, body, and attachments as request parameters.
**FRs covered:** FR-10, FR-11

---

## Epic 1: Request Management Core

### Epic Goal:
Establish the core M365 authentication flow, SharePoint data models, C# Azure Function CRUD endpoints, and the React Teams Dashboard UI personal tab.

### Story 1.1: Local Workspace and C# API Health Checks
As a developer,
I want to set up the local C# .NET 8 Azure Functions project structure and test a health endpoint,
So that I can verify my local development runtime before writing business logic.

**Acceptance Criteria:**

**Given** the workspace is initialized
**When** I run `func start` on the backend project
**Then** the local runtime starts successfully on port 7071
**And** calling `GET http://localhost:7071/api/health` returns `200 OK` with `{ "status": "healthy" }`.

### Story 1.2: Entra ID App Registrations & JWT Auth Middleware
As a developer,
I want to secure the backend API endpoints using Entra ID authentication and validate tokens,
So that only verified tenant users can interact with application data.

**Acceptance Criteria:**

**Given** an Entra ID application registration is configured
**When** I request `GET /api/me` with a valid Bearer token in the Authorization header
**Then** the API returns `200 OK` containing the user's name and email
**When** I request the API with an expired, missing, or malformed token
**Then** the API returns `401 Unauthorized`.

### Story 1.3: SharePoint Provisioning & Graph Service Integration
As an administrator,
I want to run a setup configuration that creates the required `Requests` and `RequestCategories` Lists in SharePoint,
So that the application has a structured database layer ready for data operations.

**Acceptance Criteria:**

**Given** the Microsoft Graph client is authenticated in the backend
**When** the initialization routine runs
**Then** it creates lists `Requests` and `RequestCategories` with the specified schemas
**And** populates `RequestCategories` with seed values (Hardware, Software, Expense).

### Story 1.4: Teams Tab Dashboard & Requests Table UI
As an employee,
I want to view a dashboard in Teams showing my request metrics and recent submissions,
So that I can monitor my open and completed tickets in real-time.

**Acceptance Criteria:**

**Given** I am logged into Microsoft Teams
**When** I open the Request Manager personal tab
**Then** the app performs silent SSO login via the `@microsoft/teams-js` SDK
**And** displays cards with counts for Total, Pending, Approved, and Rejected requests
**And** lists the 5 most recent requests in a table.

### Story 1.5: Submit Request Form (No Attachment)
As an employee,
I want to fill out and submit a request form with a Title, Description, Category, and Priority,
So that I can register a new request.

**Acceptance Criteria:**

**Given** I am on the "Create Request" screen in Teams
**When** I fill in the form fields (Title, Description, Category, Priority) and click "Submit"
**Then** the client sends a `POST /api/requests` containing the `Client-Request-Id` UUID
**And** the backend writes a new item to the SharePoint `Requests` list and returns `REQ-XXXXX`
**And** a duplicate POST with the same `Client-Request-Id` within 5 minutes returns the same request without duplicating it.

### Story 1.6: Review and Action Approvals
As an authorized manager,
I want to view pending requests and approve or reject them with comments in Teams,
So that the workflow can progress to fulfillment or completion.

**Acceptance Criteria:**

**Given** I am logged in and belong to the "Approvers" group
**When** I open the details page for a request in `Submitted` state and click "Approve"
**Then** the backend writes an entry to `RequestApprovals`, logs my comments, updates the request status to `Approved`, and updates the dashboard counts.

---

## Epic 2: Document Management

### Epic Goal:
Support uploading supporting documents (PDF/images) during request creation and proxying file access securely.

### Story 2.1: Attachment Form & Client-Side File Validation
As an employee,
I want to attach a supporting document on the creation form with format and size restrictions,
So that I do not accidentally upload invalid or excessively large files.

**Acceptance Criteria:**

**Given** the Create Request form is active
**When** I select a file that is not a PDF, Word doc, or JPEG/PNG image, or is larger than 10MB
**Then** the UI displays a clear validation warning and blocks form submission.

### Story 2.2: Secure Folder Upload & Isolated Subfolders
As an employee,
I want my attached file to be stored in an isolated folder named after my Request Number,
So that my documents do not collide with other requests' files.

**Acceptance Criteria:**

**Given** I submit a request with a valid file attachment
**When** the backend creates the request and generates `REQ-00001`
**Then** the Graph service creates folder `RequestDocuments/REQ-00001/`
**And** uploads the attached file into this folder.

### Story 2.3: Proxy File Download & Access Enforcement
As an authorized manager,
I want to download and review request attachments securely via an API proxy rather than direct links,
So that files are protected from unauthorized direct access.

**Acceptance Criteria:**

**Given** I click the download link for a request attachment
**When** the request is sent to `GET /api/requests/{id}/documents/{docId}`
**Then** the backend validates that my Entra ID token is authorized to view this request
**And** streams the file binary back from SharePoint.

---

## Epic 3: Outlook Extensibility

### Epic Goal:
Integrate request management directly inside Outlook via a unified manifest and task pane add-in.

### Story 3.1: Outlook Add-In Manifest & Pane UI
As an employee,
I want to launch the Request Manager Add-in pane while reading an email in Outlook,
So that I can interact with request management without leaving my mailbox.

**Acceptance Criteria:**

**Given** I am viewing an email in Outlook
**When** I click the "Request Manager" add-in button in the email command bar
**Then** a Fluent UI task pane opens on the right side of the screen.

### Story 3.2: Create Request from Email Context
As an employee,
I want the add-in to automatically pre-fill request fields using context from the current email,
So that I can create a request with minimal typing.

**Acceptance Criteria:**

**Given** the add-in task pane is open next to an email
**When** I click "Create Request from Email"
**Then** the add-in retrieves the subject, sender email, and body snippet using `Office.context.mailbox`
**And** sends them to `POST /api/outlook/create-request`
**And** displays the confirmation `REQ-XXXXX` inside the add-in pane.
