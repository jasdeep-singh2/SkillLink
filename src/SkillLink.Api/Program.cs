using Microsoft.EntityFrameworkCore;
using SkillLink.Api.Data;
using SkillLink.Api.Features.Users;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("skilllink",
    configureDbContextOptions: o => o.UseSnakeCaseNamingConvention());
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await app.ApplyMigrationsAsync();
}

app.MapGet("/api/hello", () => new { message = "Hello from .NET" });
app.MapUserEndpoints();

app.Run();