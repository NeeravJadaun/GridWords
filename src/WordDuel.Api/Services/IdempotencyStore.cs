using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WordDuel.Infrastructure.Persistence;

namespace WordDuel.Api.Services;

/// <summary>
/// Caches successful mutating-endpoint responses so a retried request with
/// the same idempotency key replays the original result instead of
/// re-applying (and re-scoring) the action. Failed requests are not cached:
/// since they never mutated state, retrying them is naturally safe.
/// </summary>
public sealed class IdempotencyStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> TryGetCachedResponseAsync<T>(
        WordDuelDbContext db, Guid matchId, Guid playerId, string endpoint, string idempotencyKey, CancellationToken ct)
    {
        var record = await db.IdempotencyRecords.AsNoTracking().FirstOrDefaultAsync(
            r => r.MatchId == matchId && r.PlayerId == playerId && r.Endpoint == endpoint && r.IdempotencyKey == idempotencyKey,
            ct);

        return record is null ? default : JsonSerializer.Deserialize<T>(record.ResponseBodyJson, JsonOptions);
    }

    public void RecordResponse<T>(
        WordDuelDbContext db, Guid matchId, Guid playerId, string endpoint, string idempotencyKey, T response)
    {
        db.IdempotencyRecords.Add(new IdempotencyRecordEntity
        {
            Id = Guid.NewGuid(),
            MatchId = matchId,
            PlayerId = playerId,
            Endpoint = endpoint,
            IdempotencyKey = idempotencyKey,
            ResponseStatusCode = 200,
            ResponseBodyJson = JsonSerializer.Serialize(response, JsonOptions),
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
