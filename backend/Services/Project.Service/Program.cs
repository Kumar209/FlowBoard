var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins("http://localhost:4200","https://flowboard.vercel.app").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
app.UseCors();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "FlowBoard Project.Service", version = "v1.2", dotnet = "10.0", status = "Running" }));
app.MapGet("/health/ready", () => Results.Ok(new { service = "Project.Service", status = "Ready", timestamp = DateTime.UtcNow }));
app.Run();
