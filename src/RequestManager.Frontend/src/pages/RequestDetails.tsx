import React, { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { 
  Text, 
  Button, 
  Textarea,
  Field,
  Spinner,
  makeStyles,
  tokens,
  Divider
} from "@fluentui/react-components";
import { ArrowLeftRegular, SendRegular, CheckmarkRegular, DismissRegular } from "@fluentui/react-icons";
import { fetchRequestById, addComment, approveRequest, rejectRequest, RequestItem } from "../services/apiService";
import { getAuthToken } from "../services/authService";

const useStyles = makeStyles({
  container: {
    padding: "24px",
    maxWidth: "850px",
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
  contentGrid: {
    display: "grid",
    gridTemplateColumns: "2fr 1fr",
    gap: "24px",
    "@media(max-width: 768px)": {
      gridTemplateColumns: "1fr"
    }
  },
  card: {
    backgroundColor: tokens.colorNeutralBackground1,
    padding: "24px",
    borderRadius: tokens.borderRadiusMedium,
    boxShadow: tokens.shadow2,
    display: "flex",
    flexDirection: "column",
    gap: "16px"
  },
  badge: {
    padding: "4px 8px",
    borderRadius: tokens.borderRadiusSmall,
    fontWeight: "bold",
    fontSize: "12px",
    textTransform: "uppercase",
    display: "inline-block",
    width: "fit-content"
  },
  submitted: { backgroundColor: "#fff2cc", color: "#b27a00" },
  approved: { backgroundColor: "#d5e8d4", color: "#274e13" },
  rejected: { backgroundColor: "#f8cecc", color: "#660000" },
  commentsSection: {
    marginTop: "16px",
    display: "flex",
    flexDirection: "column",
    gap: "12px"
  },
  commentCard: {
    padding: "12px",
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusSmall,
    display: "flex",
    flexDirection: "column",
    gap: "4px"
  },
  commentBox: {
    display: "flex",
    flexDirection: "column",
    gap: "8px",
    marginTop: "12px"
  },
  approvalPanel: {
    display: "flex",
    flexDirection: "column",
    gap: "12px",
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    padding: "16px",
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground3
  }
});

export default function RequestDetails() {
  const styles = useStyles();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [request, setRequest] = useState<RequestItem | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  const [newComment, setNewComment] = useState("");
  const [commenting, setCommenting] = useState(false);

  const [approverComment, setApproverComment] = useState("");
  const [actioning, setActioning] = useState(false);

  const loadData = () => {
    if (!id) return;
    fetchRequestById(parseInt(id))
      .then((res) => {
        setRequest(res);
        setLoading(false);
      })
      .catch((err) => {
        setError(err.message || "Failed to load request.");
        setLoading(false);
      });
  };

  useEffect(() => {
    loadData();
  }, [id]);

  const handleDownload = async (docId: string, docName: string) => {
    try {
      const token = await getAuthToken();
      const apiBase = import.meta.env.VITE_API_BASE_URL || "http://localhost:7071";
      const res = await fetch(`${apiBase}/api/requests/${id}/documents/${docId}`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (!res.ok) throw new Error("Failed to download file.");
      
      const blob = await res.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = docName;
      document.body.appendChild(a);
      a.click();
      a.remove();
      window.URL.revokeObjectURL(url);
    } catch (err: any) {
      alert("Download failed: " + err.message);
    }
  };

  const handlePostComment = async () => {
    if (!id || !newComment.trim()) return;
    setCommenting(true);
    try {
      await addComment(parseInt(id), newComment);
      setNewComment("");
      loadData();
    } catch (err: any) {
      alert(err.message || "Failed to add comment.");
    } finally {
      setCommenting(false);
    }
  };

  const handleApprove = async () => {
    if (!id) return;
    setActioning(true);
    try {
      await approveRequest(parseInt(id), approverComment);
      setApproverComment("");
      loadData();
    } catch (err: any) {
      alert(err.message || "Failed to approve request.");
    } finally {
      setActioning(false);
    }
  };

  const handleReject = async () => {
    if (!id) return;
    setActioning(true);
    try {
      await rejectRequest(parseInt(id), approverComment);
      setApproverComment("");
      loadData();
    } catch (err: any) {
      alert(err.message || "Failed to reject request.");
    } finally {
      setActioning(false);
    }
  };

  if (loading) {
    return (
      <div style={{ display: "flex", justifyContent: "center", padding: "100px" }}>
        <Spinner label="Loading request details..." />
      </div>
    );
  }

  if (error || !request) {
    return (
      <div className={styles.container}>
        <Button icon={<ArrowLeftRegular />} onClick={() => navigate("/")}>Back</Button>
        <Text style={{ color: "#a80000" }}>{error || "Request not found."}</Text>
      </div>
    );
  }

  // Helper to map status style
  const getStatusClass = (status: string) => {
    switch (status.toLowerCase()) {
      case "approved": return styles.approved;
      case "rejected": return styles.rejected;
      default: return styles.submitted;
    }
  };

  const isPending = request.status === "Submitted" || request.status === "Pending Approval";

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Button 
          icon={<ArrowLeftRegular />} 
          appearance="subtle" 
          onClick={() => navigate("/")}
        />
        <Text as="h1" size={600} weight="semibold">Request {request.requestNumber}</Text>
      </div>

      <div className={styles.contentGrid}>
        <div style={{ display: "flex", flexDirection: "column", gap: "24px" }}>
          {/* Main Info */}
          <div className={styles.card}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <span className={`${styles.badge} ${getStatusClass(request.status)}`}>
                {request.status}
              </span>
              <Text size={200} style={{ color: tokens.colorNeutralForeground4 }}>
                Submitted on {new Date(request.submittedDate).toLocaleDateString()}
              </Text>
            </div>
            
            <Text size={500} weight="semibold">{request.title}</Text>
            <Text size={300}>{request.description}</Text>
            
            <Divider />

            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "12px" }}>
              <div>
                <Text size={200} style={{ color: tokens.colorNeutralForeground4 }}>Category</Text>
                <br />
                <Text size={300} weight="semibold">{request.category}</Text>
              </div>
              <div>
                <Text size={200} style={{ color: tokens.colorNeutralForeground4 }}>Priority</Text>
                <br />
                <Text size={300} weight="semibold">{request.priority}</Text>
              </div>
              <div>
                <Text size={200} style={{ color: tokens.colorNeutralForeground4 }}>Submitted By</Text>
                <br />
                <Text size={300} weight="semibold">{request.submittedBy} ({request.submittedByEmail})</Text>
              </div>
              <div>
                <Text size={200} style={{ color: tokens.colorNeutralForeground4 }}>Assigned Approver</Text>
                <br />
                <Text size={300} weight="semibold">{request.assignedTo}</Text>
              </div>
            </div>

            {/* Supporting Documents (AD-5: Secure Proxy Downloads) */}
            {request.documents && request.documents.length > 0 && (
              <>
                <Divider />
                <div>
                  <Text size={200} style={{ color: tokens.colorNeutralForeground4 }}>Attachments</Text>
                  <div style={{ display: "flex", flexDirection: "column", gap: "8px", marginTop: "8px" }}>
                    {request.documents.map((doc) => (
                      <button 
                        key={doc.id} 
                        onClick={() => handleDownload(doc.id, doc.name)}
                        style={{
                          background: "none",
                          border: "none",
                          padding: 0,
                          textAlign: "left",
                          color: tokens.colorBrandForegroundLink,
                          cursor: "pointer",
                          textDecoration: "underline",
                          fontSize: "14px",
                          display: "flex",
                          alignItems: "center",
                          gap: "6px"
                        }}
                      >
                        📄 {doc.name}
                      </button>
                    ))}
                  </div>
                </div>
              </>
            )}

            {request.status === "Approved" && (
              <div className={styles.badge} style={{ backgroundColor: "#e2f0d9", borderLeft: "4px solid #385723", padding: "12px", width: "100%" }}>
                <Text size={300}><strong>Approved by:</strong> {request.approvedBy} on {new Date(request.approvedDate!).toLocaleString()}</Text>
              </div>
            )}

            {request.status === "Rejected" && (
              <div className={styles.badge} style={{ backgroundColor: "#fce4d6", borderLeft: "4px solid #c65911", padding: "12px", width: "100%" }}>
                <Text size={300}><strong>Rejected by:</strong> {request.rejectedBy} on {new Date(request.rejectedDate!).toLocaleString()}</Text>
              </div>
            )}
          </div>

          {/* Comments Section */}
          <div className={styles.card}>
            <Text size={400} weight="semibold">Discussion Thread</Text>
            
            <div className={styles.commentsSection}>
              {request.comments && request.comments.length > 0 ? (
                request.comments.map((c) => (
                  <div key={c.id} className={styles.commentCard}>
                    <div style={{ display: "flex", justifyContent: "space-between" }}>
                      <Text size={200} weight="semibold">{c.commentedBy}</Text>
                      <Text size={100} style={{ color: tokens.colorNeutralForeground4 }}>{new Date(c.commentedDate).toLocaleString()}</Text>
                    </div>
                    <Text size={300}>{c.comment}</Text>
                  </div>
                ))
              ) : (
                <Text size={200} style={{ color: tokens.colorNeutralForeground4 }}>No comments posted yet.</Text>
              )}
            </div>

            <div className={styles.commentBox}>
              <Field label="Post Comment">
                <Textarea 
                  value={newComment} 
                  onChange={(e) => setNewComment(e.target.value)} 
                  placeholder="Type a comment or query..."
                  disabled={commenting}
                />
              </Field>
              <Button 
                appearance="primary" 
                icon={<SendRegular />} 
                onClick={handlePostComment}
                disabled={commenting || !newComment.trim()}
                style={{ alignSelf: "flex-end" }}
              >
                Post
              </Button>
            </div>
          </div>
        </div>

        {/* Sidebar Approver Controls (Manager View) */}
        <div>
          {isPending && (
            <div className={styles.approvalPanel}>
              <Text size={400} weight="semibold">Approval Decision</Text>
              <Text size={200} style={{ color: tokens.colorNeutralForeground4 }}>Review this request and record your decision.</Text>
              
              <Field label="Decision Notes">
                <Textarea 
                  value={approverComment} 
                  onChange={(e) => setApproverComment(e.target.value)} 
                  placeholder="Provide optional rationale for your approval/rejection..."
                  disabled={actioning}
                />
              </Field>
              
              <div style={{ display: "flex", flexDirection: "column", gap: "8px", marginTop: "8px" }}>
                <Button 
                  appearance="primary" 
                  icon={<CheckmarkRegular />} 
                  onClick={handleApprove}
                  disabled={actioning}
                  style={{ backgroundColor: "#107c41" }}
                >
                  Approve
                </Button>
                <Button 
                  appearance="secondary" 
                  icon={<DismissRegular />} 
                  onClick={handleReject}
                  disabled={actioning}
                  style={{ color: "#a80000", borderColor: "#a80000" }}
                >
                  Reject
                </Button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
