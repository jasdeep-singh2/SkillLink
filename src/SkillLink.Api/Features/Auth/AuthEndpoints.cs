using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SkillLink.Api.Data;
using SkillLink.Api.Features.Users;

namespace SkillLink.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", Register);
        group.MapPost("/login", Login);

        return app;
    }

    private static async Task<IResult> Register(RegisterRequest req, AppDbContext db, IPasswordHasher<User> hasher, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["email"] = ["A valid email is required."] });
        if (string.IsNullOrWhiteSpace(req.DisplayName))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["displayName"] = ["Display name is required."] });
        if (req.Password is not { Length: >= 8 })
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["password"] = ["Password must be at least 8 characters."] });

        var user = new User { Email = req.Email.Trim(), DisplayName = req.DisplayName.Trim(), Headline = req.Headline };
        // PBKDF2 with a random salt per user: slow on purpose, so a leaked table is expensive to crack.
        user.PasswordHash = hasher.HashPassword(user, req.Password);
        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Results.Conflict(new { error = "A user with this email already exists." });
        }

        return Results.Created($"/api/users/{user.Id}", new UserResponse(user.Id, user.Email, user.DisplayName, user.Headline));
    }

    private static async Task<IResult> Login(LoginRequest req, AppDbContext db, IPasswordHasher<User> hasher, TokenService tokens, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.Trim(), ct);

        // Same answer for "no such email" and "wrong password", so attackers can't discover which emails exist.
        if (user is null || user.PasswordHash.Length == 0)
            return Results.Unauthorized();

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
        if (result == PasswordVerificationResult.Failed)
            return Results.Unauthorized();

        if (result == PasswordVerificationResult.SuccessRehashNeeded)   // hashing settings got stronger since this hash was made
        {
            user.PasswordHash = hasher.HashPassword(user, req.Password);
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(tokens.CreateAccessToken(user));
    }
}
