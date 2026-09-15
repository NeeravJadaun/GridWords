namespace WordDuel.Domain.Tiles;

/// <summary>
/// A deterministic, seeded tile bag. Given the same seed, the shuffle order
/// and every subsequent draw sequence is fully reproducible — this is what
/// makes match setup "deterministic" (see spec) while still varying per
/// match, since each match gets a fresh seed derived from its match ID.
/// </summary>
public sealed class TileBag
{
    private readonly List<char> _remaining;

    public int Seed { get; }

    public TileBag(int seed)
    {
        Seed = seed;
        _remaining = LetterValues.BuildFullTileMultiset().ToList();
        Shuffle(_remaining, seed);
    }

    private TileBag(int seed, List<char> remaining)
    {
        Seed = seed;
        _remaining = remaining;
    }

    public int RemainingCount => _remaining.Count;

    public IReadOnlyList<char> DrawUpTo(int count)
    {
        var drawCount = Math.Min(count, _remaining.Count);
        var drawn = _remaining.GetRange(0, drawCount);
        _remaining.RemoveRange(0, drawCount);
        return drawn;
    }

    public TileBag Clone() => new(Seed, new List<char>(_remaining));

    /// <summary>
    /// Deterministically rebuilds bag state by replaying: full shuffle,
    /// then removing the tiles already known to have been drawn (in draw
    /// order). Used to reconstruct bag state from persisted history.
    /// </summary>
    public static TileBag Reconstruct(int seed, int tilesAlreadyDrawn)
    {
        var bag = new TileBag(seed);
        bag.DrawUpTo(tilesAlreadyDrawn);
        return bag;
    }

    private static void Shuffle(IList<char> list, int seed)
    {
        var rng = new Random(seed);
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
