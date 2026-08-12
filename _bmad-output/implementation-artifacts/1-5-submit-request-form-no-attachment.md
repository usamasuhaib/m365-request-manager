---
id: 1-5
title: Submit Request Form (No Attachment)
epic: 1
status: done
---

# Story 1.5: Submit Request Form (No Attachment)

As an employee,
I want to fill out and submit a request form with a Title, Description, Category, and Priority,
So that I can register a new request.

## Acceptance Criteria

**Given** I am on the "Create Request" screen in Teams
**When** I fill in the form fields (Title, Description, Category, Priority) and click "Submit"
**Then** the client sends a `POST /api/requests` containing the `Client-Request-Id` UUID
**And** the backend writes a new item to the SharePoint `Requests` list and returns `REQ-XXXXX`
**And** a duplicate POST with the same `Client-Request-Id` within 5 minutes returns the same request without duplicating it.
