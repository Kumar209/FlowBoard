// FlowBoard Gateway.YARP - API Gateway using YARP 2.3 on .NET 10 with Sliding Window Counter + Serilog + Scalar
using Serilog;
using Serilog.Context;
using Scalar.AspNetCore;
using Gateway.YARP.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Serilog JSON daily rolling 7-30d, Console + File per service
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(), "logs/log-.json", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30, fileSizeLimitBytes: 10_000_000, rollOnFileSizeLimit: true)
    .CreateLogger();
builder.Host.UseSerilog();

// Load yarp.json
builder.Configuration.AddJsonFile("yarp.json", optional: false, reloadOnChange: true);

// YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Health checks
builder.Services.AddHealthChecks();

// CORS
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

// OpenAPI for Scalar aggregated docs (Gateway itself)
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Serilog request logging with enrichment already via middleware
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("CorrelationId", http.Items["X-Correlation-Id"]?.ToString() ?? "");
        diag.Set("UserId", http.User.FindFirst("sub")?.Value ?? http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "");
    };
});

// Security headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// CorrelationId must be early
app.UseMiddleware<CorrelationIdMiddleware>();
// Rate limiting sliding window counter via Upstash Redis
app.UseMiddleware<RateLimitMiddleware>();

app.UseCors();

app.MapHealthChecks("/health");
app.MapGet("/health/ready", () => Results.Ok(new { status = "Ready", timestamp = DateTime.UtcNow }));

// OpenAPI + Scalar
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "FlowBoard Gateway - Scalar Docs";
    options.Theme = ScalarTheme.BluePlanet;
});

app.MapGet("/", () => Results.Ok(new { service = "FlowBoard Gateway.YARP", version = "v1.2", status = "Running", dotnet = "10.0", yarp = "2.3", rateLimit = "Sliding Window Counter 60 IP / 100 User via Upstash", serilog = "JSON daily 30d", docs = "/scalar" }));

app.MapReverseProxy();

app.Run();
