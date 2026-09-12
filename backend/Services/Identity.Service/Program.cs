using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Context;
using Identity.Service.Application.Interfaces;
using Identity.Service.Infrastructure.Services;
using Identity.Service.Infrastructure.Persistence;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(), "logs/log-.json", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30, fileSizeLimitBytes: 10_000_000, rollOnFileSizeLimit: true)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// 1. DbContext
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Server=localhost;Database=flowboard;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(connectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<IdentityDbContext>());

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

builder.Services.AddMemoryCache();
builder.Services.AddScoped<IJwtProvider, JwtProvider>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IOrganizationRoleService, OrganizationRoleService>();
builder.Services.AddScoped<IOrganizationActivityService, OrganizationActivityService>();
builder.Services.AddScoped<IOrganizationStatsService, OrganizationStatsService>();
builder.Services.AddHttpClient<IBrevoEmailService, BrevoEmailService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing - set in appsettings.Development.json");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FlowBoard.Identity";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FlowBoard.Gateway";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireOrgAdmin", policy => policy.RequireRole("OrgAdmin", "SuperAdmin"));
    options.AddPolicy("RequireProjectManager", policy => policy.RequireRole("ProjectManager", "OrgAdmin", "SuperAdmin"));
    options.AddPolicy("RequireMember", policy => policy.RequireRole("Member", "ProjectManager", "OrgAdmin", "SuperAdmin", "Client", "Viewer"));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "FlowBoard Identity.Service", Version = "v1", Description = "Auth + Workspaces + Organizations - 6 Roles (PM can create projects) - JWT 15m + Refresh 7d HttpOnly" });
    o.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer {token}'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    o.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddHealthChecks();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins("http://localhost:4200", "https://flowboard.vercel.app").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await IdentitySeeder.SeedSuperAdminAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity.Service v1"));
}

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
app.Use(async (ctx, next) =>
{
    var cid = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(cid) || cid.Length > 100) cid = Guid.NewGuid().ToString("N");
    ctx.Items["X-Correlation-Id"] = cid;
    ctx.Response.OnStarting(() => { ctx.Response.Headers["X-Correlation-Id"] = cid!; return Task.CompletedTask; });
    var userId = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
    var workspaceId = ctx.Request.RouteValues["workspaceId"]?.ToString() ?? ctx.Request.RouteValues["wid"]?.ToString() ?? ctx.User.FindFirst("workspace_id")?.Value ?? ctx.User.FindFirst("workspaceId")?.Value ?? "";
    var projectId = ctx.Request.RouteValues["projectId"]?.ToString() ?? ctx.Request.RouteValues["pid"]?.ToString() ?? "";
    var orgId = ctx.User.FindFirst("organization_id")?.Value ?? ctx.User.FindFirst("org_id")?.Value ?? ctx.User.FindFirst("OrganizationId")?.Value ?? "";
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", cid!))
    using (Serilog.Context.LogContext.PushProperty("UserId", userId))
    using (Serilog.Context.LogContext.PushProperty("OrganizationId", orgId))
    using (Serilog.Context.LogContext.PushProperty("WorkspaceId", workspaceId))
    using (Serilog.Context.LogContext.PushProperty("ProjectId", projectId))
    {
        await next();
    }
});

app.UseSerilogRequestLogging();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/health/ready", () => Results.Ok(new { service = "Identity.Service", status = "Ready", timestamp = DateTime.UtcNow, dotnet = "10.0", db = "flowboard[identity]" }));
app.MapGet("/", () => Results.Ok(new { service = "FlowBoard Identity.Service", version = "v1.2", dotnet = "10.0", status = "Running", db = "flowboard[identity]", auth = "JWT 15m + Refresh 7d" }));

app.MapControllers();

app.Run();

public partial class Program { }
