using Microsoft.EntityFrameworkCore;
using SkillLink.Api.Data;
using SkillLink.Api.Features.Connections;
using SkillLink.Api.Features.Users;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("skilllink",
    configureDbContextOptions: o => o.UseSnakeCaseNamingConvention());
builder.AddRedisDistributedCache("cache");   // L2: Redis (name matches the AppHost)
builder.Services.AddHybridCache();           // L1 + L2, using Redis automatically
builder.Services.AddOpenApi();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // TEMPORARY: partition by the user ID in the route until we have authentication.
    options.AddPolicy("per-user", httpContext =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: httpContext.Request.RouteValues["id"]?.ToString() ?? "anonymous",
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 10,                                  // burst size
                TokensPerPeriod = 5,                              // refill amount...
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),    // ...per second
                QueueLimit = 0,                                   // reject immediately, don't queue
                AutoReplenishment = true,
            }));

    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        return ValueTask.CompletedTask;
    };
});
var app = builder.Build();

app.UseRateLimiter();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/instance", () => new { processId = Environment.ProcessId });
    app.MapOpenApi();
    await app.ApplyMigrationsAsync();
}

app.MapGet("/api/hello", () => new { message = "Hello from .NET" });
app.MapUserEndpoints();
app.MapConnectionEndpoints(); 
app.Run();