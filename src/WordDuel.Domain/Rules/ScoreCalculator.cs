using WordDuel.Domain.Board;
using WordDuel.Domain.Model;
using WordDuel.Domain.Tiles;

namespace WordDuel.Domain.Rules;

/// <summary>
/// Computes the deterministic point score for a formed word, applying
/// letter/word bonus squares only to cells that were newly placed in the
/// current move (bonus squares do not re-trigger on later moves).
/// </summary>
public static class ScoreCalculator
{
    public static int ScoreWord(GameBoard working, IReadOnlyList<(int Row, int Col)> cells, HashSet<(int Row, int Col)> newlyPlaced)
    {
        var letterSum = 0;
        var wordMultiplier = 1;

        foreach (var (row, col) in cells)
        {
            var letter = working.GetLetter(row, col)
                ?? throw new InvalidOperationException("Word cell must be occupied.");
            var basePoints = LetterValues.PointsFor(letter);

            if (newlyPlaced.Contains((row, col)))
            {
                var bonus = BoardLayout.GetBonus(row, col);
                basePoints *= LetterMultiplier(bonus);
                wordMultiplier *= WordMultiplier(bonus);
            }

            letterSum += basePoints;
        }

        return letterSum * wordMultiplier;
    }

    private static int LetterMultiplier(BonusType bonus) => bonus switch
    {
        BonusType.DoubleLetter => 2,
        BonusType.TripleLetter => 3,
        _ => 1
    };

    private static int WordMultiplier(BonusType bonus) => bonus switch
    {
        BonusType.DoubleWord => 2,
        BonusType.TripleWord => 3,
        BonusType.Start => 2,
        _ => 1
    };
}
