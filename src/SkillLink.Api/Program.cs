using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SkillLink.Api.Data;
using SkillLink.Api.Features.Auth;
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

// --- Authentication: who are you? (validate the JWT on every request) ---
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();                       // a missing signing key fails at startup, not on the first login
var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;           // keep "sub" as "sub" instead of renaming it to a long URI
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = TokenService.CreateSigningKey(jwt.SigningKey),
            ClockSkew = TimeSpan.FromSeconds(30),    // default is 5 minutes, too generous for 15-minute tokens
        };
    });
// --- Authorization: are you allowed? (for now: "must be logged in") ---
builder.Services.AddAuthorization();

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // One bucket per logged-in user, taken from the token. Clients can't fake it by changing the URL.
    options.AddPolicy("per-user", httpContext =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: httpContext.User.Identity?.IsAuthenticated == true
                ? httpContext.User.GetUserId().ToString()
                : "anonymous",
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

// Order matters: the rate limiter needs to know the user, so authentication runs first.
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/instance", () => new { processId = Environment.ProcessId });
    app.MapOpenApi();
    await app.ApplyMigrationsAsync();
}

app.MapGet("/api/hello", () => new { message = "Hello from .NET" });
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapConnectionEndpoints(); 
app.Run();