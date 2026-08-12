---
id: 1-1
title: Local Workspace and C# API Health Checks
epic: 1
status: done
---

# Story 1.1: Local Workspace and C# API Health Checks

As a developer,
I want to set up the local C# .NET 8 Azure Functions project structure and test a health endpoint,
So that I can verify my local development runtime before writing business logic.

## Acceptance Criteria

**Given** the workspace is initialized
**When** I run `func start` on the backend project
**Then** the local runtime starts successfully on port 7071
**And** calling `GET http://localhost:7071/api/health` returns `200 OK` with:
```json
{
  "status": "healthy"
}
```
