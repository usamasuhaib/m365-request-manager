---
id: 3-2
title: Create Request from Email Context
epic: 3
status: done
---

# Story 3.2: Create Request from Email Context

As an employee,
I want the add-in to automatically pre-fill request fields using context from the current email,
So that I can create a request with minimal typing.

## Acceptance Criteria

**Given** the add-in task pane is open next to an email
**When** I click "Create Request from Email"
**Then** the add-in retrieves the subject, sender email, and body snippet using `Office.context.mailbox`
**And** sends them to `POST /api/outlook/create-request`
**And** displays the confirmation `REQ-XXXXX` inside the add-in pane.
