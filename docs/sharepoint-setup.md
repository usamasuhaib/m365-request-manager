# SharePoint Provisioning Schema — Microsoft 365 Request Manager

This document provides schema specifications and provisioning instructions for the SharePoint Lists and Document Libraries that store our application's data.

---

## 1. Creating the SharePoint Site

1. Open your Microsoft 365 Tenant Admin portal and go to **SharePoint Admin Center**.
2. Click **Create** -> **Team Site** or **Communication Site**.
3. Set the Site Name to: `M365 Request Manager`.
4. Copy the Site URL (e.g. `https://yourtenant.sharepoint.com/sites/M365RequestManager`).
5. Select owner permissions and click **Finish**.

---

## 2. List Schemas

Create the following five SharePoint Lists under your site settings. Note the exact column names and types:

### A. List: `Requests`
*Core requests tracker.*

| Column Display Name | Field Name (Internal) | Field Type | Details / Choices / Lookups |
| :--- | :--- | :--- | :--- |
| **RequestNumber** | `RequestNumber` | Single line of text | e.g. `REQ-00001` (indexed, unique) |
| **Title** | `Title` | Single line of text | Title of the request |
| **Description** | `Description` | Multiple lines of text | Detailed explanations |
| **Category** | `Category` | Lookup | Lookup to `RequestCategories` list (Name column) |
| **Priority** | `Priority` | Choice | `Low`, `Medium`, `High` (Default: `Medium`) |
| **Status** | `Status` | Choice | `Draft`, `Submitted`, `Pending Approval`, `Approved`, `Rejected`, `Completed` |
| **SubmittedBy** | `SubmittedBy` | Single line of text | User's display name |
| **SubmittedByEmail** | `SubmittedByEmail`| Single line of text | User's email |
| **SubmittedDate** | `SubmittedDate` | Date and Time | Timestamp |
| **AssignedTo** | `AssignedTo` | Single line of text | Email of assigned approver |
| **ApprovedBy** | `ApprovedBy` | Single line of text | Approver display name |
| **ApprovedDate** | `ApprovedDate` | Date and Time | Timestamp |
| **RejectedBy** | `RejectedBy` | Single line of text | Rejecter display name |
| **RejectedDate** | `RejectedDate` | Date and Time | Timestamp |

### B. List: `RequestCategories`
*Classification lookup.*

| Column Display Name | Field Name (Internal) | Field Type | Details / Seed Values |
| :--- | :--- | :--- | :--- |
| **Title** (Default) | `Title` | Single line of text | Seed values: `Hardware`, `Software`, `Expense`, `Access` |
| **Description** | `Description` | Single line of text | Category explanation |
| **IsActive** | `IsActive` | Yes/No (Boolean) | Default: `Yes` |

### C. List: `RequestComments`
*Audit trail comments.*

| Column Display Name | Field Name (Internal) | Field Type | Details / Lookups |
| :--- | :--- | :--- | :--- |
| **RequestId** | `RequestId` | Number (Integer) | References `ID` from `Requests` list |
| **Comment** | `Comment` | Multiple lines of text | Plain text comment |
| **CommentedBy** | `CommentedBy` | Single line of text | Author name |
| **CommentedDate** | `CommentedDate` | Date and Time | Timestamp |

### D. List: `RequestApprovals`
*Formal workflow records.*

| Column Display Name | Field Name (Internal) | Field Type | Details / Choices |
| :--- | :--- | :--- | :--- |
| **RequestId** | `RequestId` | Number (Integer) | References `ID` from `Requests` list |
| **Approver** | `Approver` | Single line of text | Approver display name |
| **Status** | `Status` | Choice | `Approved`, `Rejected` |
| **Comments** | `Comments` | Multiple lines of text | Approver notes |
| **ActionDate** | `ActionDate` | Date and Time | Timestamp |

### E. List: `AppSettings`
*Application config.*

| Column Display Name | Field Name (Internal) | Field Type | Seed Value Examples |
| :--- | :--- | :--- | :--- |
| **Title** (Key) | `Title` | Single line of text | `AllowedFileExtensions`, `MaxFileSizeMB` |
| **Value** | `Value` | Single line of text | `.pdf,.docx,.png,.jpg`, `10` |

---

## 3. Document Library: `RequestDocuments`

1. On the Site Home, select **New** -> **Document Library**.
2. Set the Name to: `RequestDocuments`.
3. The folder hierarchy is generated dynamically by the C# backend using the request numbering rule:
   ```text
   RequestDocuments/
     REQ-00001/
       quote.pdf
       receipt.png
     REQ-00002/
       license_info.docx
   ```

---

## 4. Provisioning Automation (CLI for Microsoft 365)

If you prefer to automate list creation rather than manually clicking through SharePoint UI, run these CLI commands (make sure you install CLI for Microsoft 365 first via `npm i -g @pnp/cli-microsoft365`):

```bash
# Log in to your tenant
m365 login

# Create RequestCategories List
m365 spo list add --title "RequestCategories" --baseTemplate GenericList --webUrl https://yourtenant.sharepoint.com/sites/M365RequestManager

# Add columns to RequestCategories
m365 spo field add --webUrl https://yourtenant.sharepoint.com/sites/M365RequestManager --listTitle "RequestCategories" --xml "<Field Type='Text' Name='Description' DisplayName='Description' />"
m365 spo field add --webUrl https://yourtenant.sharepoint.com/sites/M365RequestManager --listTitle "RequestCategories" --xml "<Field Type='Boolean' Name='IsActive' DisplayName='IsActive'><Default>1</Default></Field>"
```
