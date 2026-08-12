import React, { useEffect, useState } from "react";
import { 
  Text, 
  Button, 
  Field, 
  Dropdown, 
  Option,
  Spinner,
  makeStyles,
  tokens
} from "@fluentui/react-components";
import { MailRegular, SendRegular, CheckmarkCircleRegular } from "@fluentui/react-icons";
import { getAuthToken } from "../services/authService";

declare const Office: any;

const useStyles = makeStyles({
  container: {
    padding: "16px",
    display: "flex",
    flexDirection: "column",
    gap: "16px",
    backgroundColor: tokens.colorNeutralBackground1,
    minHeight: "100vh"
  },
  card: {
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    padding: "16px",
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground2,
    display: "flex",
    flexDirection: "column",
    gap: "8px"
  },
  actions: {
    display: "flex",
    flexDirection: "column",
    gap: "10px",
    marginTop: "16px"
  },
  successMessage: {
    color: "#107c41",
    fontWeight: "semibold",
    display: "flex",
    alignItems: "center",
    gap: "8px",
    padding: "12px",
    backgroundColor: "#d5e8d4",
    borderRadius: tokens.borderRadiusSmall
  }
});

export default function OutlookPane() {
  const styles = useStyles();
  
  const [subject, setSubject] = useState("");
  const [sender, setSender] = useState("");
  const [bodySnippet, setBodySnippet] = useState("");
  
  const [category, setCategory] = useState("Software");
  const [priority, setPriority] = useState("Medium");
  
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [successNumber, setSuccessNumber] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const categories = ["Hardware", "Software", "Expense"];
  const priorities = ["Low", "Medium", "High"];

  useEffect(() => {
    // Bootstrap Office JS
    if (typeof Office !== "undefined") {
      Office.onReady((info: any) => {
        if (info.host === Office.HostType.Outlook) {
          const item = Office.context.mailbox.item;
          if (item) {
            setSubject(item.subject || "No Subject");
            setSender(item.from?.emailAddress || "unknown@sender.com");
            
            // Extract body text async
            item.body.getAsync(Office.CoercionType.Text, (result: any) => {
              if (result.status === Office.AsyncResultStatus.Succeeded) {
                const text = result.value || "";
                setBodySnippet(text.substring(0, 400));
              }
              setLoading(false);
            });
          } else {
            setLoading(false);
          }
        } else {
          loadMockContext();
        }
      });
    } else {
      loadMockContext();
    }
  }, []);

  const loadMockContext = () => {
    // Mock context for local browser testing outside Outlook client
    setSubject("Software Invoice INV-9923");
    setSender("billing@softwarevendor.com");
    setBodySnippet("Hi Priya, please find attached our invoice for the renewal of Visual Studio Enterprise licenses ($450). Let us know if you have any questions.");
    setLoading(false);
  };

  const handleCreateRequest = async () => {
    setSubmitting(true);
    setError(null);
    setSuccessNumber(null);

    const apiBase = import.meta.env.VITE_API_BASE_URL || "http://localhost:7071";
    
    try {
      const token = await getAuthToken();
      const res = await fetch(`${apiBase}/api/outlook/create-request`, {
        method: "POST",
        headers: {
          "Authorization": `Bearer ${token}`,
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          subject,
          sender,
          bodySnippet,
          category,
          priority
        })
      });

      if (!res.ok) {
        const err = await res.json();
        throw new Error(err.message || "Failed to create request from email.");
      }

      const payload = await res.json();
      setSuccessNumber(payload.data.requestNumber);
    } catch (err: any) {
      setError(err.message || "Error submitting request.");
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className={styles.container} style={{ justifyContent: "center", alignItems: "center" }}>
        <Spinner label="Reading email context..." />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
        <MailRegular style={{ fontSize: "24px", color: tokens.colorBrandForeground1 }} />
        <Text as="h2" size={400} weight="semibold">M365 Request Ingest</Text>
      </div>

      <Text size={200} style={{ color: tokens.colorNeutralForeground4 }}>
        Extract and create request tickets directly from your email inbox.
      </Text>

      {successNumber && (
        <div className={styles.successMessage}>
          <CheckmarkCircleRegular style={{ fontSize: "20px" }} />
          <div>
            <Text size={200}><strong>Success!</strong> Request created: </Text>
            <br />
            <Text size={300} weight="bold">{successNumber}</Text>
          </div>
        </div>
      )}

      {error && (
        <Text style={{ color: "#a80000", fontSize: "14px" }}>{error}</Text>
      )}

      <div className={styles.card}>
        <Text size={100} style={{ color: tokens.colorNeutralForeground4 }}>Sender</Text>
        <Text size={200} weight="semibold">{sender}</Text>
      </div>

      <div className={styles.card}>
        <Text size={100} style={{ color: tokens.colorNeutralForeground4 }}>Subject</Text>
        <Text size={200} weight="semibold">{subject}</Text>
      </div>

      <div className={styles.card}>
        <Text size={100} style={{ color: tokens.colorNeutralForeground4 }}>Body Preview</Text>
        <Text size={200} style={{ fontStyle: "italic" }}>"{bodySnippet}"</Text>
      </div>

      <Field label="Category" required>
        <Dropdown
          value={category}
          onOptionSelect={(_, data) => setCategory(data.optionValue || "Software")}
          disabled={submitting}
        >
          {categories.map((cat) => (
            <Option key={cat} value={cat}>{cat}</Option>
          ))}
        </Dropdown>
      </Field>

      <Field label="Priority" required>
        <Dropdown
          value={priority}
          onOptionSelect={(_, data) => setPriority(data.optionValue || "Medium")}
          disabled={submitting}
        >
          {priorities.map((pri) => (
            <Option key={pri} value={pri}>{pri}</Option>
          ))}
        </Dropdown>
      </Field>

      <div className={styles.actions}>
        <Button 
          appearance="primary" 
          icon={<SendRegular />} 
          onClick={handleCreateRequest}
          disabled={submitting || !!successNumber}
        >
          {submitting ? "Processing..." : "Create Request from Email"}
        </Button>
      </div>
    </div>
  );
}
