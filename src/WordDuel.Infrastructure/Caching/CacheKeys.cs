namespace WordDuel.Infrastructure.Caching;

public static class CacheKeys
{
    public static string MatchSnapshot(Guid matchId) => $"wordduel:match:{matchId}:snapshot";
}
