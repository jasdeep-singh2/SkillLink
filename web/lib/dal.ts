import "server-only";
import { redirect } from "next/navigation";
import { ApiError, apiFetch } from "./api";
import { getToken } from "./session";
import type { Connection, IncomingRequest, Suggestion, User, UserSummary } from "./types";

// Data Access Layer: every call for the logged-in user goes through here, so the
// "is there a valid session?" check can't be forgotten on a new page.
export async function authedFetch<T>(path: string, options: { method?: "GET" | "POST"; body?: unknown } = {}): Promise<T> {
  const token = await getToken();
  if (!token) redirect("/login");

  try {
    return await apiFetch<T>(path, { ...options, token });
  } catch (error) {
    // The JWT expired (15 minutes) or was rejected: send the user back to log in.
    if (error instanceof ApiError && error.status === 401) redirect("/login?expired=1");
    throw error;
  }
}

export const getCurrentUser = () => authedFetch<User>("/api/users/me");
export const getSuggestions = () => authedFetch<Suggestion[]>("/api/me/suggestions");
export const getConnections = () => authedFetch<Connection[]>("/api/me/connections");
export const getIncomingRequests = () => authedFetch<IncomingRequest[]>("/api/me/connections/requests");
export const searchUsers = (q: string) => authedFetch<UserSummary[]>(`/api/users/search?q=${encodeURIComponent(q)}`);

// For UI that shows on every page (the header), including /login: returns null instead of redirecting,
// otherwise an expired cookie on /login would redirect to /login forever.
export async function tryGetCurrentUser(): Promise<User | null> {
  const token = await getToken();
  if (!token) return null;
  try {
    return await apiFetch<User>("/api/users/me", { token });
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) return null;
    throw error;
  }
}
