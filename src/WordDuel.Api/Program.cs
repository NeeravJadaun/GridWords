using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using WordDuel.Api.Auth;
using WordDuel.Api.Hubs;
using WordDuel.Api.Middleware;
using WordDuel.Api.Services;
using WordDuel.Domain.WordList;
using WordDuel.Infrastructure;
using WordDuel.Infrastructure.Persistence;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException("Missing Jwt configuration section.");
    if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
    {
        throw new InvalidOperationException("Jwt:SigningKey must be configured and at least 32 bytes long.");
    }

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // Without this, the handler remaps short claim names (e.g. "sub")
            // to long legacy URIs, breaking ClaimsPrincipalExtensions lookups.
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            // SignalR sends the access token via query string (browsers cannot
            // set Authorization headers on WebSocket upgrade requests).
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddWordDuelInfrastructure(builder.Configuration);

    builder.Services.AddSingleton<WordListProvider>();
    builder.Services.AddSingleton<PlayerTokenService>();
    builder.Services.AddSingleton<IdempotencyStore>();
    builder.Services.AddScoped<IMatchService, MatchService>();
    builder.Services.AddScoped<IMatchNotifier, SignalRMatchNotifier>();

    builder.Services.AddSignalR();
    builder.Services.AddControllers();

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("WordDuelClient", policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
        });
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Word Duel API",
            Version = "v1",
            Description = "Educational portfolio project. All matches, players, and scores are synthetic."
        });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Player token issued by POST /api/matches or POST /api/matches/{id}/join. Example: Bearer {token}",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            }
        });
    });

    var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres");
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:Redis");

    builder.Services.AddHealthChecks()
        .AddNpgSql(postgresConnectionString, name: "postgres", tags: new[] { "ready" })
        .AddRedis(redisConnectionString, name: "redis", tags: new[] { "ready" });

    var app = builder.Build();

    await ApplyMigrationsWithRetryAsync(app);

    app.UseSerilogRequestLogging();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("WordDuelClient");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<MatchHub>("/hubs/match");

    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false // liveness only: process is up, no dependency checks
    });
    app.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Word Duel API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static async Task ApplyMigrationsWithRetryAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<WordDuelDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Migration attempt {Attempt}/{MaxAttempts} failed, retrying in 3s...", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program
{
}
