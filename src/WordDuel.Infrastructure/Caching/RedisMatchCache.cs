using StackExchange.Redis;

namespace WordDuel.Infrastructure.Caching;

public sealed class RedisMatchCache : IMatchCache
{
    private readonly IConnectionMultiplexer _multiplexer;

    public RedisMatchCache(IConnectionMultiplexer multiplexer)
    {
        _multiplexer = multiplexer;
    }

    private IDatabase Db => _multiplexer.GetDatabase();

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await Db.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        await Db.StringSetAsync(key, value, ttl);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await Db.KeyDeleteAsync(key);
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await Db.PingAsync();
            return latency >= TimeSpan.Zero;
        }
        catch
        {
            return false;
        }
    }
}
