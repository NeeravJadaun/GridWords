namespace WordDuel.Domain.Board;

/// <summary>
/// Mutable representation of the letters currently placed on the 7x7 grid.
/// Null cell means empty. Pure in-memory game state — no persistence concerns.
/// </summary>
public sealed class GameBoard
{
    private readonly char?[,] _cells;

    public GameBoard()
    {
        _cells = new char?[BoardLayout.Size, BoardLayout.Size];
    }

    private GameBoard(char?[,] cells)
    {
        _cells = cells;
    }

    public char? GetLetter(int row, int col)
    {
        if (!BoardLayout.IsInBounds(row, col))
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }

        return _cells[row, col];
    }

    public bool IsEmpty(int row, int col) => GetLetter(row, col) is null;

    public bool HasAnyTiles()
    {
        for (var r = 0; r < BoardLayout.Size; r++)
            for (var c = 0; c < BoardLayout.Size; c++)
            {
                if (_cells[r, c] is not null)
                {
                    return true;
                }
            }

        return false;
    }

    public void SetLetter(int row, int col, char letter)
    {
        if (!BoardLayout.IsInBounds(row, col))
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }

        _cells[row, col] = char.ToUpperInvariant(letter);
    }

    public GameBoard Clone()
    {
        var copy = new char?[BoardLayout.Size, BoardLayout.Size];
        Array.Copy(_cells, copy, _cells.Length);
        return new GameBoard(copy);
    }

    /// <summary>
    /// Serializes the board to a flat 49-character string, row-major, using
    /// '.' for empty cells. Used for compact persistence/snapshotting.
    /// </summary>
    public string ToFlatString()
    {
        Span<char> buffer = stackalloc char[BoardLayout.Size * BoardLayout.Size];
        var i = 0;
        for (var r = 0; r < BoardLayout.Size; r++)
            for (var c = 0; c < BoardLayout.Size; c++)
            {
                buffer[i++] = _cells[r, c] ?? '.';
            }

        return new string(buffer);
    }

    public static GameBoard FromFlatString(string flat)
    {
        if (flat.Length != BoardLayout.Size * BoardLayout.Size)
        {
            throw new ArgumentException($"Expected {BoardLayout.Size * BoardLayout.Size} characters.", nameof(flat));
        }

        var cells = new char?[BoardLayout.Size, BoardLayout.Size];
        var i = 0;
        for (var r = 0; r < BoardLayout.Size; r++)
            for (var c = 0; c < BoardLayout.Size; c++)
            {
                var ch = flat[i++];
                cells[r, c] = ch == '.' ? null : ch;
            }

        return new GameBoard(cells);
    }
}
