import { getAuthToken } from "./authService";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:7071";

// Helper to generate UUIDs for request idempotency (AD-4)
function generateUuid(): string {
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === "x" ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

async function getHeaders(): Promise<HeadersInit> {
  const token = await getAuthToken();
  return {
    "Authorization": `Bearer ${token}`,
    "Content-Type": "application/json"
  };
}

export interface RequestMetrics {
  total: number;
  pending: number;
  approved: number;
  rejected: number;
}

export interface RequestItem {
  id: number;
  requestNumber: string;
  title: string;
  description: string;
  category: string;
  priority: string;
  status: string;
  submittedBy: string;
  submittedByEmail: string;
  submittedDate: string;
  assignedTo: string;
  approvedBy: string;
  approvedDate?: string;
  rejectedBy?: string;
  rejectedDate?: string;
  comments?: CommentItem[];
  documents?: DocumentItem[];
}

export interface DocumentItem {
  id: string;
  name: string;
  uploadedBy: string;
  uploadedDate: string;
  downloadUrl: string;
}

export interface CommentItem {
  id: number;
  requestId: number;
  comment: string;
  commentedBy: string;
  commentedDate: string;
}

export interface DashboardData {
  metrics: RequestMetrics;
  data: RequestItem[];
}

export async function fetchDashboardData(): Promise<DashboardData> {
  const headers = await getHeaders();
  const res = await fetch(`${API_BASE_URL}/api/requests`, { headers });
  if (!res.ok) throw new Error("Failed to load dashboard data.");
  return await res.json();
}

export async function fetchRequestById(id: number): Promise<RequestItem> {
  const headers = await getHeaders();
  const res = await fetch(`${API_BASE_URL}/api/requests/${id}`, { headers });
  if (!res.ok) throw new Error(`Failed to load request details for ID ${id}.`);
  const payload = await res.json();
  return payload.data;
}

export async function createRequest(
  title: string, 
  description: string, 
  category: string, 
  priority: string,
  attachmentName?: string,
  attachmentContent?: string
): Promise<RequestItem> {
  const headers = await getHeaders();
  // Include Idempotency Header (AD-4)
  const clientRequestId = generateUuid();
  
  const res = await fetch(`${API_BASE_URL}/api/requests`, {
    method: "POST",
    headers: {
      ...headers,
      "Client-Request-Id": clientRequestId
    },
    body: JSON.stringify({ 
      title, 
      description, 
      category, 
      priority,
      attachmentName,
      attachmentContent
    })
  });

  if (!res.ok) {
    const err = await res.json();
    throw new Error(err.message || "Failed to submit request.");
  }

  const payload = await res.json();
  return payload.data;
}

export async function addComment(requestId: number, comment: string): Promise<void> {
  const headers = await getHeaders();
  const res = await fetch(`${API_BASE_URL}/api/requests/${requestId}/comment`, {
    method: "POST",
    headers,
    body: JSON.stringify({ comment })
  });
  if (!res.ok) throw new Error("Failed to post comment.");
}

export async function approveRequest(requestId: number, comment: string): Promise<void> {
  const headers = await getHeaders();
  const res = await fetch(`${API_BASE_URL}/api/requests/${requestId}/approve`, {
    method: "POST",
    headers,
    body: JSON.stringify({ comment })
  });
  if (!res.ok) {
    const err = await res.json();
    throw new Error(err.message || "Failed to approve request.");
  }
}

export async function rejectRequest(requestId: number, comment: string): Promise<void> {
  const headers = await getHeaders();
  const res = await fetch(`${API_BASE_URL}/api/requests/${requestId}/reject`, {
    method: "POST",
    headers,
    body: JSON.stringify({ comment })
  });
  if (!res.ok) {
    const err = await res.json();
    throw new Error(err.message || "Failed to reject request.");
  }
}

export async function provisionDatabase(): Promise<void> {
  const res = await fetch(`${API_BASE_URL}/api/setup`);
  if (!res.ok) throw new Error("Database setup failed.");
}
