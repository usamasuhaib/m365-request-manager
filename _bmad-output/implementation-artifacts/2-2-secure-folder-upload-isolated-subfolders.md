---
id: 2-2
title: Secure Folder Upload & Isolated Subfolders
epic: 2
status: done
---

# Story 2.2: Secure Folder Upload & Isolated Subfolders

As an employee,
I want my attached file to be stored in an isolated folder named after my Request Number,
So that my documents do not collide with other requests' files.

## Acceptance Criteria

**Given** I submit a request with a valid file attachment
**When** the backend creates the request and generates `REQ-00001`
**Then** the Graph service creates folder `RequestDocuments/REQ-00001/`
**And** uploads the attached file into this folder.
