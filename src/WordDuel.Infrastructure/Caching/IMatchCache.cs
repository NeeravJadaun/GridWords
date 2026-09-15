namespace WordDuel.Infrastructure.Caching;

/// <summary>
/// Thin, string-based Redis cache abstraction used for match snapshot
/// caching. Kept generic (raw strings) so this layer has no dependency on
/// the API project's DTO types — callers own their own serialization.
/// </summary>
public interface IMatchCache
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>True if the underlying Redis connection is reachable (used by /ready).</summary>
    Task<bool> PingAsync(CancellationToken cancellationToken = default);
}
