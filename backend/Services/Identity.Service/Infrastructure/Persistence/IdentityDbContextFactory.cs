using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Identity.Service.Infrastructure.Persistence;

// Single top comment: This factory is ONLY for `dotnet ef` CLI at design time (migrations add/update) - when you run `dotnet ef migrations add` or `dotnet ef database update`, EF needs a DbContext instance but Program.cs isn't running. This factory tells EF how to create IdentityDbContext by reading appsettings.Development.json (Server=localhost;Database=flowboard) and configuring UseSqlServer with MigrationsHistoryTable in schema [identity]. At runtime (dotnet run), Program.cs creates DbContext via DI, not this factory.
public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Server=localhost;Database=flowboard;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseSqlServer(connectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "identity"));

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
