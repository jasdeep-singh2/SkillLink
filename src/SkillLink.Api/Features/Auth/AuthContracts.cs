namespace SkillLink.Api.Features.Auth;

public sealed record RegisterRequest(string Email, string DisplayName, string? Headline, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt);
