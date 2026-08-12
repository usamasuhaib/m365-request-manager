---
id: 1-2
title: Entra ID App Registrations & JWT Auth Middleware
epic: 1
status: done
---

# Story 1.2: Entra ID App Registrations & JWT Auth Middleware

As a developer,
I want to secure the backend API endpoints using Entra ID authentication and validate tokens,
So that only verified tenant users can interact with application data.

## Acceptance Criteria

**Given** an Entra ID application registration is configured
**When** I request `GET /api/me` with a valid Bearer token in the Authorization header
**Then** the API returns `200 OK` containing the user's name and email
**When** I request the API with an expired, missing, or malformed token
**Then** the API returns `401 Unauthorized`.
