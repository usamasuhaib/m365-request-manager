---
id: 2-3
title: Proxy File Download & Access Enforcement
epic: 2
status: done
---

# Story 2.3: Proxy File Download & Access Enforcement

As an authorized manager,
I want to download and review request attachments securely via an API proxy rather than direct links,
So that files are protected from unauthorized direct access.

## Acceptance Criteria

**Given** I click the download link for a request attachment
**When** the request is sent to `GET /api/requests/{id}/documents/{docId}`
**Then** the backend validates that my Entra ID token is authorized to view this request
**And** streams the file binary back from SharePoint.
