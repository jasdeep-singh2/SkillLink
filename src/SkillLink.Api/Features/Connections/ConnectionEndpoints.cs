using System.Security.Claims;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Npgsql;
using SkillLink.Api.Data;
using SkillLink.Api.Features.Auth;

namespace SkillLink.Api.Features.Connections;

public static class ConnectionEndpoints
{
    public static IEndpointRouteBuilder MapConnectionEndpoints(this IEndpointRouteBuilder app)
    {
        // Every route here acts as the logged-in user, so there are no user IDs in the URLs to tamper with.
        var group = app.MapGroup("/api").WithTags("Connections").RequireAuthorization();

        group.MapPost("/connections", SendRequest);
        group.MapPost("/connections/{requesterId:guid}/accept", AcceptRequest);
        group.MapGet("/me/connections", GetConnections);
        group.MapGet("/me/suggestions", GetSuggestions).RequireRateLimiting("per-user");

        return app;
    }

    private static async Task<IResult> SendRequest(SendConnectionRequest req, ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var me = principal.GetUserId();
        if (me == req.AddresseeId)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["addresseeId"] = ["You can't connect with yourself."] });

        db.Connections.Add(new Connection { RequesterId = me, AddresseeId = req.AddresseeId });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Results.Conflict(new { error = "A connection between these users already exists." });
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            return Results.NotFound(new { error = "One of these users doesn't exist." });
        }

        return Results.Created($"/api/me/connections", null);
    }

    // Only the person who received the request can accept it.
    private static async Task<IResult> AcceptRequest(Guid requesterId, ClaimsPrincipal principal, AppDbContext db, HybridCache cache, CancellationToken ct)
    {
        var addresseeId = principal.GetUserId();

        var updated = await db.Connections
            .Where(c => c.RequesterId == requesterId && c.AddresseeId == addresseeId && c.Status == ConnectionStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Status, ConnectionStatus.Accepted)
                .SetProperty(c => c.RespondedAt, DateTimeOffset.UtcNow), ct);

        if (updated == 0)
            return Results.NotFound(new { error = "No pending request found." });

        // The two people involved see the change immediately.
        // Their friends' suggestions also changed; the 1-minute TTL covers those.
        await cache.RemoveByTagAsync([$"user:{requesterId}", $"user:{addresseeId}"], ct);

        return Results.NoContent();
    }

    private static async Task<IResult> GetConnections(ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var id = principal.GetUserId();
        var friendIds = db.Connections
            .Where(c => c.RequesterId == id && c.Status == ConnectionStatus.Accepted).Select(c => c.AddresseeId)
            .Concat(db.Connections
                .Where(c => c.AddresseeId == id && c.Status == ConnectionStatus.Accepted).Select(c => c.RequesterId));

        var friends = await db.Users.AsNoTracking()
            .Where(u => friendIds.Contains(u.Id))
            .Select(u => new ConnectionResponse(u.Id, u.DisplayName))
            .ToListAsync(ct);

        return Results.Ok(friends);
    }

    private static readonly HybridCacheEntryOptions SuggestionsCacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(1),               // L2 (Redis): our "within 1 minute" requirement
        LocalCacheExpiration = TimeSpan.FromSeconds(30),    // L1 (memory): shorter, may be stale on other instances
    };

    private static async Task<IResult> GetSuggestions(ClaimsPrincipal principal, AppDbContext db, HybridCache cache, CancellationToken ct)
    {
        var id = principal.GetUserId();
        var suggestions = await cache.GetOrCreateAsync(
            $"suggestions:{id}",                            // cache key: one entry per user
            async token => (await db.Database.GetDbConnection().QueryAsync<SuggestionResponse>(
                new CommandDefinition(ConnectionQueries.SuggestionsSql, new { me = id }, cancellationToken: token))).ToList(),
            SuggestionsCacheOptions,
            tags: [$"user:{id}"],                           // lets us invalidate by user
            cancellationToken: ct);

        return Results.Ok(suggestions);
    }


}