namespace WordDuel.Domain.Tiles;

/// <summary>
/// Original Word Duel letter distribution and point values (not copied from
/// any commercial word game's tile distribution).
/// </summary>
public static class LetterValues
{
    private static readonly (char Letter, int Count, int Points)[] Distribution =
    {
        ('A', 7, 1),
        ('B', 2, 4),
        ('C', 2, 4),
        ('D', 3, 2),
        ('E', 10, 1),
        ('F', 2, 4),
        ('G', 2, 3),
        ('H', 2, 4),
        ('I', 7, 1),
        ('J', 1, 9),
        ('K', 1, 6),
        ('L', 3, 2),
        ('M', 2, 4),
        ('N', 5, 2),
        ('O', 6, 1),
        ('P', 2, 4),
        ('Q', 1, 9),
        ('R', 5, 1),
        ('S', 4, 1),
        ('T', 5, 1),
        ('U', 3, 2),
        ('V', 1, 5),
        ('W', 2, 4),
        ('X', 1, 7),
        ('Y', 2, 4),
        ('Z', 1, 9)
    };

    public static readonly IReadOnlyDictionary<char, int> Points =
        Distribution.ToDictionary(d => d.Letter, d => d.Points);

    public static int PointsFor(char letter) => Points[char.ToUpperInvariant(letter)];

    /// <summary>
    /// Builds the full ordered tile bag contents (unshuffled) — one entry per
    /// physical tile, per the fixed Word Duel letter distribution.
    /// </summary>
    public static IReadOnlyList<char> BuildFullTileMultiset()
    {
        var tiles = new List<char>();
        foreach (var (letter, count, _) in Distribution)
        {
            for (var i = 0; i < count; i++)
            {
                tiles.Add(letter);
            }
        }

        return tiles;
    }

    public static int TotalTileCount => Distribution.Sum(d => d.Count);
}
