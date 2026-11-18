using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace SkillLink.Api.Tests.Infrastructure;

// Runs the real API in memory against real Postgres and Redis in Docker.
// Real dependencies catch what fakes hide: unique indexes, citext, the suggestions SQL, cache serialization.
public sealed class SkillLinkApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SigningKey = "integration-tests-signing-key-not-a-secret-0123456789abcdefghijkl";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

    public async ValueTask InitializeAsync() =>
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Same config keys Aspire would inject in a normal run. The environment stays "Development",
        // so Program.cs applies the EF migrations to the fresh database on startup.
        builder.UseSetting("ConnectionStrings:skilllink", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:cache", _redis.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", SigningKey);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }
}

// One factory (and one pair of containers) shared by every test class in this collection.
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<SkillLinkApiFactory>
{
    public const string Name = "api";
}
