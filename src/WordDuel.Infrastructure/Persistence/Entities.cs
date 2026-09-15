namespace WordDuel.Infrastructure.Persistence;

public class MatchEntity
{
    public Guid Id { get; set; }
    public int Status { get; set; } // WordDuel.Domain.Model.MatchStatus
    public int BoardSeed { get; set; }
    public string BoardStateFlat { get; set; } = new string('.', 49);
    public int TilesDrawnCount { get; set; }
    public int CurrentTurnSeat { get; set; }
    public int ConsecutivePasses { get; set; }
    public int EndReason { get; set; } // WordDuel.Domain.Model.MatchEndReason
    public int? WinnerSeat { get; set; }
    public int MoveSequenceCounter { get; set; }

    /// <summary>Optimistic concurrency token, also exposed to API clients as the match version.</summary>
    public int Version { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }

    public List<PlayerEntity> Players { get; set; } = new();
    public List<MoveEntity> Moves { get; set; } = new();
}

public class PlayerEntity
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public MatchEntity Match { get; set; } = null!;

    public int Seat { get; set; } // 0 or 1
    public string DisplayName { get; set; } = string.Empty;
    public string RackFlat { get; set; } = string.Empty; // concatenated upper-case letters
    public int Score { get; set; }
    public bool HasResigned { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
}

public class MoveEntity
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public MatchEntity Match { get; set; } = null!;

    public Guid PlayerId { get; set; }
    public int Seat { get; set; }
    public int SequenceNumber { get; set; }
    public int Type { get; set; } // WordDuel.Domain.Model.MoveType

    public int? StartRow { get; set; }
    public int? StartCol { get; set; }
    public int? Direction { get; set; } // WordDuel.Domain.Model.Direction
    public string? TilesSubmitted { get; set; }
    public string? WordsFormedJson { get; set; }
    public int PointsScored { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public class IdempotencyRecordEntity
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public Guid PlayerId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public string ResponseBodyJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
