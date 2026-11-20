// Shapes of the .NET API's JSON responses (see the *Contracts.cs files in SkillLink.Api).

export type User = { id: string; email: string; displayName: string; headline: string | null };

export type UserSummary = { id: string; displayName: string; headline: string | null };

export type Suggestion = { id: string; displayName: string; mutualCount: number };

export type Connection = { userId: string; displayName: string };

export type IncomingRequest = { userId: string; displayName: string; sentAt: string };

export type TokenResponse = { accessToken: string; expiresAt: string };

// RFC 9457 error body the API returns for every failure.
export type ProblemDetails = {
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
};
