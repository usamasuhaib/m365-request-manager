---
id: 1-3
title: SharePoint Provisioning & Graph Service Integration
epic: 1
status: done
---

# Story 1.3: SharePoint Provisioning & Graph Service Integration

As an administrator,
I want to run a setup configuration that creates the required `Requests` and `RequestCategories` Lists in SharePoint,
So that the application has a structured database layer ready for data operations.

## Acceptance Criteria

**Given** the Microsoft Graph client is authenticated in the backend
**When** the initialization routine runs
**Then** it creates lists `Requests` and `RequestCategories` with the specified schemas
**And** populates `RequestCategories` with seed values (Hardware, Software, Expense).
