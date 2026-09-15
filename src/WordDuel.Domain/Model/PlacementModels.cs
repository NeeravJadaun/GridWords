namespace WordDuel.Domain.Model;

/// <summary>One tile sitting on a specific board cell.</summary>
public readonly record struct PlacedTile(int Row, int Col, char Letter);

/// <summary>
/// A player's requested move: the cell to start from, the direction to lay
/// tiles along, and the full ordered sequence of letters for that run —
/// including letters over cells that are already occupied on the board.
/// The engine checks each occupied cell's letter against the request
/// (mismatch = overlap conflict) and treats each empty cell's letter as a
/// new tile drawn from the player's rack.
/// </summary>
public sealed record PlacementRequest(int StartRow, int StartCol, Direction Direction, IReadOnlyList<char> Tiles);

/// <summary>A dictionary word formed (fully or partly) by this move.</summary>
public sealed record FormedWord(string Word, int Points, IReadOnlyList<PlacedTile> Cells);

/// <summary>The validated, scored result of applying a placement.</summary>
public sealed record PlacementOutcome(
    int TotalScore,
    IReadOnlyList<FormedWord> WordsFormed,
    IReadOnlyList<PlacedTile> NewlyPlacedTiles,
    bool UsedFullRackBonus);
