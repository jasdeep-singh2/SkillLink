import { NextResponse, type NextRequest } from "next/server";
import { SESSION_COOKIE } from "./lib/session-cookie";

// Optimistic check: no cookie at all means no point rendering a private page, so redirect early.
// It doesn't prove the token is valid (that would need the API); lib/dal.ts does the real check.
export function proxy(request: NextRequest) {
  if (!request.cookies.has(SESSION_COOKIE)) {
    return NextResponse.redirect(new URL("/login", request.url));
  }
}

export const config = {
  matcher: ["/", "/people", "/connections"],
};
