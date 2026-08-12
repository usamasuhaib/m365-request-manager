---
id: 1-6
title: Review and Action Approvals
epic: 1
status: done
---

# Story 1.6: Review and Action Approvals

As an authorized manager,
I want to view pending requests and approve or reject them with comments in Teams,
So that the workflow can progress to fulfillment or completion.

## Acceptance Criteria

**Given** I am logged in and belong to the "Approvers" group
**When** I open the details page for a request in `Submitted` state and click "Approve"
**Then** the backend writes an entry to `RequestApprovals`, logs my comments, updates the request status to `Approved`, and updates the dashboard counts.
