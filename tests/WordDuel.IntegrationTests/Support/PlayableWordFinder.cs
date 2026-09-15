namespace WordDuel.IntegrationTests.Support;

/// <summary>
/// Match setup draws a deterministic-but-unpredictable rack per match ID, so
/// tests can't hard-code a word to play. This scans the real bundled
/// dictionary (loaded the same way WordListProvider does) for any word the
/// given rack can actually play, so tests stay valid regardless of the draw.
/// </summary>
public static class PlayableWordFinder
{
    private static readonly IReadOnlyList<string> AllWords = LoadWords();

    public static (string Word, int StartRow, int StartCol) FindFirstMovePlacement(string rack)
    {
        if (TryFindFirstMovePlacement(rack, out var placement))
        {
            return placement;
        }

        throw new InvalidOperationException($"No playable word found for rack '{rack}'.");
    }

    /// <summary>
    /// Non-throwing variant. A real deterministic draw can occasionally be
    /// unplayable (e.g. an all-consonant rack) — callers that control match
    /// creation should retry with a fresh match rather than treat this as a
    /// dictionary/engine bug.
    /// </summary>
    public static bool TryFindFirstMovePlacement(string rack, out (string Word, int StartRow, int StartCol) placement)
    {
        foreach (var word in AllWords)
        {
            if (ContainsAll(rack, word))
            {
                var len = word.Length;
                var startCol = Math.Clamp(3 - (len / 2), 0, 7 - len);
                placement = (word, 3, startCol);
                return true;
            }
        }

        placement = default;
        return false;
    }

    /// <summary>Finds two rack letters whose 2-letter combination is NOT a dictionary word.</summary>
    public static (char First, char Second) FindUnplayablePair(string rack)
    {
        for (var i = 0; i < rack.Length; i++)
        {
            for (var j = 0; j < rack.Length; j++)
            {
                if (i == j) continue;
                var candidate = $"{rack[i]}{rack[j]}";
                if (!AllWords.Contains(candidate))
                {
                    return (rack[i], rack[j]);
                }
            }
        }

        throw new InvalidOperationException($"Could not find an unplayable 2-letter combination for rack '{rack}'.");
    }

    private static bool ContainsAll(string rack, string word)
    {
        var pool = rack.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());
        foreach (var ch in word)
        {
            if (!pool.TryGetValue(ch, out var n) || n == 0)
            {
                return false;
            }

            pool[ch] = n - 1;
        }

        return true;
    }

    private static IReadOnlyList<string> LoadWords()
    {
        var assembly = typeof(WordDuel.Domain.WordList.WordListProvider).Assembly;
        var resourceName = assembly.GetManifestResourceNames().Single(n => n.EndsWith("words.txt", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);

        var words = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var word = line.Trim().ToUpperInvariant();
            if (word.Length is >= 2 and <= 7)
            {
                words.Add(word);
            }
        }

        return words.OrderBy(w => w.Length).ToList();
    }
}
