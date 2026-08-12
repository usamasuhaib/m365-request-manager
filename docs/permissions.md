# Permissions Matrix & Role Mappings — Microsoft 365 Request Manager

This document defines the security boundaries, delegated Graph API permission scopes, and role authorization mappings for the application.

---

## 1. Microsoft Graph Delegated Permissions

The application uses **delegated permissions** to act on behalf of the logged-in user. This ensures that the user's individual SharePoint permissions are enforced and audits record their identity.

| API Permission | Scope Type | Admin Consent Required | Purpose |
| :--- | :--- | :---: | :--- |
| **`User.Read`** | Delegated | No | Accesses the active user's profile info (Name, Email, Job Title). |
| **`Sites.ReadWrite.All`** | Delegated | **Yes** | Allows the Azure Function to create, read, update, and delete list items and folders in the target SharePoint site on behalf of the user. |

### Why Admin Consent is Mandatory
Because `Sites.ReadWrite.All` permits reading and writing all SharePoint sites in the tenant, Entra ID requires a Global Administrator to perform a one-time consent approval (details in **[entra-id.md](file:///c:/Solvefy%20Projects/M365%20DemoApp/docs/entra-id.md)**) before the app is sideloaded.

---

## 2. Security Roles: Submitters & Approvers

We define two distinct security tiers using **Microsoft Entra ID Security Groups**:

```
┌────────────────────────────────────────────────────────┐
│             Microsoft Entra ID Tenant                  │
├──────────────────────────┬─────────────────────────────┤
│  M365 Request Submitters │   M365 Request Approvers    │
│  (All Employee Accounts)  │   (Managers & Finance)      │
└────────────┬─────────────┘              │              │
             │                            ▼              │
             │                 Allowed to run POST/PUT   │
             ▼                 to /api/requests/{id}/    │
     Allowed to create         approve or /reject        │
     and view own requests                               │
```

1. **Submitters Group (`M365 Request Submitters`):** Contains all employee accounts who can log requests and upload documents.
2. **Approvers Group (`M365 Request Approvers`):** Contains managers authorized to change the request status to `Approved` or `Rejected` and comment on requests.

---

## 3. Backend Role Enforcement Logic

The backend Azure Function enforces these roles using one of two patterns:

### Pattern A: Directory Group Membership Check (Recommended)
During request actions (approve/reject), the backend function inspects the user's transit group memberships.
1. The backend API receives the OBO Graph client.
2. Queries the Graph endpoint:
   `GET https://graph.microsoft.com/v1.0/me/transitiveMemberOf`
3. Checks if the `id` of any returned group matches the configured `M365 Request Approvers` group GUID (configured in the function's Application Settings).

```csharp
public async Task<bool> IsUserInApproversGroupAsync(GraphServiceClient graphClient, string approverGroupGuid)
{
    var memberOf = await graphClient.Me.TransitiveMemberOf.GetAsync();
    
    // Check if the target Group GUID is in the collection
    return memberOf.Value.Any(directoryObject => directoryObject.Id == approverGroupGuid);
}
```

### Pattern B: SharePoint Role Lookup List (Simple Demo Alternative)
If creating Entra ID Security Groups is restricted due to licensing:
1. Create a SharePoint List named `ApproversList` containing authorized emails.
2. The Azure Function queries `ApproversList` using Graph and verifies if the submitter's email (`userEmail`) is registered.
