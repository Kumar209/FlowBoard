using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Notification.Service.Consumers;
using Notification.Service.Hubs;
using Notification.Service.Infrastructure.Persistence;
using Shared.Contracts.Events;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("Default") ?? "Server=localhost;Database=flowboard;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
builder.Services.AddDbContext<NotificationDbContext>(o => o.UseSqlServer(cs, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "notification")));
builder.Services.AddScoped<Notification.Service.Application.Interfaces.IApplicationDbContext>(sp => sp.GetRequiredService<NotificationDbContext>());
builder.Services.AddScoped<Notification.Service.Application.Interfaces.INotificationService, Notification.Service.Infrastructure.Services.NotificationService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

var jwtKey = builder.Configuration["Jwt:Key"] ?? "PASTE_SUPER_SECRET_32_CHARS_MINIMUM_FOR_HS256";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FlowBoard.Identity";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FlowBoard.Gateway";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer, ValidAudience = jwtAudience, IssuerSigningKey = new SymmetricSecurityKey(keyBytes), ClockSkew = TimeSpan.Zero
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

// SignalR 10.0 + Upstash Redis backplane (same rediss:// key local/prod) — best-effort with AbortOnConnectFail=false
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

// MassTransit 8.3 + CloudAMQP same key local/prod + consumers (Task 3.2) + retry 3x + _error + quorum durable
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? builder.Configuration["RabbitMQ__Host"] ?? "";
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TaskCreatedConsumer>();
    x.AddConsumer<TaskMovedConsumer>();
    x.AddConsumer<TaskCommentedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        if (!string.IsNullOrWhiteSpace(rabbitHost) && !rabbitHost.Contains("PASTE_"))
            cfg.Host(new Uri(rabbitHost));
        else
            cfg.Host("rabbitmq://localhost");
        cfg.Message<TaskCreatedEvent>(c => c.SetEntityName("flowboard.events"));
        cfg.Message<TaskMovedEvent>(c => c.SetEntityName("flowboard.events"));
        cfg.Message<TaskCommentedEvent>(c => c.SetEntityName("flowboard.events"));
        cfg.Publish<TaskCreatedEvent>(c => c.ExchangeType = "fanout");
        cfg.Publish<TaskMovedEvent>(c => c.ExchangeType = "fanout");
        cfg.Publish<TaskCommentedEvent>(c => c.ExchangeType = "fanout");
        cfg.UseMessageRetry(r => r.Immediate(3));
        // Quorum durable queues per consumer (MNC-grade) — Durable must be set before ConfigureConsumer
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
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins("http://localhost:4200","https://flowboard.vercel.app").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification.Service v1"));
}
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
