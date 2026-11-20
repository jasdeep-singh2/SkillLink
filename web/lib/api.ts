import "server-only"; // build error if a Client Component ever imports this
import type { ProblemDetails } from "./types";

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails | null,
    public readonly retryAfterSeconds: number | null,
  ) {
    super(problem?.title ?? `API returned ${status}`);
  }
}

type ApiOptions = { method?: "GET" | "POST"; body?: unknown; token?: string };

// The only place that talks to the .NET API. It runs on the Next.js server, so the browser
// never sees the API's address or the user's token.
export async function apiFetch<T>(path: string, { method = "GET", body, token }: ApiOptions = {}): Promise<T> {
  const baseUrl = process.env.services__api__http__0; // injected by Aspire (WithReference(api))
  if (!baseUrl) throw new Error("API address not set. Start the app through the Aspire AppHost.");

  const headers: Record<string, string> = {};
  if (body !== undefined) headers["Content-Type"] = "application/json";
  if (token) headers.Authorization = `Bearer ${token}`;

  const res = await fetch(`${baseUrl}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  if (!res.ok) {
    const isProblem = res.headers.get("content-type")?.includes("json");
    const problem = isProblem ? ((await res.json()) as ProblemDetails) : null;
    const retryAfter = res.headers.get("retry-after");
    throw new ApiError(res.status, problem, retryAfter ? Number(retryAfter) : null);
  }

  // 201 and 204 responses from the API can have an empty body.
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}
