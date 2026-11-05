var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
                      .WithDataVolume()
                      .WithPgAdmin();
var db = postgres.AddDatabase("skilllink");

var api = builder.AddProject<Projects.SkillLink_Api>("api")
                 .WithReference(db)
                 .WaitFor(db);

builder.AddJavaScriptApp("web", "../../web")
       .WithReference(api)
       .WithHttpEndpoint(env: "PORT")
       .WithExternalHttpEndpoints();

builder.Build().Run();