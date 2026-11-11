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
            new CommandDefinition(ConnectionQueries.SuggestionsSql, new { me = id }, cancellationToken: ct));

        return Results.Ok(suggestions);
    }

    
}