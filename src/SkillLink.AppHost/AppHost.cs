var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
                      .WithDataVolume()
                      .WithPgAdmin();

var db = postgres.AddDatabase("skilllink");

var cache = builder.AddRedis("cache")
                   .WithRedisInsight();

// Random 64-char key, generated once and saved to the AppHost's user secrets (outside the repo).
var jwtSigningKey = builder.AddParameter("jwt-signing-key",
    new GenerateParameterDefault { MinLength = 64, Special = false }, secret: true, persist: true);

var api = builder.AddProject<Projects.SkillLink_Api>("api")
                 .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
                 .WithReference(db)
                 .WithReference(cache)
                 .WaitFor(db)
                 .WaitFor(cache)
                 .WithReplicas(1);

builder.AddJavaScriptApp("web", "../../web")
       .WithReference(api)
       .WithHttpEndpoint(env: "PORT")
       .WithExternalHttpEndpoints();

builder.Build().Run();