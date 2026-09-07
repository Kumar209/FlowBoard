using MassTransit;
using Shared.Contracts.Events;

var builder = WebApplication.CreateBuilder(args);

// MassTransit 8.3 + CloudAMQP same key local/prod (consumer side, Task 3.2 will add consumers + SignalR Hub)
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? builder.Configuration["RabbitMQ__Host"] ?? "";
builder.Services.AddMassTransit(x =>
{
    // No consumers yet (Task 3.2 adds TaskCreated/Moved consumers); just topology setup for 3.1 verification
    x.UsingRabbitMq((context, cfg) =>
    {
        if (!string.IsNullOrWhiteSpace(rabbitHost) && !rabbitHost.Contains("PASTE_"))
            cfg.Host(new Uri(rabbitHost));
        else
            cfg.Host("rabbitmq://localhost");
        cfg.Message<TaskCreatedEvent>(c => c.SetEntityName("flowboard.events"));
        cfg.Message<TaskMovedEvent>(c => c.SetEntityName("flowboard.events"));
        cfg.Message<TaskCommentedEvent>(c => c.SetEntityName("flowboard.events"));
        cfg.UseMessageRetry(r => r.Immediate(3));
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
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "FlowBoard Notification.Service", version = "v1.2", dotnet = "10.0", status = "Running", swagger = "/swagger", hub = "/hubs/board" }));
app.MapGet("/health/ready", () => Results.Ok(new { service = "Notification.Service", status = "Ready", timestamp = DateTime.UtcNow }));
app.MapControllers();
app.Run();
