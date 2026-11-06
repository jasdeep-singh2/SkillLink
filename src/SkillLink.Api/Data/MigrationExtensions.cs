using Microsoft.EntityFrameworkCore;

namespace SkillLink.Api.Data;

public static class MigrationExtensions
{
    // Dev only: in production, migrations run as a pipeline step, not at startup.
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }
}