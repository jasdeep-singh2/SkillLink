import "server-only";
import { cookies } from "next/headers";
import { SESSION_COOKIE } from "./session-cookie";

export async function getToken(): Promise<string | undefined> {
  return (await cookies()).get(SESSION_COOKIE)?.value;
}

// Only callable from Server Actions or Route Handlers: cookies can't be set while a page is rendering.
export async function saveToken(token: string, expiresAt: string) {
  (await cookies()).set(SESSION_COOKIE, token, {
    httpOnly: true, // JavaScript in the browser can't read it, so an XSS bug can't steal the token
    secure: process.env.NODE_ENV === "production", // HTTPS only in production (local dev runs on http)
    sameSite: "lax", // not sent on cross-site POSTs: the first line of CSRF defence
    path: "/",
    expires: new Date(expiresAt), // the cookie disappears when the JWT expires
  });
}

export async function deleteToken() {
  (await cookies()).delete(SESSION_COOKIE);
}
