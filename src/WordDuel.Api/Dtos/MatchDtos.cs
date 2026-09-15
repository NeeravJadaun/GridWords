using System.ComponentModel.DataAnnotations;

namespace WordDuel.Api.Dtos;

// ---- Requests ----

public sealed class CreateMatchRequest
{
    [Required, MinLength(1), MaxLength(40)]
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class JoinMatchRequest
{
    [Required, MinLength(1), MaxLength(40)]
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class SubmitMoveRequest
{
    [Required]
    public int ExpectedMatchVersion { get; set; }

    [Range(0, 6)]
    public int StartRow { get; set; }

    [Range(0, 6)]
    public int StartCol { get; set; }

    /// <summary>"Across" or "Down".</summary>
    [Required]
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// The full word run as it will read after this move, including letters
    /// over cells that are already occupied on the board.
    /// </summary>
    [Required, RegularExpression("^[A-Za-z]{1,7}$", ErrorMessage = "Tiles must be 1-7 letters (A-Z).")]
    public string Tiles { get; set; } = string.Empty;
}

public sealed class MatchVersionedActionRequest
{
    [Required]
    public int ExpectedMatchVersion { get; set; }
}

// ---- Responses ----

public sealed class PlayerPublicDto
{
    public Guid PlayerId { get; set; }
    public int Seat { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int Score { get; set; }
    public int RackTileCount { get; set; }
    public bool HasResigned { get; set; }
}

public sealed class MatchSnapshotDto
{
    public Guid MatchId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Board { get; set; } = string.Empty; // 49-char flat string, '.' = empty
    public List<PlayerPublicDto> Players { get; set; } = new();
    public int CurrentTurnSeat { get; set; }
    public int ConsecutivePasses { get; set; }
    public string EndReason { get; set; } = "None";
    public int? WinnerSeat { get; set; }
    public int MoveCount { get; set; }
    public int TilesRemainingInBag { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>Populated only when the requester authenticates as a player in this match.</summary>
    public string? YourRack { get; set; }
    public int? YourSeat { get; set; }
    public bool? IsYourTurn { get; set; }
}

public sealed class CreateMatchResponse
{
    public Guid MatchId { get; set; }
    public Guid PlayerId { get; set; }
    public int Seat { get; set; }
    public string PlayerToken { get; set; } = string.Empty;
    public string Rack { get; set; } = string.Empty;
    public MatchSnapshotDto Match { get; set; } = null!;
}

public sealed class JoinMatchResponse
{
    public Guid PlayerId { get; set; }
    public int Seat { get; set; }
    public string PlayerToken { get; set; } = string.Empty;
    public string Rack { get; set; } = string.Empty;
    public MatchSnapshotDto Match { get; set; } = null!;
}

public sealed class FormedWordDto
{
    public string Word { get; set; } = string.Empty;
    public int Points { get; set; }
}

public sealed class MoveResultResponse
{
    public MatchSnapshotDto Match { get; set; } = null!;
    public List<FormedWordDto> WordsFormed { get; set; } = new();
    public int PointsScored { get; set; }
    public bool UsedFullRackBonus { get; set; }
    public string YourRack { get; set; } = string.Empty;
}

public sealed class MatchActionResponse
{
    public MatchSnapshotDto Match { get; set; } = null!;
}

public sealed class MoveHistoryItemDto
{
    public Guid MoveId { get; set; }
    public int SequenceNumber { get; set; }
    public int Seat { get; set; }
    public string PlayerDisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? StartRow { get; set; }
    public int? StartCol { get; set; }
    public string? Direction { get; set; }
    public string? TilesSubmitted { get; set; }
    public List<FormedWordDto> WordsFormed { get; set; } = new();
    public int PointsScored { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class MoveHistoryResponse
{
    public List<MoveHistoryItemDto> Moves { get; set; } = new();
}
