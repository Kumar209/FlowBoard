using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Context;
using Project.Service.Application.AI.Interfaces;
using Project.Service.Application.Behaviors;
using Project.Service.Application.Interfaces;
using Project.Service.Infrastructure.AI;
using Project.Service.Infrastructure.AI.Providers;
using Project.Service.Infrastructure.Caching;
using Project.Service.Infrastructure.Messaging;
using Project.Service.Infrastructure.Persistence;
using Shared.Contracts.Events;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(), "logs/log-.json", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30, fileSizeLimitBytes: 10_000_000, rollOnFileSizeLimit: true)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
var cs = builder.Configuration.GetConnectionString("Default") ?? "Server=localhost;Database=flowboard;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
builder.Services.AddDbContext<ProjectDbContext>(o => o.UseSqlServer(cs, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "project")));
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ProjectDbContext>());

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddBehavior(typeof(MediatR.IPipelineBehavior<,>), typeof(CachingBehavior<,>));
});
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
builder.Services.AddScoped<IProjectStatsService, Project.Service.Infrastructure.Services.ProjectStatsService>();
builder.Services.AddScoped<IProjectService, Project.Service.Infrastructure.Services.ProjectService>();
builder.Services.AddScoped<IBoardService, Project.Service.Infrastructure.Services.BoardService>();
builder.Services.AddScoped<ITaskService, Project.Service.Infrastructure.Services.TaskService>();
builder.Services.AddScoped<ISprintService, Project.Service.Infrastructure.Services.SprintService>();
builder.Services.AddScoped<ITeamService, Project.Service.Infrastructure.Services.TeamService>();
builder.Services.AddScoped<IEnvironmentService, Project.Service.Infrastructure.Services.EnvironmentService>();
builder.Services.AddScoped<ICommentService, Project.Service.Infrastructure.Services.CommentService>();
builder.Services.AddScoped<ISubTaskService, Project.Service.Infrastructure.Services.SubTaskService>();
builder.Services.AddScoped<IActivityService, Project.Service.Infrastructure.Services.ActivityService>();
builder.Services.AddScoped<IProjectMemberService, Project.Service.Infrastructure.Services.ProjectMemberService>();
builder.Services.AddScoped<IStatusService, Project.Service.Infrastructure.Services.StatusService>();

builder.Services.AddSingleton<IAiRateLimiter, AiRateLimiter>();
builder.Services.AddHttpClient<GeminiProvider>(c => c.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddHttpClient<GroqProvider>(c => c.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddScoped<IAiService, AiService>();

var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? builder.Configuration["RabbitMQ__Host"] ?? "";
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        if (!string.IsNullOrWhiteSpace(rabbitHost) && !rabbitHost.Contains("PASTE_"))
        {
            cfg.Host(new Uri(rabbitHost));
        }
        else
        {
            cfg.Host("rabbitmq://localhost");
        }
        cfg.Message<TaskCreatedEvent>(c => c.SetEntityName("flowboard.events"));
        cfg.Message<TaskMovedEvent>(c => c.SetEntityName("flowboard.events"));
        cfg.Message<TaskCommentedEvent>(c => c.SetEntityName("flowboard.events"));
        cfg.Publish<TaskCreatedEvent>(c => c.ExchangeType = "fanout");
        cfg.Publish<TaskMovedEvent>(c => c.ExchangeType = "fanout");
        cfg.Publish<TaskCommentedEvent>(c => c.ExchangeType = "fanout");
        cfg.UseMessageRetry(r => r.Immediate(3));
        cfg.ConfigureEndpoints(context);
    });
});
builder.Services.AddHostedService<OutboxBackgroundService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "FlowBoard Project.Service", Version = "v1", Description = "Projects, Lists, Tasks, Comments - 6 Roles (PM can create projects, Client view+comment only) - Task 1.5 + Task 2.x" });
    o.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Bearer. Enter 'Bearer {token}'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    o.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});
builder.Services.AddHealthChecks();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins("http://localhost:4200","https://flowboard.vercel.app").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var jwtKey = builder.Configuration["Jwt:Key"] ?? "PASTE_SUPER_SECRET_32_CHARS_MINIMUM_FOR_HS256";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FlowBoard.Identity";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FlowBoard.Gateway";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
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
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var at = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(at) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs")) ctx.Token = at;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Project.Service v1"));
}
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});
if (!app.Environment.IsDevelopment()) app.UseHsts();
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
app.MapGet("/", () => Results.Ok(new { service = "FlowBoard Project.Service", version = "v1.2", dotnet = "10.0", status = "Running", swagger = "/swagger" }));
app.MapGet("/health/ready", () => Results.Ok(new { service = "Project.Service", status = "Ready", timestamp = DateTime.UtcNow }));
app.MapControllers();
app.Run();
public partial class Program { }
