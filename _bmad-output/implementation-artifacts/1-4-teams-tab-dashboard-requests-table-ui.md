---
id: 1-4
title: Teams Tab Dashboard & Requests Table UI
epic: 1
status: done
---

# Story 1.4: Teams Tab Dashboard & Requests Table UI

As an employee,
I want to view a dashboard in Teams showing my request metrics and recent submissions,
So that I can monitor my open and completed tickets in real-time.

## Acceptance Criteria

**Given** I am logged into Microsoft Teams
**When** I open the Request Manager personal tab
**Then** the app performs silent SSO login via the `@microsoft/teams-js` SDK
**And** displays cards with counts for Total, Pending, Approved, and Rejected requests
**And** lists the 5 most recent requests in a table.
