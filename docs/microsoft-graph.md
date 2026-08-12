# Microsoft Graph API Integration — Microsoft 365 Request Manager

This document outlines the Microsoft Graph API service contracts, HTTP endpoints, scopes, and integration interfaces implemented in the backend application.

---

## 1. Graph API Scopes & Permissions

The application uses **Delegated Permissions** to interact with Graph, ensuring that actions run under the user's context and permissions are checked by the platform.

| Permission Scope | Type | Purpose |
| :--- | :--- | :--- |
| `User.Read` | Delegated | Resolves display name, email, and department of the logged-in user. |
| `Sites.ReadWrite.All` | Delegated | Accesses the SharePoint site, creates list items, updates status, and uploads documents. |

---

## 2. Graph API Endpoint Catalog

The C# infrastructure wrapper calls the following REST endpoints:

### User Profiles
- **Fetch Me:** `GET https://graph.microsoft.com/v1.0/me`
  * *Purpose:* Identifies the logged-in user's details for auditing and profile badges.

### SharePoint Site & Metadata
- **Get Site ID:** `GET https://graph.microsoft.com/v1.0/sites/{hostname}:/sites/{site-path}`
  * *Purpose:* Translates a site name into a unique Microsoft Graph GUID.
- **Get Lists:** `GET https://graph.microsoft.com/v1.0/sites/{site-id}/lists`
  * *Purpose:* Identifies the lists inside the site.

### SharePoint List Operations
- **Read Requests:** `GET https://graph.microsoft.com/v1.0/sites/{site-id}/lists/{list-id}/items?expand=fields`
  * *Purpose:* Fetches request records, filtering by submitter email or status.
- **Write Request Item:** `POST https://graph.microsoft.com/v1.0/sites/{site-id}/lists/{list-id}/items`
  * *Body:* JSON representing field values (Title, Priority, Category lookup).
- **Update Request Status:** `PATCH https://graph.microsoft.com/v1.0/sites/{site-id}/lists/{list-id}/items/{item-id}/fields`
  * *Purpose:* Advances request state (Draft → Submitted → Approved).

### File Upload & Storage (Document Libraries)
- **Get Drive ID:** `GET https://graph.microsoft.com/v1.0/sites/{site-id}/drives`
  * *Purpose:* Resolves the unique Drive GUID for the `RequestDocuments` library.
- **Create Isolation Folder:** `POST https://graph.microsoft.com/v1.0/drives/{drive-id}/items/{parent-id}/children`
  * *Purpose:* Creates folder `REQ-XXXXX/` for request isolation.
- **Initialize Upload Session (Files > 4MB):** `POST https://graph.microsoft.com/v1.0/drives/{drive-id}/items/{parent-id}:/{filename}:/createUploadSession`
  * *Purpose:* Allows safe chunked file upload to prevent timeouts.

---

## 3. C# Graph Services Interfaces

The backend code isolates the Graph SDK behind clean application interfaces:

```csharp
namespace RequestManager.Functions.Graph
{
    public interface ISharePointService
    {
        Task<RequestDto> GetRequestByIdAsync(string id);
        Task<IEnumerable<RequestDto>> GetRequestsAsync(string userEmail, string statusFilter);
        Task<RequestDto> CreateRequestAsync(CreateRequestDto dto, string userEmail, string userName);
        Task<RequestDto> UpdateRequestStatusAsync(string id, string status, string comment, string approverEmail);
    }

    public interface IDocumentService
    {
        Task<DocumentDto> UploadDocumentAsync(string requestId, string fileName, Stream fileStream);
        Task<Stream> DownloadDocumentAsync(string requestId, string documentId);
        Task<IEnumerable<DocumentDto>> GetDocumentsAsync(string requestId);
    }
}
```

---

## 4. Throttling and Failure Handling

Microsoft Graph enforces request rate limits (HTTP `429 Too Many Requests`). 
The C# Graph Client handles this using standard handlers configured during Dependency Injection (`Program.cs`):

```csharp
// Configure Microsoft Graph Client with retry policies
var graphClient = new GraphServiceClient(oboCredential);

// The v5 SDK includes a default RetryHandler that automatically inspects
// the "Retry-After" response header and delays retries using exponential backoff.
```
If a Graph call fails, the service wrapper catches `ODataError` and maps it to a standard internal error schema, returning clean HTTP status codes (`500 Internal Server Error` or `429 Throttled`) instead of exposing raw stack traces.
