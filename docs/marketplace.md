# Microsoft Marketplace & AppSource Readiness — Microsoft 365 Request Manager

This document outlines the steps and validation checklist required to submit the application to the **Microsoft Commercial Marketplace** (AppSource) and the **Teams App Store**.

---

## 1. Partner Center Portal Setup

To publish commercial Microsoft 365 apps, you must register a publisher profile:
1. Open the [Microsoft Partner Center Portal](https://partner.microsoft.com/).
2. Log in using your organizational Global Admin account.
3. Complete the account verification and enroll in the **Commercial Marketplace** program.
4. Set up your publisher profile and billing information.

---

## 2. Listing Assets Checklist

When creating a new offer in Partner Center, you must prepare the following listing assets:

### Copywriting
* **App Name (Short):** Max 30 characters.
* **App Name (Full):** Max 100 characters. Must not contain the word "Teams" or "Office" unless it is "for Teams."
* **Short Description:** Max 80 characters.
* **Long Description:** Max 4000 characters. Must detail target users, core features, value proposition, and hosting dependencies.

### Brand Assets & Icons
* **`color.png`:** Full-color square icon, **96x96 pixels** in PNG format.
* **`outline.png`:** White/transparent outline icon, **32x32 pixels** in PNG format.
* **Screenshots:** At least 3, and up to 5 screenshots showing key pages (Dashboard, Creation Form, Approvals tab). Recommended size: **1366x768** or **1920x1080** pixels. No device frames allowed (laptop borders, taskbars).

### Compliance & Legal Links
* **Privacy Policy URL:** A live webpage hosting your app's privacy policy.
* **Terms of Use URL:** A live webpage detailing terms.
* **Support URL:** A contact page where users can file support tickets.

---

## 3. Microsoft Store Certification Hurdles

Microsoft reviews every application manually before publishing. To ensure your submission is not rejected, verify these three critical criteria:

### A. Manifest Validation
Verify that your manifest package is schema-compliant. Run the Teams App validation CLI tool:
```bash
# Validate your manifest package against Microsoft schemas
npx @microsoft/teamsappmanifest validate .\manifest\manifest.json
```
If errors are returned (e.g. missing fields, invalid domain declarations), resolve them before building the zip archive.

### B. Test Account Credentials
You must supply a **working test environment** and credentials for the Microsoft review team:
* Provide a test user account (Submitter) with pre-populated dummy requests.
* Provide an administrator/approver account to test the approval action.
* Supply a step-by-step PDF manual detailing how the reviewer should log in, create a request, check SharePoint lists, and approve the request.

### C. SSO Login and Infinite Loop Prevention
* The app must implement silent SSO login without infinite loops. If SSO fails, you must offer an explicit fallback login button instead of immediately redirecting the page.
* Reviewers test apps on multiple devices and browsers; any browser pop-up block must be handled gracefully by triggering login on a user click action.
