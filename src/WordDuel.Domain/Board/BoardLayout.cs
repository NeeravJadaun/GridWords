using WordDuel.Domain.Model;

namespace WordDuel.Domain.Board;

/// <summary>
/// Fixed, deterministic 7x7 bonus-square layout for Word Duel. This is an
/// original, symmetric pattern designed for this project — not copied from
/// any commercial word game.
/// </summary>
public static class BoardLayout
{
    public const int Size = 7;
    public const int CenterRow = 3;
    public const int CenterCol = 3;

    private static readonly BonusType[,] Bonuses = BuildBonusGrid();

    public static BonusType GetBonus(int row, int col)
    {
        if (!IsInBounds(row, col))
        {
            throw new ArgumentOutOfRangeException(nameof(row), "Cell is outside the 7x7 board.");
        }

        return Bonuses[row, col];
    }

    public static bool IsInBounds(int row, int col) =>
        row >= 0 && row < Size && col >= 0 && col < Size;

    public static bool IsCenter(int row, int col) => row == CenterRow && col == CenterCol;

    private static BonusType[,] BuildBonusGrid()
    {
        // T = Triple Word, D = Double Word, t = Triple Letter, d = Double Letter,
        // S = Start (center, also acts as Double Word), . = plain
        string[] pattern =
        {
            "T..d..T",
            ".D...D.",
            "..t.t..",
            "d..S..d",
            "..t.t..",
            ".D...D.",
            "T..d..T"
        };

        var grid = new BonusType[Size, Size];
        for (var row = 0; row < Size; row++)
        {
            for (var col = 0; col < Size; col++)
            {
                grid[row, col] = pattern[row][col] switch
                {
                    'T' => BonusType.TripleWord,
                    'D' => BonusType.DoubleWord,
                    't' => BonusType.TripleLetter,
                    'd' => BonusType.DoubleLetter,
                    'S' => BonusType.Start,
                    _ => BonusType.None
                };
            }
        }

        return grid;
    }
}
