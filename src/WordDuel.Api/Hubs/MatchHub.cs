using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WordDuel.Api.Auth;

namespace WordDuel.Api.Hubs;

/// <summary>
/// Authenticated real-time channel for match updates. Each connection's JWT
/// (issued on create/join) determines which match group it is placed in —
/// clients cannot join a group for a match they are not a player in.
/// </summary>
[Authorize]
public sealed class MatchHub : Hub
{
    public static string GroupName(Guid matchId) => $"match:{matchId}";

    public override async Task OnConnectedAsync()
    {
        var matchId = Context.User?.GetMatchId();
        if (matchId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(matchId.Value));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var matchId = Context.User?.GetMatchId();
        if (matchId is not null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(matchId.Value));
        }

        await base.OnDisconnectedAsync(exception);
    }
}
