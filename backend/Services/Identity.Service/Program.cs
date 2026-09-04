using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Identity.Service.Application.Interfaces;
using Identity.Service.Application.Services;
using Identity.Service.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 1. DbContext - Single DB flowboard with schema [identity], Server=localhost (from appsettings.Development.json)
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Server=localhost;Database=flowboard;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(connectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<IdentityDbContext>());

// 2. MediatR - CQRS handlers for Register/Login/Refresh (FluentValidation validators are auto-discovered but not required for build)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// 3. Application services - DIP interfaces (enterprise)
builder.Services.AddScoped<IJwtProvider, JwtProvider>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddHttpClient<BrevoEmailService>();

// 4. JWT Authentication - reads Jwt:Key/Issuer/Audience from config (32+ chars, HS256, 15m)
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
        // SignalR Hub will use query ?access_token=xxx - also allow header Authorization: Bearer xxx
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

// 5. Authorization - 6 roles (SuperAdmin, OrgAdmin, ProjectManager, Member, Client, Viewer) + Tenant isolation via WorkspaceId
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireOrgAdmin", policy => policy.RequireRole("OrgAdmin", "SuperAdmin"));
    options.AddPolicy("RequireProjectManager", policy => policy.RequireRole("ProjectManager", "OrgAdmin", "SuperAdmin"));
    options.AddPolicy("RequireMember", policy => policy.RequireRole("Member", "ProjectManager", "OrgAdmin", "SuperAdmin", "Client", "Viewer"));
});

// 6. Controllers + CORS + Health
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins("http://localhost:4200", "https://flowboard.vercel.app").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

// 7. Middleware pipeline
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/health/ready", () => Results.Ok(new { service = "Identity.Service", status = "Ready", timestamp = DateTime.UtcNow, dotnet = "10.0", db = "flowboard[identity]" }));
app.MapGet("/", () => Results.Ok(new { service = "FlowBoard Identity.Service", version = "v1.2", dotnet = "10.0", status = "Running", db = "flowboard[identity]", auth = "JWT 15m + Refresh 7d" }));

app.MapControllers();

app.Run();

// For integration tests (WebApplicationFactory)
public partial class Program { }
