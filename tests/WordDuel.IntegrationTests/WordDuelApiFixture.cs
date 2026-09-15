using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace WordDuel.IntegrationTests;

/// <summary>
/// Boots real Postgres and Redis containers (Testcontainers) and the API
/// in-process (WebApplicationFactory / TestServer) against them. Shared once
/// across the whole integration test run via the "WordDuelApi" collection.
/// </summary>
public sealed class WordDuelApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("wordduel_test")
        .WithUsername("wordduel_test")
        .WithPassword("wordduel_test_password")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public const string TestSigningKey = "integration-test-signing-key-at-least-32-bytes!";

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        // Program.cs reads configuration (Jwt signing key, connection strings)
        // between WebApplication.CreateBuilder(args) and Build() — earlier than
        // WebApplicationFactory's ConfigureWebHost overrides become visible for
        // a minimal-hosting entry point. Environment variables, by contrast,
        // are picked up immediately by CreateBuilder's default config sources,
        // so set them before anything calls CreateClient()/builds the host.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", _redis.GetConnectionString());
        Environment.SetEnvironmentVariable("Jwt__SigningKey", TestSigningKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "worddue-api-tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "worddue-client-tests");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<Microsoft.Extensions.Logging.LoggerFilterOptions>(options =>
                options.MinLevel = Microsoft.Extensions.Logging.LogLevel.Warning);
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition("WordDuelApi")]
public sealed class WordDuelApiCollection : ICollectionFixture<WordDuelApiFixture>
{
}
