"use server";

import { refresh } from "next/cache";
import { ApiError } from "@/lib/api";
import { authedFetch } from "@/lib/dal";

// Server Actions are public HTTP endpoints, so anyone can call them with any argument.
// That's safe here because authedFetch sends the caller's own token and the API decides what's allowed.

export async function sendRequest(addresseeId: string) {
  try {
    await authedFetch("/api/connections", { method: "POST", body: { addresseeId } });
  } catch (error) {
    // 409: a request already exists (double click, or two tabs). The result the user wanted is already true.
    if (!(error instanceof ApiError && error.status === 409)) throw error;
  }
  refresh(); // re-render the current page with fresh data
}

export async function acceptRequest(requesterId: string) {
  await authedFetch(`/api/connections/${encodeURIComponent(requesterId)}/accept`, { method: "POST" });
  refresh();
}
