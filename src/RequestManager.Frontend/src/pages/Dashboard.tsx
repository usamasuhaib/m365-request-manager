import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { 
  Card, 
  Text, 
  Button, 
  Table, 
  TableHeader, 
  TableRow, 
  TableHeaderCell, 
  TableBody, 
  TableCell,
  Spinner,
  makeStyles,
  tokens
} from "@fluentui/react-components";
import { 
  AddRegular, 
  CheckmarkCircleRegular, 
  DismissCircleRegular, 
  ClockRegular, 
  DocumentFolderRegular 
} from "@fluentui/react-icons";
import { fetchDashboardData, DashboardData } from "../services/apiService";

const useStyles = makeStyles({
  container: {
    padding: "24px",
    display: "flex",
    flexDirection: "column",
    gap: "24px",
  },
  header: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center"
  },
  metricsGrid: {
    display: "grid",
    gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))",
    gap: "16px"
  },
  metricCard: {
    padding: "16px",
    display: "flex",
    flexDirection: "row",
    alignItems: "center",
    gap: "16px",
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    boxShadow: tokens.shadow2
  },
  iconContainer: {
    display: "flex",
    padding: "10px",
    borderRadius: tokens.borderRadiusCircular,
    color: "#fff"
  },
  blue: { backgroundColor: "#0078d4" },
  yellow: { backgroundColor: "#ffb900" },
  green: { backgroundColor: "#107c41" },
  red: { backgroundColor: "#a80000" },
  recentSection: {
    backgroundColor: tokens.colorNeutralBackground1,
    padding: "20px",
    borderRadius: tokens.borderRadiusMedium,
    boxShadow: tokens.shadow2,
    display: "flex",
    flexDirection: "column",
    gap: "12px"
  },
  clickableRow: {
    cursor: "pointer",
    ":hover": {
      backgroundColor: tokens.colorNeutralBackground1Hover
    }
  }
});

export default function Dashboard() {
  const styles = useStyles();
  const navigate = useNavigate();
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchDashboardData()
      .then((res) => {
        setData(res);
        setLoading(false);
      })
      .catch((err) => {
        setError(err.message || "Failed to load data");
        setLoading(false);
      });
  }, []);

  if (loading) {
    return (
      <div style={{ display: "flex", justifyContent: "center", padding: "100px" }}>
        <Spinner label="Loading dashboard metrics..." />
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ padding: "24px", color: "#a80000" }}>
        <h3>Error Loading Dashboard</h3>
        <p>{error}</p>
      </div>
    );
  }

  const metrics = data?.metrics || { total: 0, pending: 0, approved: 0, rejected: 0 };
  const requests = data?.data || [];

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <Text as="h1" size={700} weight="semibold">Request Manager Dashboard</Text>
          <br />
          <Text size={300} style={{ color: tokens.colorNeutralForeground4 }}>Manage and track M365 approvals and requests in real-time.</Text>
        </div>
        <Button 
          icon={<AddRegular />} 
          appearance="primary" 
          onClick={() => navigate("/create")}
        >
          Create Request
        </Button>
      </div>

      {/* Metrics Cards */}
      <div className={styles.metricsGrid}>
        <Card className={styles.metricCard}>
          <div className={`${styles.iconContainer} ${styles.blue}`}>
            <DocumentFolderRegular />
          </div>
          <div>
            <Text size={200} style={{ color: tokens.colorNeutralForeground4 }}>Total Requests</Text>
            <br />
            <Text size={600} weight="bold">{metrics.total}</Text>
          </div>
        </Card>

        <Card className={styles.metricCard}>
          <div className={`${styles.iconContainer} ${styles.yellow}`}>
            <ClockRegular />
          </div>
          <div>
            <Text size={200} style={{ color: tokens.colorNeutralForeground4 }}>Pending Approval</Text>
            <br />
            <Text size={600} weight="bold">{metrics.pending}</Text>
          </div>
        </Card>

        <Card className={styles.metricCard}>
          <div className={`${styles.iconContainer} ${styles.green}`}>
            <CheckmarkCircleRegular />
          </div>
          <div>
            <Text size={200} style={{ color: tokens.colorNeutralForeground4 }}>Approved</Text>
            <br />
            <Text size={600} weight="bold">{metrics.approved}</Text>
          </div>
        </Card>

        <Card className={styles.metricCard}>
          <div className={`${styles.iconContainer} ${styles.red}`}>
            <DismissCircleRegular />
          </div>
          <div>
            <Text size={200} style={{ color: tokens.colorNeutralForeground4 }}>Rejected</Text>
            <br />
            <Text size={600} weight="bold">{metrics.rejected}</Text>
          </div>
        </Card>
      </div>

      {/* Requests List */}
      <div className={styles.recentSection}>
        <Text as="h2" size={400} weight="semibold">Recent Requests</Text>
        {requests.length === 0 ? (
          <Text size={300} style={{ color: tokens.colorNeutralForeground4 }}>No requests logged yet.</Text>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>Request ID</TableHeaderCell>
                <TableHeaderCell>Title</TableHeaderCell>
                <TableHeaderCell>Category</TableHeaderCell>
                <TableHeaderCell>Priority</TableHeaderCell>
                <TableHeaderCell>Status</TableHeaderCell>
                <TableHeaderCell>Submitted Date</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {requests.map((item) => (
                <TableRow 
                  key={item.id} 
                  className={styles.clickableRow}
                  onClick={() => navigate(`/request/${item.id}`)}
                >
                  <TableCell>
                    <strong>{item.requestNumber}</strong>
                  </TableCell>
                  <TableCell>{item.title}</TableCell>
                  <TableCell>{item.category}</TableCell>
                  <TableCell>{item.priority}</TableCell>
                  <TableCell>{item.status}</TableCell>
                  <TableCell>{new Date(item.submittedDate).toLocaleDateString()}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </div>
    </div>
  );
}
