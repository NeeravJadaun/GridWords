using WordDuel.Api.Dtos;

namespace WordDuel.Api.Services;

public interface IMatchService
{
    Task<CreateMatchResponse> CreateMatchAsync(string displayName, CancellationToken ct);

    Task<JoinMatchResponse> JoinMatchAsync(Guid matchId, string displayName, CancellationToken ct);

    Task<MatchSnapshotDto> GetMatchAsync(Guid matchId, Guid? requestingPlayerId, CancellationToken ct);

    Task<MoveHistoryResponse> GetMoveHistoryAsync(Guid matchId, CancellationToken ct);

    Task<MoveResultResponse> SubmitMoveAsync(
        Guid matchId, Guid playerId, SubmitMoveRequest request, string? idempotencyKey, CancellationToken ct);

    Task<MatchActionResponse> PassAsync(
        Guid matchId, Guid playerId, MatchVersionedActionRequest request, string? idempotencyKey, CancellationToken ct);

    Task<MatchActionResponse> ResignAsync(
        Guid matchId, Guid playerId, MatchVersionedActionRequest request, string? idempotencyKey, CancellationToken ct);
}
