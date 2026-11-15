var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
                      .WithDataVolume()
                      .WithPgAdmin();

var db = postgres.AddDatabase("skilllink");

var cache = builder.AddRedis("cache")
                   .WithRedisInsight();

var api = builder.AddProject<Projects.SkillLink_Api>("api")
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