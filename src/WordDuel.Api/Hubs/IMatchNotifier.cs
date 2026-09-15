using WordDuel.Api.Dtos;

namespace WordDuel.Api.Hubs;

public interface IMatchNotifier
{
    Task NotifyMatchUpdatedAsync(Guid matchId, MatchSnapshotDto snapshot, CancellationToken cancellationToken = default);
}
