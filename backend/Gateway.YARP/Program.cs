var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("yarp.json", optional: false, reloadOnChange: true);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://flowboard.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();

app.MapHealthChecks("/health");
app.MapGet("/health/ready", () => Results.Ok(new { status = "Ready", timestamp = DateTime.UtcNow }));
app.MapGet("/", () => Results.Ok(new { service = "FlowBoard Gateway.YARP", version = "v1.2", status = "Running", dotnet = "10.0", yarp = "2.3" }));

app.MapReverseProxy();

app.Run();
