using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WordDuel.Infrastructure.Persistence;

/// <summary>
/// Used only by `dotnet ef migrations` tooling to build a DbContext without
/// needing to boot the full API host. Never used at application runtime.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<WordDuelDbContext>
{
    public WordDuelDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("WORDDUEL_MIGRATIONS_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=wordduel;Username=wordduel;Password=wordduel_dev_password";

        var optionsBuilder = new DbContextOptionsBuilder<WordDuelDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new WordDuelDbContext(optionsBuilder.Options);
    }
}
