using System.Reflection;

namespace WordDuel.Domain.WordList;

/// <summary>
/// Loads the small, bundled, synthetic Word Duel dictionary (common English
/// words used purely as game content; no proprietary word lists).
/// </summary>
public sealed class WordListProvider
{
    private readonly HashSet<string> _words;

    public WordListProvider()
    {
        _words = LoadFromEmbeddedResource();
    }

    public int WordCount => _words.Count;

    public bool IsValidWord(string word) => _words.Contains(word.ToUpperInvariant());

    private static HashSet<string> LoadFromEmbeddedResource()
    {
        var assembly = typeof(WordListProvider).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(n => n.EndsWith("words.txt", StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Bundled word list resource not found.");
        using var reader = new StreamReader(stream);

        var set = new HashSet<string>(StringComparer.Ordinal);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var word = line.Trim().ToUpperInvariant();
            if (word.Length > 0)
            {
                set.Add(word);
            }
        }

        return set;
    }
}
