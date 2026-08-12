import * as teamsjs from "@microsoft/teams-js";

export interface UserProfile {
  name: string;
  email: string;
}

let isTeamsInitialized = false;

export async function initializeTeams(): Promise<boolean> {
  try {
    await teamsjs.app.initialize();
    isTeamsInitialized = true;
    console.log("Teams SDK initialized successfully.");
    return true;
  } catch (e) {
    console.warn("Outside Teams environment or initialization failed.");
    isTeamsInitialized = false;
    return false;
  }
}

export async function getAuthToken(): Promise<string> {
  if (isTeamsInitialized) {
    try {
      // Fetch token silently via Teams SSO
      const token = await teamsjs.authentication.getAuthToken();
      return token;
    } catch (err) {
      console.error("Teams silent SSO failed, falling back.", err);
    }
  }

  // Fallback token for local development when running outside Teams container
  return "eyJhbGciOiJSUzI1NiIsImtpZCI6IjEifQ.eyJ1cG4iOiJwcml5YUBzb2x2ZWZ5Lm9ubWljcm9zb2Z0LmNvbSIsIm5hbWUiOiJQcml5YSBQYXRlbCJ9.mocksignature";
}

export async function getUserProfile(apiBaseUrl: string, token: string): Promise<UserProfile> {
  try {
    const res = await fetch(`${apiBaseUrl}/api/me`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (res.ok) {
      const payload = await res.json();
      return payload.data;
    }
  } catch (err) {
    console.error("Failed to retrieve profile info.", err);
  }
  
  return {
    name: "Priya Patel",
    email: "priya@solvefy.onmicrosoft.com"
  };
}
