// FlowBoard Gateway.YARP - API Gateway using YARP 2.3 on .NET 10
// YARP = Yet Another Reverse Proxy (Microsoft's official reverse proxy, replaces Ocelot)
// Purpose: Single public entry point for Angular 22. Angular calls http://localhost:5000 (Gateway),
//          Gateway forwards to internal microservices :5001-5004 based on yarp.json routes.
//          Without Gateway, Angular would need 4 base URLs and CORS/JWT duplicated 4 times.

// 1. Create WebApplication builder - sets up DI container, config, logging, Kestrel server
var builder = WebApplication.CreateBuilder(args);

// 2. Load yarp.json into configuration
//    - AddJsonFile reads backend/Gateway.YARP/yarp.json (contains Routes + Clusters)
//    - optional: false = fail if file missing, reloadOnChange: true = hot-reload routes without restart
//    - This makes builder.Configuration.GetSection("ReverseProxy") available for YARP
builder.Configuration.AddJsonFile("yarp.json", optional: false, reloadOnChange: true);

// 3. Register YARP reverse proxy services
//    - AddReverseProxy() registers YARP's routing, forwarding, load balancing, health checks
//    - LoadFromConfig() reads the "ReverseProxy" section from yarp.json (Routes + Clusters)
//    - NuGet required: Yarp.ReverseProxy 2.3.0 (installed via dotnet add package - also pulls System.IO.Hashing 8.0.0)
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// 4. Register health checks
//    - AddHealthChecks() enables /health endpoint for monitoring (used by YARP and MonsterASP.net)
//    - Each microservice also has /health, Gateway aggregates them
builder.Services.AddHealthChecks();

// 5. Register CORS policy
//    - CORS = Cross-Origin Resource Sharing - allows Angular (different origin) to call Gateway
//    - WithOrigins: localhost:4200 (local ng serve) + vercel.app (prod) are allowed
//    - AllowAnyHeader/Method: Allow Authorization (JWT Bearer), Content-Type, etc.
//    - AllowCredentials: Required for SignalR + Cookie (HttpOnly refresh token) to work
//    - Without CORS, browser blocks Angular fetch to Gateway with "CORS policy" error
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

// 6. Build the app - compiles DI, validates yarp.json routes, prepares middleware pipeline
var app = builder.Build();

// 7. Enable CORS middleware - must be before MapReverseProxy so preflight OPTIONS requests are handled
app.UseCors();

// 8. Map health check endpoint
//    - GET /health returns 200 if app healthy (used by MonsterASP.net, Vercel, k6 load tests)
//    - Example: curl http://localhost:5000/health -> {"status":"Healthy"}
app.MapHealthChecks("/health");

// 9. Map ready check endpoint
//    - GET /health/ready is a more detailed readiness probe (could check downstream services in future)
//    - Returns JSON with timestamp for debugging
app.MapGet("/health/ready", () => Results.Ok(new { status = "Ready", timestamp = DateTime.UtcNow }));

// 10. Map root endpoint - simple info for verifying Gateway is running
//     - GET / returns service name, version, dotnet/yarp versions
//     - Example: curl http://localhost:5000/ -> {"service":"FlowBoard Gateway.YARP","version":"v1.2",...}
app.MapGet("/", () => Results.Ok(new { service = "FlowBoard Gateway.YARP", version = "v1.2", status = "Running", dotnet = "10.0", yarp = "2.3" }));

// 11. Map YARP reverse proxy middleware - THE CORE
//     - Intercepts every request not matched above and forwards based on yarp.json Routes
//     - Example: Angular GET http://localhost:5000/api/auth/login -> Gateway matches "identity-route" (Path: /api/auth/{**catch-all}) -> forwards to http://localhost:5001/api/auth/login
//     - {**catch-all} = wildcard - forwards any subpath (/api/auth/login, /api/auth/me, etc.)
//     - ClusterId picks destination (identity-cluster -> 5001, project-cluster -> 5002, etc.)
//     - In production (Task 5.4), yarp.json Addresses will change to https://identity-xxxxx.monsterasp.net etc.
app.MapReverseProxy();

// 12. Run the app - starts Kestrel on ports from Properties/launchSettings.json (http 5000, https 5001)
//     - Blocks here until Ctrl+C, listening for requests
app.Run();
