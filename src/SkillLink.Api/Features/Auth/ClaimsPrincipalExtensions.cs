using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace SkillLink.Api.Features.Auth;

public static class ClaimsPrincipalExtensions
{
    // Only call this on endpoints that RequireAuthorization: there, a valid token always carries "sub".
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("No 'sub' claim. Is this endpoint missing RequireAuthorization?"));
}
