using WordDuel.Api.Dtos;
using WordDuel.Domain.Model;
using WordDuel.Domain.Tiles;
using WordDuel.Infrastructure.Persistence;

namespace WordDuel.Api.Services;

public static class MatchSnapshotMapper
{
    public static MatchSnapshotDto ToSnapshot(MatchEntity match, int moveCount, Guid? requestingPlayerId = null)
    {
        var tilesDrawnSoFar = match.TilesDrawnCount;
        var tilesRemaining = Math.Max(0, LetterValues.TotalTileCount - tilesDrawnSoFar);

        var dto = new MatchSnapshotDto
        {
            MatchId = match.Id,
            Status = ((MatchStatus)match.Status).ToString(),
            Version = match.Version,
            Board = match.BoardStateFlat,
            Players = match.Players
                .OrderBy(p => p.Seat)
                .Select(p => new PlayerPublicDto
                {
                    PlayerId = p.Id,
                    Seat = p.Seat,
                    DisplayName = p.DisplayName,
                    Score = p.Score,
                    RackTileCount = p.RackFlat.Length,
                    HasResigned = p.HasResigned
                })
                .ToList(),
            CurrentTurnSeat = match.CurrentTurnSeat,
            ConsecutivePasses = match.ConsecutivePasses,
            EndReason = ((MatchEndReason)match.EndReason).ToString(),
            WinnerSeat = match.WinnerSeat,
            MoveCount = moveCount,
            TilesRemainingInBag = tilesRemaining,
            CreatedAt = match.CreatedAt,
            StartedAt = match.StartedAt,
            FinishedAt = match.FinishedAt
        };

        if (requestingPlayerId is not null)
        {
            var self = match.Players.FirstOrDefault(p => p.Id == requestingPlayerId.Value);
            if (self is not null)
            {
                dto.YourRack = self.RackFlat;
                dto.YourSeat = self.Seat;
                dto.IsYourTurn = match.Status == (int)MatchStatus.InProgress && match.CurrentTurnSeat == self.Seat;
            }
        }

        return dto;
    }
}
