using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Project.Service.Application.Behaviors;
using Project.Service.Application.Interfaces;
using Project.Service.Infrastructure.Caching;
using Project.Service.Infrastructure.Persistence;

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
