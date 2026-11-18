using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SkillLink.Api.Data;

namespace SkillLink.Api.Features.Auth;

public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider time)
{
    private readonly JwtOptions _jwt = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public TokenResponse CreateAccessToken(User user)
    {
        var expiresAt = time.GetUtcNow().AddMinutes(_jwt.AccessTokenMinutes);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Expires = expiresAt.UtcDateTime,
            // Claims are readable by anyone who has the token (it's only signed, not encrypted),
            // so put identifiers here, never secrets.
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.DisplayName),
            ]),
            SigningCredentials = new SigningCredentials(CreateSigningKey(_jwt.SigningKey), SecurityAlgorithms.HmacSha256),
        });

        return new TokenResponse(token, expiresAt);
    }

    public static SymmetricSecurityKey CreateSigningKey(string key) => new(Encoding.UTF8.GetBytes(key));
}
