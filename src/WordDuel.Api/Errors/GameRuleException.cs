using WordDuel.Domain.Model;

namespace WordDuel.Api.Errors;

/// <summary>
/// Thrown by application services when a request is well-formed but
/// violates a game or match-lifecycle rule. Caught centrally and mapped to
/// a ProblemDetails response with a stable, machine-readable error code.
/// </summary>
public sealed class GameRuleException : Exception
{
    public GameErrorCode ErrorCode { get; }
    public int? CurrentMatchVersion { get; }

    public GameRuleException(GameErrorCode errorCode, string message, int? currentMatchVersion = null)
        : base(message)
    {
        ErrorCode = errorCode;
        CurrentMatchVersion = currentMatchVersion;
    }
}
