# M365 Request Manager — Step-by-Step Demonstration Script

This document provides a sequential, step-by-step script for conducting a live technical demonstration of the **Microsoft 365 Request & Document Manager** proof of concept.

---

## 🎬 Phase 1: Environment Initialization & Storage Setup

| Step | Action | Expected Terminal/Browser Output | Invariant Verified |
| :--- | :--- | :--- | :---: |
| **1** | Open a browser and navigate to: `GET http://localhost:7071/api/setup` | `{ "success": true, "message": "SharePoint schema storage lists provisioned successfully." }` | **AD-3** |
| **2** | Open a browser and navigate to the health status check: `GET http://localhost:7071/api/health` | `{ "status": "healthy" }` | Setup check |

---

## 💻 Phase 2: Teams Dashboard & Silent SSO Login (Submitter Persona: Priya)

| Step | Action | UI Result / Screen Changes | Invariant Verified |
| :--- | :--- | :--- | :---: |
| **1** | Open browser to `http://localhost:3000/`. | Page loads, displaying "Loading Request Manager...". Silent SSO runs. | **AD-2** |
| **2** | Submitter profile details are loaded. | The dashboard opens showing: Submitter name **Priya Patel** in the header. | **AD-2** |
| **3** | Review the metric counters. | Total: **2**, Pending: **1**, Approved: **1**, Rejected: **0**. | **FR-1** |
| **4** | Review the "Recent Requests" table. | Displays request `REQ-00001` (Submitted) and `REQ-00002` (Approved). | **FR-2** |

---

## 📝 Phase 3: Creating Requests & Upload Protections

| Step | Action | UI Result / Screen Changes | Invariant Verified |
| :--- | :--- | :--- | :---: |
| **1** | Click **Create Request** button in the dashboard. | Form opens with inputs for Title, Description, Category, Priority. | **FR-3** |
| **2** | Enter title: "Budget Request", Description: "", click Submit. | The field validator triggers: "Title and Description are required." | **FR-3** |
| **3** | Enter title: "Laptop Procurement", Description: "New joiner workstation request". | Form validation passes. | **FR-3** |
| **4** | Click the file selector and select a `.zip` or `.exe` file. | The client validator blocks upload: "Unsupported attachment format..." | **FR-4** |
| **5** | Select a `.pdf` file larger than 10MB. | The client validator blocks upload: "File exceeds the maximum allowed size..." | **FR-4** |
| **6** | Select a valid `.pdf` invoice (e.g. `invoice.pdf` of 2MB) and click **Submit Request**. | The form closes, and you are redirected to the Dashboard. | **FR-5** |
| **7** | Double-Click the submit button rapidly during submit. | The backend's `Client-Request-Id` cache detects duplicate. No duplicate list items created. | **AD-4** |

---

## 💬 Phase 4: Discussion Thread & Document Proxying

| Step | Action | UI Result / Screen Changes | Invariant Verified |
| :--- | :--- | :--- | :---: |
| **1** | Click on the new request (`REQ-00003`) in the recent table. | The Request Details page loads showing all request parameters. | **FR-5** |
| **2** | Scroll to **Attachments** section and click on `invoice.pdf`. | The browser downloads the file via `GET /api/requests/3/documents/{docId}`. | **AD-5** |
| **3** | Try to download the file directly using browser private window (no headers). | The proxy endpoint blocks access returning `401 Unauthorized`. | **AD-5** |
| **4** | Type "Vendor confirmed stock availability." in comments, click Post. | Comment is saved and instantly appended to the Discussion Thread. | Commenting |

---

## ⚖️ Phase 5: Manager Review & Workflow Decisions (Approver Persona: Winston)

| Step | Action | UI Result / Screen Changes | Invariant Verified |
| :--- | :--- | :--- | :---: |
| **1** | Log in or simulate Winston (Approver group member). | The Approval Decision panel is visible on `REQ-00003` details page. | **FR-5** |
| **2** | Type "Approved for procurement." in Decision Notes, click **Approve**. | The request status changes to `Approved`. The approved banner is displayed. | Workflow |
| **3** | Return to Dashboard. | Metrics counter updates (Approved: **2**, Pending: **1**). | **FR-1** |

---

## 📧 Phase 6: Outlook Mail Add-In (Submitter Persona: Priya)

| Step | Action | UI Result / Screen Changes | Invariant Verified |
| :--- | :--- | :--- | :---: |
| **1** | Navigate to `http://localhost:3000/#/outlook-pane`. | Simulates the Outlook task pane side drawer. | **FR-10** |
| **2** | Review the pre-extracted email context cards. | Pre-fills: Subject: **Software Invoice INV-9923**, Sender: **billing@softwarevendor.com**. | **FR-10** |
| **3** | Click **Create Request from Email** button. | Progress spinner runs, then shows success banner: "Request created: REQ-00004". | **FR-11** |
| **4** | Check the dashboard list. | Request `REQ-00004` appears in the list with Description header `[Ingested from Outlook]`. | **FR-2** |
