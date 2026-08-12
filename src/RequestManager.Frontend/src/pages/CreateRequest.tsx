import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { 
  Text, 
  Button, 
  Input, 
  Textarea,
  Dropdown,
  Option,
  Field,
  makeStyles,
  tokens
} from "@fluentui/react-components";
import { ArrowLeftRegular, SendRegular } from "@fluentui/react-icons";
import { createRequest } from "../services/apiService";

const useStyles = makeStyles({
  container: {
    padding: "24px",
    maxWidth: "600px",
    margin: "0 auto",
    display: "flex",
    flexDirection: "column",
    gap: "24px",
  },
  header: {
    display: "flex",
    alignItems: "center",
    gap: "12px"
  },
  formCard: {
    backgroundColor: tokens.colorNeutralBackground1,
    padding: "24px",
    borderRadius: tokens.borderRadiusMedium,
    boxShadow: tokens.shadow2,
    display: "flex",
    flexDirection: "column",
    gap: "16px"
  },
  actions: {
    display: "flex",
    justifyContent: "flex-end",
    gap: "12px",
    marginTop: "16px"
  },
  errorMessage: {
    color: "#a80000",
    fontSize: "14px"
  }
});

export default function CreateRequest() {
  const styles = useStyles();
  const navigate = useNavigate();

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [category, setCategory] = useState("Hardware");
  const [priority, setPriority] = useState("Medium");
  
  const [fileName, setFileName] = useState<string>("");
  const [fileContent, setFileContent] = useState<string>(""); // base64 string
  
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files || files.length === 0) {
      setFileName("");
      setFileContent("");
      return;
    }

    const file = files[0];
    const extension = file.name.substring(file.name.lastIndexOf(".")).toLowerCase();
    const allowed = [".pdf", ".docx", ".png", ".jpg", ".jpeg"];
    
    // Validate file formats
    if (!allowed.includes(extension)) {
      setError(`Unsupported attachment format: ${extension}. Only PDF, DOCX, and JPG/PNG images are allowed.`);
      e.target.value = "";
      setFileName("");
      setFileContent("");
      return;
    }

    // Validate size (< 10MB)
    if (file.size > 10 * 1024 * 1024) {
      setError("File exceeds the maximum allowed size of 10MB.");
      e.target.value = "";
      setFileName("");
      setFileContent("");
      return;
    }

    setError(null);
    setFileName(file.name);

    // Read file as base64 string
    const reader = new FileReader();
    reader.onload = () => {
      const result = reader.result as string;
      const base64Data = result.substring(result.indexOf(",") + 1); // remove dataUrl header
      setFileContent(base64Data);
    };
    reader.readAsDataURL(file);
  };

  const categories = ["Hardware", "Software", "Expense"];
  const priorities = ["Low", "Medium", "High"];

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !description.trim()) {
      setError("Title and Description are required fields.");
      return;
    }

    setSubmitting(true);
    setError(null);

    try {
      await createRequest(title, description, category, priority, fileName || undefined, fileContent || undefined);
      navigate("/");
    } catch (err: any) {
      setError(err.message || "Failed to submit request.");
      setSubmitting(false);
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Button 
          icon={<ArrowLeftRegular />} 
          appearance="subtle" 
          onClick={() => navigate("/")}
        />
        <Text as="h1" size={600} weight="semibold">Create New Request</Text>
      </div>

      <form onSubmit={handleSubmit} className={styles.formCard}>
        {error && <Text className={styles.errorMessage}>{error}</Text>}

        <Field label="Request Title" required>
          <Input 
            value={title} 
            onChange={(e) => setTitle(e.target.value)} 
            placeholder="e.g. Developer Laptop Upgrade"
            maxLength={100}
            disabled={submitting}
          />
        </Field>

        <Field label="Description" required>
          <Textarea 
            value={description} 
            onChange={(e) => setDescription(e.target.value)} 
            placeholder="Provide details about your request..."
            maxLength={1000}
            rows={5}
            disabled={submitting}
          />
        </Field>

        <Field label="Category" required>
          <Dropdown
            value={category}
            onOptionSelect={(_, data) => setCategory(data.optionValue || "Hardware")}
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

        <Field label="Supporting Attachment (Max 10MB)">
          <input 
            type="file" 
            accept=".pdf,.docx,.png,.jpg,.jpeg" 
            onChange={handleFileChange}
            disabled={submitting}
            style={{
              padding: "8px",
              border: "1px dashed #d1d1d1",
              borderRadius: "4px",
              cursor: "pointer"
            }}
          />
        </Field>

        <div className={styles.actions}>
          <Button 
            disabled={submitting} 
            onClick={() => navigate("/")}
          >
            Cancel
          </Button>
          <Button 
            type="submit" 
            appearance="primary" 
            icon={<SendRegular />}
            disabled={submitting}
          >
            {submitting ? "Submitting..." : "Submit Request"}
          </Button>
        </div>
      </form>
    </div>
  );
}
