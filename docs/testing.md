# Testing & E2E Validation Plan — Microsoft 365 Request Manager

This document defines the unit, integration, and end-to-end (E2E) testing strategies for the application.

---

## 1. Unit Testing & Mocking Strategy

Because our application core is isolated from the presentation and infrastructure layers (per Layered Hexagonal architecture), we can unit test the business logic and state machine transitions without making actual calls to Microsoft Graph or SharePoint.

### Mocking Interfaces
We write unit tests in `.NET 8` using **xUnit** and **Moq** (or NSubstitute) to mock our core ports:

```csharp
public class RequestServiceTests
{
    private readonly Mock<ISharePointRepository> _repoMock;
    private readonly RequestService _service;

    public RequestServiceTests()
    {
        _repoMock = new Mock<ISharePointRepository>();
        _service = new RequestService(_repoMock.Object);
    }

    [Fact]
    public async Task SubmitRequest_OnlyAllowedIfStateIsDraft()
    {
        // Arrange
        var mockRequest = new Request { Id = 1, Status = "Approved" };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(mockRequest);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SubmitAsync(1));
    }
}
```

---

## 2. Integration Testing

Integration tests verify the communication between the Azure Functions and Microsoft Graph.

### Sandbox Environment
1. Use an isolated **M365 Developer Tenant** sandbox for integration runs.
2. Store integration credentials in a local configurations file (or pipeline environment variables).
3. The test runner initiates token exchange using an App Registration with Application permissions (configured strictly for testing) to clean up lists after each test run.

---

## 3. End-to-End (E2E) Playwright Testing

We utilize **Playwright** to test the React frontend. Since Teams SSO depends on the Teams host container, we use mocks for the `@microsoft/teams-js` SDK during local browser testing, simulating the silent token delivery:

```typescript
// mock-teams.js
window.microsoftTeams = {
  initialize: () => Promise.resolve(),
  app: {
    getContext: () => Promise.resolve({ user: { userPrincipalName: "priya@solvefy.onmicrosoft.com" } })
  },
  authentication: {
    getAuthToken: () => Promise.resolve("mock_jwt_token")
  }
};
```

---

## 4. Manual End-to-End Live Demo Checklist

This checklist must be executed to certify the application's correctness before delivery:

| Step | Action | Expected Result | Checked |
| :--- | :--- | :--- | :---: |
| **1** | Log into Teams Web Client as Priya. | Personal tab loads and performs silent SSO login. | [ ] |
| **2** | Open "Request Dashboard" tab. | Summary cards load showing 0 open requests. | [ ] |
| **3** | Click "Create Request." | The form loads. Title validation error shows if left empty. | [ ] |
| **4** | Upload file `quote.pdf` (5MB). | File validates successfully and displays inside form. | [ ] |
| **5** | Click "Submit." | Success popup displays: "Request REQ-00001 submitted." | [ ] |
| **6** | Open SharePoint admin site. | Item `REQ-00001` exists in `Requests` List with status `Submitted`. | [ ] |
| **7** | Check SharePoint Document Library. | Folder `RequestDocuments/REQ-00001/` contains `quote.pdf`. | [ ] |
| **8** | Log into Teams as Approver Winston. | Approvals tab shows badge "1 Pending Approval." | [ ] |
| **9** | Winston clicks `REQ-00001`, reviews details, types "Approved", clicks Approve. | Request status updates to `Approved` in list. Log written to `RequestApprovals`. | [ ] |
| **10**| Open Outlook Web Client as Priya. | Select email invoice, click "Request Manager" add-in. | [ ] |
| **11**| Click "Create Request from Email." | Add-in reads Subject/Body and generates `REQ-00002`. | [ ] |
