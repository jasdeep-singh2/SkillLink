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
        group.MapGet("/{id:guid}", GetUser);

        return app;
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
