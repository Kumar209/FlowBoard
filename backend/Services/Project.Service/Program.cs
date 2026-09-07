using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Project.Service.Application.Behaviors;
using Project.Service.Application.Interfaces;
using Project.Service.Infrastructure.Caching;
using Project.Service.Infrastructure.Messaging;
using Project.Service.Infrastructure.Persistence;
using Shared.Contracts.Events;

var builder = WebApplication.CreateBuilder(args);
// DbContext - Same DB flowboard with schema [project] (Task 2.1) - DIP with IApplicationDbContext (like Identity Task 1.2.1)
var cs = builder.Configuration.GetConnectionString("Default") ?? "Server=localhost;Database=flowboard;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
builder.Services.AddDbContext<ProjectDbContext>(o => o.UseSqlServer(cs, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "project")));
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ProjectDbContext>());

// MNC-GRADE MediatR PIPELINE - Register all handlers from this assembly + add CachingBehavior as IPipelineBehavior
// What this line does: For EVERY MediatR Send(request), MediatR will first create CachingBehavior<TRequest,TResponse> (if TRequest is ICacheableRequest) and call its Handle() before the actual Handler.
// Why used: Keeps Api thin (controller just Send(query)), caching is auto for any ICacheableRequest (GetBoard, GetTasks) via pipeline, reuse across all services (File, Notification, Gemini). Without this, each controller would repeat GetAsync/SetAsync manually (duplication, missed invalidation). Boilerplate is intentional for MNC prod-grade.
// Concrete class CachingBehavior implements interface IPipelineBehavior<,> (MediatR's abstraction) - we always write concrete class, MediatR calls it via interface.
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddBehavior(typeof(MediatR.IPipelineBehavior<,>), typeof(CachingBehavior<,>));
});
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
builder.Services.AddScoped<IProjectService, Project.Service.Infrastructure.Services.ProjectService>();
builder.Services.AddScoped<IBoardService, Project.Service.Infrastructure.Services.BoardService>();
builder.Services.AddScoped<ITaskService, Project.Service.Infrastructure.Services.TaskService>();
builder.Services.AddScoped<ISprintService, Project.Service.Infrastructure.Services.SprintService>();
builder.Services.AddScoped<ITeamService, Project.Service.Infrastructure.Services.TeamService>();
builder.Services.AddScoped<IEnvironmentService, Project.Service.Infrastructure.Services.EnvironmentService>();
builder.Services.AddScoped<ICommentService, Project.Service.Infrastructure.Services.CommentService>();
builder.Services.AddScoped<ISubTaskService, Project.Service.Infrastructure.Services.SubTaskService>();
builder.Services.AddScoped<IActivityService, Project.Service.Infrastructure.Services.ActivityService>();

// MassTransit 8.3 + CloudAMQP (same amqps:// key local/prod, 2s Outbox poll, durable quorum, retry 3x + _error)
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
            // Fallback to in-memory for local dev without CloudAMQP (best-effort, Outbox still persists)
            cfg.Host("rabbitmq://localhost");
        }
        // Contracts -> durable fanout exchange flowboard.events (quorum, same key local/prod)
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

// JWT auth - same key as Identity.Service (HS256, 15m) 
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
// Seed demo data (Task 2.1) - 1 Project + 3 Lists + 12 Tasks
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
    // Ensure DB created (migration already applied, but ensure)
    try { await ProjectSeeder.SeedAsync(db); } catch (Exception ex) { Console.WriteLine($"[Seeder] {ex.Message}"); }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Project.Service v1"));
}
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "FlowBoard Project.Service", version = "v1.2", dotnet = "10.0", status = "Running", swagger = "/swagger" }));
app.MapGet("/health/ready", () => Results.Ok(new { service = "Project.Service", status = "Ready", timestamp = DateTime.UtcNow }));
app.MapControllers();
app.Run();
public partial class Program { }
