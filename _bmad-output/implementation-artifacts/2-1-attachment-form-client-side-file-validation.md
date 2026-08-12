---
id: 2-1
title: Attachment Form & Client-Side File Validation
epic: 2
status: done
---

# Story 2.1: Attachment Form & Client-Side File Validation

As an employee,
I want to attach a supporting document on the creation form with format and size restrictions,
So that I do not accidentally upload invalid or excessively large files.

## Acceptance Criteria

**Given** the Create Request form is active
**When** I select a file that is not a PDF, Word doc, or JPEG/PNG image, or is larger than 10MB
**Then** the UI displays a clear validation warning and blocks form submission.
