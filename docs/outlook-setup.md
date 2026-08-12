# Outlook Add-In Setup Guide — Microsoft 365 Request Manager

This document provides setup instructions for embedding the **Request & Document Manager** directly inside Microsoft Outlook as a mail task pane add-in.

---

## 1. Outlook Add-In Extensibility Architecture

Outlook Add-ins are web applications that load inside an iframe task pane next to an active email. The add-in interacts with Outlook using the **Office JavaScript Library (Office.js)**.

The Outlook Add-in is configured either through the **Unified Teams JSON Manifest (v1.16+)** or a traditional **Outlook XML Manifest**. For this demo, we document the traditional XML manifest setup as it is widely supported across all Outlook clients (Desktop, Web, Mac).

---

## 2. Outlook XML Manifest (`manifest-outlook.xml`)

Save this XML configuration file under `manifest/manifest-outlook.xml` in your workspace. Update the `{YOUR_FRONTEND_URL}` references to point to your React dev server or production URL.

```xml
<?xml version="1.0" encoding="UTF-8"?>
<OfficeApp 
  xmlns="http://schemas.microsoft.com/office/appshistory/1.0" 
  xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" 
  xsi:type="MailApp">
  <Id>d2c369e8-468e-4a6f-bd1a-96e06b3a24d5</Id>
  <Version>1.0.0.0</Version>
  <ProviderName>Solvefy</ProviderName>
  <DefaultLocale>en-US</DefaultLocale>
  <DisplayName DefaultValue="M365 Request Manager" />
  <Description DefaultValue="Create SharePoint requests directly from Outlook email context." />
  <IconUrl DefaultValue="{YOUR_FRONTEND_URL}/color.png"/>
  <HighResolutionIconUrl DefaultValue="{YOUR_FRONTEND_URL}/color.png"/>
  
  <Hosts>
    <Host Name="Mailbox" />
  </Hosts>
  
  <Requirements>
    <Sets DefaultMinVersion="1.1">
      <Set Name="Mailbox" />
    </Sets>
  </Requirements>
  
  <FormSettings>
    <Form xsi:type="ItemRead">
      <DesktopSettings>
        <SourceLocation DefaultValue="{YOUR_FRONTEND_URL}/index.html#/outlook-pane"/>
        <RequestedHeight>250</RequestedHeight>
      </DesktopSettings>
    </Form>
  </FormSettings>
  
  <Permissions>ReadWriteItem</Permissions>
  <Rule xsi:type="RuleCollection" Mode="Or">
    <Rule xsi:type="ItemIs" ItemType="Message" FormType="Read" />
  </Rule>
  <DisableEntityHighlighting>false</DisableEntityHighlighting>
</OfficeApp>
```

---

## 3. Reading Email Context via Office.js

Inside the React application, the Outlook task pane component initialized on route `/outlook-pane` extracts email parameters using the Office mailbox APIs:

```typescript
import { useEffect, useState } from "react";

export function OutlookPane() {
  const [subject, setSubject] = useState("");
  const [sender, setSender] = useState("");
  const [bodySnippet, setBodySnippet] = useState("");

  useEffect(() => {
    // Ensure Office.js is initialized
    Office.onReady((info) => {
      if (info.host === Office.HostType.Outlook) {
        const item = Office.context.mailbox.item;
        
        // Read metadata
        setSubject(item.subject);
        setSender(item.from.emailAddress);

        // Read email body snippet (as HTML or Text)
        item.body.getAsync(Office.CoercionType.Text, (result) => {
          if (result.status === Office.AsyncResultStatus.Succeeded) {
            setBodySnippet(result.value.substring(0, 500)); // Grab first 500 chars
          }
        });
      }
    });
  }, []);

  const handleCreateRequest = async () => {
    // Send subject, sender, and snippet to /api/outlook/create-request
  };

  return (
    <div style={{ padding: 16 }}>
      <h2>Create Request from Email</h2>
      <p><strong>Subject:</strong> {subject}</p>
      <p><strong>From:</strong> {sender}</p>
      <button onClick={handleCreateRequest}>Create Request</button>
    </div>
  );
}
```

---

## 4. Sideloading the Add-in in Outlook

### Outlook Web App (OWA)
1. Go to [Outlook Web App](https://outlook.office.com/).
2. Open any email.
3. Click the **More actions** (...) menu inside the email header and select **Get Add-ins** (or **Apps** -> **Add apps**).
4. Click **Manage my add-ins** -> **Add a custom add-in** -> **Add from file...**
5. Upload your `manifest-outlook.xml` file.
6. Click **Install**. The add-in is now visible when reading emails.
