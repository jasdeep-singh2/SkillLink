using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SkillLink.Api.Data;
using SkillLink.Api.Features.Auth;

namespace SkillLink.Api.Features.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        // Creating users moved to POST /api/auth/register (it needs a password now).
        var group = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization();

        group.MapGet("/me", GetMe);
        group.MapGet("/search", Search);
        group.MapGet("/{id:guid}", GetUser);

        return app;
    }

    // Find people by name so new users (no friends, so no suggestions) can make a first connection.
    // A leading-wildcard ILIKE can't use a normal index: fine at this size, needs pg_trgm at scale.
    private static async Task<IResult> Search([MinLength(2), MaxLength(100)] string q, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var me = principal.GetUserId();
        var pattern = "%" + q.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";

        var users = await db.Users.AsNoTracking()
            .Where(u => u.Id != me && EF.Functions.ILike(u.DisplayName, pattern))
            .OrderBy(u => u.DisplayName)
            .Take(20)
            .Select(u => new UserSummary(u.Id, u.DisplayName, u.Headline))
            .ToListAsync(ct);

        return Results.Ok(users);
    }

    private static Task<IResult> GetMe(ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) =>
        GetUser(principal.GetUserId(), db, ct);

    private static async Task<IResult> GetUser(Guid id, AppDbContext db, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserResponse(u.Id, u.Email, u.DisplayName, u.Headline))
            .FirstOrDefaultAsync(ct);

        return user is null ? Results.NotFound() : Results.Ok(user);
    }
}
