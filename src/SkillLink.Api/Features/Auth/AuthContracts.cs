using System.ComponentModel.DataAnnotations;

namespace SkillLink.Api.Features.Auth;

// Rules live on the request types. AddValidation() checks them before the endpoint runs
// and returns 400 with a ProblemDetails "errors" dictionary if any fail.
// ErrorMessage: the default texts ("must be a string or array type with a minimum length...") are shown to users.
public sealed record RegisterRequest(
    [Required(ErrorMessage = "Enter your email."), EmailAddress(ErrorMessage = "Enter a valid email address."),
     MaxLength(254, ErrorMessage = "Email must be at most 254 characters.")] string Email,
    [Required(ErrorMessage = "Enter your name."), MaxLength(100, ErrorMessage = "Name must be at most 100 characters.")] string DisplayName,
    [MaxLength(200, ErrorMessage = "Headline must be at most 200 characters.")] string? Headline,
    [Required(ErrorMessage = "Enter a password."), MinLength(8, ErrorMessage = "Password must be at least 8 characters."),
     MaxLength(128, ErrorMessage = "Password must be at most 128 characters.")] string Password);   // max stops huge inputs making hashing slow

public sealed record LoginRequest(
    [Required] string Email,
    [Required] string Password);

public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt);
