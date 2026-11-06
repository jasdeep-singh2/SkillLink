using Microsoft.EntityFrameworkCore;
using Npgsql;
using SkillLink.Api.Data;

namespace SkillLink.Api.Features.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        group.MapPost("", CreateUser);
        group.MapGet("/{id:guid}", GetUser);

        return app;
    }

    private static async Task<IResult> CreateUser(CreateUserRequest req, AppDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["email"] = ["A valid email is required."] });
        if (string.IsNullOrWhiteSpace(req.DisplayName))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["displayName"] = ["Display name is required."] });

        var user = new User { Email = req.Email.Trim(), DisplayName = req.DisplayName.Trim(), Headline = req.Headline };
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

    private static async Task<IResult> GetUser(Guid id, AppDbContext db, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserResponse(u.Id, u.Email, u.DisplayName, u.Headline))
            .FirstOrDefaultAsync(ct);

        return user is null ? Results.NotFound() : Results.Ok(user);
    }
}