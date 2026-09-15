using Microsoft.AspNetCore.Mvc;
using WordDuel.Api.Errors;

namespace WordDuel.Api.Middleware;

/// <summary>
/// Converts <see cref="GameRuleException"/> into a consistent ProblemDetails
/// response carrying a stable, machine-readable error code, plus the
/// current match version when relevant (so clients can refresh on 409s).
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (GameRuleException ex)
        {
            var statusCode = GameErrorCodeHttpMapper.ToHttpStatusCode(ex.ErrorCode);
            var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var cid) ? cid?.ToString() : null;

            _logger.LogInformation(
                "Game rule rejection {ErrorCode} on {Path}: {Message}",
                ex.ErrorCode, context.Request.Path, ex.Message);

            var problem = new ProblemDetails
            {
                Title = "Request could not be completed.",
                Detail = ex.Message,
                Status = statusCode,
                Type = $"https://worddue-api.local/errors/{ex.ErrorCode}",
                Instance = context.Request.Path
            };
            problem.Extensions["errorCode"] = ex.ErrorCode.ToString();
            problem.Extensions["correlationId"] = correlationId;
            if (ex.CurrentMatchVersion is not null)
            {
                problem.Extensions["currentMatchVersion"] = ex.CurrentMatchVersion;
            }

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
