"use server";

import { redirect } from "next/navigation";
import { ApiError, apiFetch } from "@/lib/api";
import { deleteToken, saveToken } from "@/lib/session";
import type { TokenResponse, User } from "@/lib/types";

export type AuthFormState =
  | {
      error?: string;
      fieldErrors?: Record<string, string[]>; // keys lower-cased: "email", "password", ...
      values?: { email?: string; displayName?: string; headline?: string }; // refill the form (never the password)
    }
  | undefined;

export async function login(_prev: AuthFormState, formData: FormData): Promise<AuthFormState> {
  const email = String(formData.get("email") ?? "");
  const password = String(formData.get("password") ?? "");

  try {
    await logIn(email, password);
  } catch (error) {
    // 400 (empty fields) and 401 get the same message, matching the API's no-enumeration rule.
    if (error instanceof ApiError && (error.status === 401 || error.status === 400))
      return { error: "Invalid email or password.", values: { email } };
    throw error;
  }

  redirect("/"); // outside try: redirect() works by throwing, and the catch would swallow it
}

export async function register(_prev: AuthFormState, formData: FormData): Promise<AuthFormState> {
  const values = {
    email: String(formData.get("email") ?? ""),
    displayName: String(formData.get("displayName") ?? ""),
    headline: String(formData.get("headline") ?? ""),
  };
  const password = String(formData.get("password") ?? "");

  try {
    await apiFetch<User>("/api/auth/register", {
      method: "POST",
      body: { ...values, headline: values.headline || null, password },
    });
    await logIn(values.email, password);
  } catch (error) {
    if (error instanceof ApiError && error.status === 400)
      return { fieldErrors: lowerCaseKeys(error.problem?.errors ?? {}), values };
    if (error instanceof ApiError && error.status === 409)
      return { fieldErrors: { email: ["An account with this email already exists."] }, values };
    throw error;
  }

  redirect("/");
}

export async function logout() {
  await deleteToken();
  redirect("/login");
}

async function logIn(email: string, password: string) {
  const { accessToken, expiresAt } = await apiFetch<TokenResponse>("/api/auth/login", {
    method: "POST",
    body: { email, password },
  });
  await saveToken(accessToken, expiresAt);
}

function lowerCaseKeys(errors: Record<string, string[]>) {
  return Object.fromEntries(Object.entries(errors).map(([key, messages]) => [key.toLowerCase(), messages]));
}
