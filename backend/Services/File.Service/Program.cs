using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using File.Service.Application.Interfaces;
using File.Service.Infrastructure.Persistence;
using File.Service.Infrastructure.Services;
using File.Service.Infrastructure.Messaging;
using Shared.Contracts.Events;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("Default") ?? "Server=localhost;Database=flowboard;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
builder.Services.AddDbContext<FileDbContext>(o => o.UseSqlServer(cs, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "file")));
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<FileDbContext>());

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// MassTransit 8.3 + CloudAMQP (same amqps:// key local/prod, 2s Outbox poll, durable quorum)
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? builder.Configuration["RabbitMQ__Host"] ?? "";
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        if (!string.IsNullOrWhiteSpace(rabbitHost) && !rabbitHost.Contains("PASTE_"))
            cfg.Host(new Uri(rabbitHost));
        else
            cfg.Host("rabbitmq://localhost");
        cfg.Message<FileUploadedEvent>(c => c.SetEntityName("flowboard.events"));
        cfg.Publish<FileUploadedEvent>(c => c.ExchangeType = "fanout");
        cfg.UseMessageRetry(r => r.Immediate(3));
        cfg.ConfigureEndpoints(context);
    });
});
builder.Services.AddHostedService<OutboxBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "FlowBoard File.Service", Version = "v1", Description = "Cloudinary attachments - POST /api/files/upload + GET /api/tasks/{id}/attachments + DELETE /api/files/{id} - Task 4.1 with strict org/workspace/project permission checks" });
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
    });
builder.Services.AddAuthorization();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "File.Service v1"));
}
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "FlowBoard File.Service", version = "v1.2", dotnet = "10.0", status = "Running", swagger = "/swagger" }));
app.MapGet("/health/ready", () => Results.Ok(new { service = "File.Service", status = "Ready", timestamp = DateTime.UtcNow }));
app.MapControllers();
app.Run();
public partial class Program { }
