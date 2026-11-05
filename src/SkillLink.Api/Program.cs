var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();          // ← add after CreateBuilder
// ...existing code...
var app = builder.Build();
app.MapDefaultEndpoints();             // ← add after Build (adds /health and /alive)