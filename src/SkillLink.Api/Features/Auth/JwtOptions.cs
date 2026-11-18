using System.ComponentModel.DataAnnotations;

namespace SkillLink.Api.Features.Auth;

// Bound from the "Jwt" config section. The signing key comes from the AppHost (a secret parameter),
// so it never lives in appsettings.json or in git.
public sealed class JwtOptions
{
    public const string Section = "Jwt";

    [Required] public string Issuer { get; init; } = "";      // who created the token (this API)
    [Required] public string Audience { get; init; } = "";    // who the token is meant for
    [Required, MinLength(32)] public string SigningKey { get; init; } = "";   // HMAC-SHA256 needs at least 256 bits
    [Range(1, 1440)] public int AccessTokenMinutes { get; init; } = 15;      // short-lived: a stolen token expires soon
}
