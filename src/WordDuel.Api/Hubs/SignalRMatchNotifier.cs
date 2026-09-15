using Microsoft.AspNetCore.SignalR;
using WordDuel.Api.Dtos;

namespace WordDuel.Api.Hubs;

public sealed class SignalRMatchNotifier : IMatchNotifier
{
    public const string MatchUpdatedEvent = "MatchUpdated";

    private readonly IHubContext<MatchHub> _hubContext;

    public SignalRMatchNotifier(IHubContext<MatchHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyMatchUpdatedAsync(Guid matchId, MatchSnapshotDto snapshot, CancellationToken cancellationToken = default)
    {
        // Broadcast the PUBLIC snapshot only — never includes any player's
        // rack letters, since a shared match group includes both players.
        return _hubContext.Clients.Group(MatchHub.GroupName(matchId))
            .SendAsync(MatchUpdatedEvent, snapshot, cancellationToken);
    }
}
