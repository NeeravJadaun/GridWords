namespace WordDuel.Domain.Model;

/// <summary>
/// Lightweight result type for expected domain-rule failures (invalid
/// placements, turn violations, etc.). Avoids using exceptions for control
/// flow on the hot path of move validation.
/// </summary>
public sealed class GameResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public GameErrorCode ErrorCode { get; }
    public string? ErrorMessage { get; }

    private GameResult(bool isSuccess, T? value, GameErrorCode errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static GameResult<T> Success(T value) => new(true, value, GameErrorCode.None, null);

    public static GameResult<T> Failure(GameErrorCode code, string message) =>
        new(false, default, code, message);
}
