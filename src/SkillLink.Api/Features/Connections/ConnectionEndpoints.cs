using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SkillLink.Api.Data;

namespace SkillLink.Api.Features.Connections;

public static class ConnectionEndpoints
{
    public static IEndpointRouteBuilder MapConnectionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Connections");

        group.MapPost("/connections", SendRequest);
        group.MapPost("/connections/{requesterId:guid}/{addresseeId:guid}/accept", AcceptRequest);
        group.MapGet("/users/{id:guid}/connections", GetConnections);
        group.MapGet("/users/{id:guid}/suggestions", GetSuggestions);

        return app;
    }

    // TEMPORARY: the requester comes from the request body until we add auth.
    // Right now anyone could send a request "as" anyone else.
    private static async Task<IResult> SendRequest(SendConnectionRequest req, AppDbContext db, CancellationToken ct)
    {
        if (req.RequesterId == req.AddresseeId)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["addresseeId"] = ["You can't connect with yourself."] });

        db.Connections.Add(new Connection { RequesterId = req.RequesterId, AddresseeId = req.AddresseeId });

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

        return Results.Created($"/api/connections/{req.RequesterId}/{req.AddresseeId}", null);
    }

    private static async Task<IResult> AcceptRequest(Guid requesterId, Guid addresseeId, AppDbContext db, CancellationToken ct)
    {
        // Atomic conditional update: only one concurrent accept/reject can win.
        var updated = await db.Connections
            .Where(c => c.RequesterId == requesterId && c.AddresseeId == addresseeId && c.Status == ConnectionStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Status, ConnectionStatus.Accepted)
                .SetProperty(c => c.RespondedAt, DateTimeOffset.UtcNow), ct);

        return updated == 1 ? Results.NoContent() : Results.NotFound(new { error = "No pending request found." });
    }

    private static async Task<IResult> GetConnections(Guid id, AppDbContext db, CancellationToken ct)
    {
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

    private static async Task<IResult> GetSuggestions(Guid id, AppDbContext db, CancellationToken ct)
    {
        var suggestions = await db.Database.GetDbConnection().QueryAsync<SuggestionResponse>(
            new CommandDefinition(SuggestionsSql, new { me = id }, cancellationToken: ct));

        return Results.Ok(suggestions);
    }

    private const string SuggestionsSql = """
    WITH my_links AS (      -- everyone I have ANY connection row with (~200 rows)
        SELECT addressee_id AS other_id, status FROM connections WHERE requester_id = @me
        UNION ALL
        SELECT requester_id, status FROM connections WHERE addressee_id = @me
    ),
    my_friends AS (
        SELECT other_id AS friend_id FROM my_links WHERE status = 'Accepted'
    ),
    second_hop AS (
        SELECT f.friend_id, c.addressee_id AS candidate_id
        FROM my_friends f JOIN connections c ON c.requester_id = f.friend_id AND c.status = 'Accepted'
        UNION ALL
        SELECT f.friend_id, c.requester_id
        FROM my_friends f JOIN connections c ON c.addressee_id = f.friend_id AND c.status = 'Accepted'
    ),
    ranked AS (             -- aggregate and limit BEFORE touching users
        SELECT h.candidate_id, COUNT(DISTINCT h.friend_id)::int AS mutual_count
        FROM second_hop h
        WHERE h.candidate_id <> @me
          AND NOT EXISTS (SELECT 1 FROM my_links l WHERE l.other_id = h.candidate_id)
        GROUP BY h.candidate_id
        ORDER BY mutual_count DESC
        LIMIT 10
    )
    SELECT u.id AS "Id", u.display_name AS "DisplayName", r.mutual_count AS "MutualCount"
    FROM ranked r
    JOIN users u ON u.id = r.candidate_id
    ORDER BY r.mutual_count DESC;
    """;
}