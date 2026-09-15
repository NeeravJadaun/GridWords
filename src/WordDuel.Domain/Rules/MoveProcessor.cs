using WordDuel.Domain.Board;
using WordDuel.Domain.Model;
using WordDuel.Domain.Tiles;
using WordDuel.Domain.WordList;

namespace WordDuel.Domain.Rules;

/// <summary>
/// Validates and scores a word placement against a board/rack snapshot.
/// Pure domain logic: no persistence, no I/O. Callers apply the returned
/// outcome to their own board/rack copies and persist the result.
/// </summary>
public static class MoveProcessor
{
    public const int FullRackBonusPoints = 20;
    public const int MinimumWordLength = 2;

    public static GameResult<PlacementOutcome> ProcessPlacement(
        GameBoard board,
        Rack rack,
        PlacementRequest request,
        bool isFirstMoveOfMatch,
        WordListProvider wordList)
    {
        if (request.Tiles.Count == 0)
        {
            return GameResult<PlacementOutcome>.Failure(
                GameErrorCode.NoTilesPlaced, "At least one tile must be placed.");
        }

        if (!BoardLayout.IsInBounds(request.StartRow, request.StartCol))
        {
            return GameResult<PlacementOutcome>.Failure(
                GameErrorCode.OutOfBounds, "Start cell is outside the 7x7 board.");
        }

        var (rowStep, colStep) = request.Direction == Direction.Across ? (0, 1) : (1, 0);

        var endRow = request.StartRow + rowStep * (request.Tiles.Count - 1);
        var endCol = request.StartCol + colStep * (request.Tiles.Count - 1);
        if (!BoardLayout.IsInBounds(endRow, endCol))
        {
            return GameResult<PlacementOutcome>.Failure(
                GameErrorCode.OutOfBounds, "Placement extends beyond the 7x7 board.");
        }

        var newlyPlaced = new List<PlacedTile>();
        for (var i = 0; i < request.Tiles.Count; i++)
        {
            var row = request.StartRow + rowStep * i;
            var col = request.StartCol + colStep * i;
            var requestedLetter = char.ToUpperInvariant(request.Tiles[i]);
            var existing = board.GetLetter(row, col);

            if (existing is not null)
            {
                if (existing.Value != requestedLetter)
                {
                    return GameResult<PlacementOutcome>.Failure(
                        GameErrorCode.OverlapConflict,
                        $"Cell ({row},{col}) already holds '{existing.Value}', which conflicts with the submitted letter '{requestedLetter}'.");
                }
            }
            else
            {
                newlyPlaced.Add(new PlacedTile(row, col, requestedLetter));
            }
        }

        if (newlyPlaced.Count == 0)
        {
            return GameResult<PlacementOutcome>.Failure(
                GameErrorCode.NoTilesPlaced, "This placement does not add any new tiles to the board.");
        }

        if (!rack.ContainsAll(newlyPlaced.Select(t => t.Letter)))
        {
            return GameResult<PlacementOutcome>.Failure(
                GameErrorCode.TilesNotInRack, "One or more placed tiles are not available in the player's rack.");
        }

        if (isFirstMoveOfMatch)
        {
            var coversStart = newlyPlaced.Any(t => BoardLayout.IsCenter(t.Row, t.Col));
            if (!coversStart)
            {
                return GameResult<PlacementOutcome>.Failure(
                    GameErrorCode.FirstMoveMustCoverStart,
                    "The first move of a match must cover the center start square.");
            }
        }
        else
        {
            var touchesExisting = newlyPlaced.Any(t => HasAdjacentExistingTile(board, t.Row, t.Col));
            if (!touchesExisting)
            {
                return GameResult<PlacementOutcome>.Failure(
                    GameErrorCode.NotConnected,
                    "This placement must connect to at least one tile already on the board.");
            }
        }

        var working = board.Clone();
        foreach (var tile in newlyPlaced)
        {
            working.SetLetter(tile.Row, tile.Col, tile.Letter);
        }

        var newlyPlacedSet = newlyPlaced.Select(t => (t.Row, t.Col)).ToHashSet();

        var mainWordCells = FindWordCells(working, request.StartRow, request.StartCol, rowStep, colStep);
        var allWordCellRuns = new List<IReadOnlyList<(int Row, int Col)>>();

        if (mainWordCells.Count >= MinimumWordLength)
        {
            allWordCellRuns.Add(mainWordCells);
        }

        var (perpRowStep, perpColStep) = (colStep, rowStep);
        foreach (var tile in newlyPlaced)
        {
            var crossCells = FindWordCells(working, tile.Row, tile.Col, perpRowStep, perpColStep);
            if (crossCells.Count >= MinimumWordLength)
            {
                allWordCellRuns.Add(crossCells);
            }
        }

        if (allWordCellRuns.Count == 0)
        {
            return GameResult<PlacementOutcome>.Failure(
                GameErrorCode.NoWordFormed, "This placement does not form a word of at least two letters.");
        }

        var formedWords = new List<FormedWord>();
        foreach (var cells in allWordCellRuns)
        {
            var word = new string(cells.Select(c => working.GetLetter(c.Row, c.Col)!.Value).ToArray());
            if (!wordList.IsValidWord(word))
            {
                return GameResult<PlacementOutcome>.Failure(
                    GameErrorCode.WordNotInDictionary, $"\"{word}\" is not in the Word Duel dictionary.");
            }

            var points = ScoreCalculator.ScoreWord(working, cells, newlyPlacedSet);
            formedWords.Add(new FormedWord(word, points, cells.Select(c => new PlacedTile(c.Row, c.Col, working.GetLetter(c.Row, c.Col)!.Value)).ToList()));
        }

        var usedFullRack = newlyPlaced.Count == Rack.Capacity;
        var totalScore = formedWords.Sum(w => w.Points) + (usedFullRack ? FullRackBonusPoints : 0);

        var outcome = new PlacementOutcome(totalScore, formedWords, newlyPlaced, usedFullRack);
        return GameResult<PlacementOutcome>.Success(outcome);
    }

    private static bool HasAdjacentExistingTile(GameBoard originalBoard, int row, int col)
    {
        (int dr, int dc)[] deltas = { (-1, 0), (1, 0), (0, -1), (0, 1) };
        foreach (var (dr, dc) in deltas)
        {
            var nr = row + dr;
            var nc = col + dc;
            if (BoardLayout.IsInBounds(nr, nc) && !originalBoard.IsEmpty(nr, nc))
            {
                return true;
            }
        }

        return false;
    }

    private static List<(int Row, int Col)> FindWordCells(GameBoard working, int anchorRow, int anchorCol, int stepR, int stepC)
    {
        var backR = anchorRow;
        var backC = anchorCol;
        while (BoardLayout.IsInBounds(backR - stepR, backC - stepC) && !working.IsEmpty(backR - stepR, backC - stepC))
        {
            backR -= stepR;
            backC -= stepC;
        }

        var cells = new List<(int Row, int Col)>();
        var r = backR;
        var c = backC;
        while (BoardLayout.IsInBounds(r, c) && !working.IsEmpty(r, c))
        {
            cells.Add((r, c));
            r += stepR;
            c += stepC;
        }

        return cells;
    }
}
