using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Notification.Service.Consumers;
using Notification.Service.Hubs;
using Notification.Service.Infrastructure.Persistence;
using Shared.Contracts.Events;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(), "logs/log-.json", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30, fileSizeLimitBytes: 10_000_000, rollOnFileSizeLimit: true)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

var cs = builder.Configuration.GetConnectionString("Default") ?? "Server=localhost;Database=flowboard;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
builder.Services.AddDbContext<NotificationDbContext>(o => o.UseSqlServer(cs, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "notification")));
builder.Services.AddScoped<Notification.Service.Application.Interfaces.IApplicationDbContext>(sp => sp.GetRequiredService<NotificationDbContext>());
builder.Services.AddScoped<Notification.Service.Application.Interfaces.INotificationService, Notification.Service.Infrastructure.Services.NotificationService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing - set in appsettings.Development.json");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FlowBoard.Identity";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FlowBoard.Gateway";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer, ValidAudience = jwtAudience, IssuerSigningKey = new SymmetricSecurityKey(keyBytes), ClockSkew = TimeSpan.FromMinutes(2)
        };
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var at = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(at) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs")) ctx.Token = at.ToString();
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// SignalR backplane via Redis — best-effort
var redisConn = builder.Configuration["Redis:Connection"] ?? builder.Configuration["Redis__Connection"] ?? "";
if (!string.IsNullOrWhiteSpace(redisConn) && !redisConn.Contains("PASTE_"))
{
    builder.Services.AddSignalR().AddStackExchangeRedis(redisConn, opts =>
    {
        opts.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("FlowBoard");
        opts.Configuration.AbortOnConnectFail = false;
        opts.Configuration.ConnectRetry = 3;
    });
}
else
{
    builder.Services.AddSignalR();
}

// MassTransit 8.3 + CloudAMQP same key local/prod + consumers + retry 3x + _error + quorum durable
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? builder.Configuration["RabbitMQ__Host"] ?? "";
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TaskCreatedConsumer>();
    x.AddConsumer<TaskMovedConsumer>();
    x.AddConsumer<TaskCommentedConsumer>();
    x.AddConsumer<TaskAssignedConsumer>();
    x.AddConsumer<TaskDeletedConsumer>();
    x.AddConsumer<ProjectMemberAddedConsumer>();
    x.AddConsumer<ComplaintCreatedConsumer>();
    // DLQ observability: Fault consumers for _error queues (see FaultConsumers.cs)
    x.AddConsumer<TaskCreatedFaultConsumer>();
    x.AddConsumer<TaskMovedFaultConsumer>();
    x.AddConsumer<TaskCommentedFaultConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        if (!string.IsNullOrWhiteSpace(rabbitHost) && !rabbitHost.Contains("PASTE_"))
            cfg.Host(new Uri(rabbitHost));
        else
            cfg.Host("rabbitmq://localhost");
        cfg.Message<TaskCreatedEvent>(c => c.SetEntityName("flowboard.events"));
        cfg.Message<TaskMovedEvent>(c => c.SetEntityName("flowboard.events"));
        cfg.Message<TaskCommentedEvent>(c => c.SetEntityName("flowboard.events"));
        cfg.Message<TaskAssignedEvent>(c => c.SetEntityName("flowboard.events"));
        cfg.Message<TaskDeletedEvent>(c => c.SetEntityName("flowboard.events"));
        cfg.Message<ProjectMemberAddedEvent>(c => c.SetEntityName("flowboard.events"));
        cfg.Message<ComplaintCreatedEvent>(c => c.SetEntityName("flowboard.events"));
        cfg.Publish<TaskCreatedEvent>(c => c.ExchangeType = "fanout");
        cfg.Publish<TaskMovedEvent>(c => c.ExchangeType = "fanout");
        cfg.Publish<TaskCommentedEvent>(c => c.ExchangeType = "fanout");
        cfg.Publish<TaskAssignedEvent>(c => c.ExchangeType = "fanout");
        cfg.Publish<TaskDeletedEvent>(c => c.ExchangeType = "fanout");
        cfg.Publish<ProjectMemberAddedEvent>(c => c.ExchangeType = "fanout");
        cfg.Publish<ComplaintCreatedEvent>(c => c.ExchangeType = "fanout");
        cfg.UseMessageRetry(r => r.Immediate(3));
        // Quorum durable queues per consumer — Durable must be set before ConfigureConsumer
        cfg.ReceiveEndpoint("notification-task-created", e =>
        {
            e.Durable = true;
            e.ConfigureConsumer<TaskCreatedConsumer>(context);
            e.UseMessageRetry(r => r.Intervals(100, 500, 1000));
        });
        cfg.ReceiveEndpoint("notification-task-moved", e =>
        {
            e.Durable = true;
            e.ConfigureConsumer<TaskMovedConsumer>(context);
            e.UseMessageRetry(r => r.Intervals(100, 500, 1000));
        });
        cfg.ReceiveEndpoint("notification-task-commented", e =>
        {
            e.Durable = true;
            e.ConfigureConsumer<TaskCommentedConsumer>(context);
            e.UseMessageRetry(r => r.Intervals(100, 500, 1000));
        });
        cfg.ReceiveEndpoint("notification-task-assigned", e =>
        {
            e.Durable = true;
            e.ConfigureConsumer<TaskAssignedConsumer>(context);
            e.UseMessageRetry(r => r.Intervals(100, 500, 1000));
        });
        cfg.ReceiveEndpoint("notification-task-deleted", e =>
        {
            e.Durable = true;
            e.ConfigureConsumer<TaskDeletedConsumer>(context);
            e.UseMessageRetry(r => r.Intervals(100, 500, 1000));
        });
        cfg.ReceiveEndpoint("notification-project-member-added", e =>
        {
            e.Durable = true;
            e.ConfigureConsumer<ProjectMemberAddedConsumer>(context);
            e.UseMessageRetry(r => r.Intervals(100, 500, 1000));
        });
        cfg.ReceiveEndpoint("notification-complaint-created", e =>
        {
            e.Durable = true;
            e.ConfigureConsumer<ComplaintCreatedConsumer>(context);
            e.UseMessageRetry(r => r.Intervals(100, 500, 1000));
        });
        // DLQ: consume Fault messages from _error queues for alerting (CloudAMQP alarm on notification-task-*_error depth > 0)
        cfg.ReceiveEndpoint("notification-faults", e =>
        {
            e.Durable = true;
            e.ConfigureConsumer<TaskCreatedFaultConsumer>(context);
            e.ConfigureConsumer<TaskMovedFaultConsumer>(context);
            e.ConfigureConsumer<TaskCommentedFaultConsumer>(context);
        });
        cfg.ConfigureEndpoints(context);
    });
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "FlowBoard Notification.Service", Version = "v1", Description = "SignalR 10.0 Hub + MassTransit consumers + GET /api/notifications" });
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
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.SetIsOriginAllowed(origin => origin == "http://localhost:4200" || origin == "https://flowboard.vercel.app" || origin == "https://flow-board-seven-gilt.vercel.app" || (origin != null && origin.EndsWith(".vercel.app"))).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    try { await db.Database.MigrateAsync(); Log.Logger.Information("Notification DB migrated"); }
    catch (Exception ex) { Log.Logger.Warning(ex, "Notification DB migrate failed"); }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification.Service v1"));
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
app.UseMiddleware<Notification.Service.Middleware.CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseMiddleware<Notification.Service.Middleware.MaintenanceMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "FlowBoard Notification.Service", version = "v1.2", dotnet = "10.0", status = "Running", swagger = "/swagger", hub = "/hubs/board" }));
app.MapGet("/health/ready", () => Results.Ok(new { service = "Notification.Service", status = "Ready", timestamp = DateTime.UtcNow }));
app.MapControllers();
app.MapHub<BoardHub>("/hubs/board");
app.Run();
public partial class Program { }
