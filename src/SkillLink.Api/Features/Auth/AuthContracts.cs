using System.ComponentModel.DataAnnotations;

namespace SkillLink.Api.Features.Auth;

// Rules live on the request types. AddValidation() checks them before the endpoint runs
// and returns 400 with a ProblemDetails "errors" dictionary if any fail.
public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MaxLength(100)] string DisplayName,
    [MaxLength(200)] string? Headline,
    [Required, MinLength(8), MaxLength(128)] string Password);   // max stops huge inputs making hashing slow

public sealed record LoginRequest(
    [Required] string Email,
    [Required] string Password);

public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt);
