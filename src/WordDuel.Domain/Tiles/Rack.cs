namespace WordDuel.Domain.Tiles;

/// <summary>
/// A player's hand of tiles. Order is not meaningful for gameplay, only
/// membership and counts.
/// </summary>
public sealed class Rack
{
    private readonly List<char> _tiles;

    public const int Capacity = 7;

    public Rack(IEnumerable<char> tiles)
    {
        _tiles = tiles.Select(char.ToUpperInvariant).ToList();
    }

    public IReadOnlyList<char> Tiles => _tiles;

    public int Count => _tiles.Count;

    /// <summary>
    /// Checks whether this rack contains at least the given multiset of
    /// letters (each requested letter must be available as a distinct tile).
    /// </summary>
    public bool ContainsAll(IEnumerable<char> letters)
    {
        var pool = _tiles.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());
        foreach (var letter in letters.Select(char.ToUpperInvariant))
        {
            if (!pool.TryGetValue(letter, out var available) || available == 0)
            {
                return false;
            }

            pool[letter] = available - 1;
        }

        return true;
    }

    public void RemoveAll(IEnumerable<char> letters)
    {
        foreach (var letter in letters.Select(char.ToUpperInvariant))
        {
            _tiles.Remove(letter);
        }
    }

    public void Add(IEnumerable<char> letters)
    {
        _tiles.AddRange(letters.Select(char.ToUpperInvariant));
    }
}
