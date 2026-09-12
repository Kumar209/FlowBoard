// FlowBoard Gateway.YARP - API Gateway YARP 2.3 + Sliding Window Counter + Serilog + Scalar
using Serilog;
using StackExchange.Redis;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
using Gateway.YARP.Middleware;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(), "logs/log-.json", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30, fileSizeLimitBytes: 10_000_000, rollOnFileSizeLimit: true)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Forwarded headers for trusted proxy - do not trust X-Forwarded-For directly
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// YARP config
builder.Configuration.AddJsonFile("yarp.json", optional: false, reloadOnChange: true);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Redis singleton for rate limiting via DI
var redisConn = builder.Configuration["Redis:Connection"] ?? builder.Configuration["Redis__Connection"] ?? "";
if (!string.IsNullOrWhiteSpace(redisConn) && !redisConn.Contains("PASTE_"))
{
    try
    {
        ConfigurationOptions opts;
        if (redisConn.StartsWith("rediss://") || redisConn.StartsWith("redis://"))
            opts = ConfigurationOptions.Parse(redisConn);
        else
            opts = ConfigurationOptions.Parse(redisConn);
        opts.AbortOnConnectFail = false;
        opts.ConnectRetry = 3;
        opts.ConnectTimeout = 5000;
        var mux = ConnectionMultiplexer.Connect(opts);
        builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
    }
    catch { }
}

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
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Forwarded headers must be early
app.UseForwardedHeaders();

// Security headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});
if (!app.Environment.IsDevelopment()) app.UseHsts();

// CorrelationId first, before Serilog logging
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

// Rate limiting after correlation and logging, before CORS
app.UseMiddleware<RateLimitMiddleware>();

app.UseCors();

app.MapHealthChecks("/health");
app.MapGet("/health/ready", () => Results.Ok(new { status = "Ready", timestamp = DateTime.UtcNow }));
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "FlowBoard Gateway - Scalar Docs";
    options.Theme = ScalarTheme.BluePlanet;
});
app.MapGet("/", () => Results.Ok(new { service = "FlowBoard Gateway.YARP", version = "v1.2", status = "Running", dotnet = "10.0", yarp = "2.3" }));
app.MapReverseProxy();
app.Run();
