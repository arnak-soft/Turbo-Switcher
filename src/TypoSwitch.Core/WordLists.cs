using System.Text;

namespace TypoSwitch;

public static class WordLists
{
    public static HashSet<string> Russian { get; } = Load("ru_words.txt");
    public static HashSet<string> English { get; } = Load("en_words.txt");
    public static HashSet<string> Exceptions { get; } = Load("exceptions.txt");

    private static HashSet<string> Load(string fileName)
    {
        var assembly = typeof(WordLists).Assembly;
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Resource {fileName} not found.");

        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Cannot open {fileName}.");
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.ReadLine() is { } line)
        {
            var word = line.Trim().ToLowerInvariant();
            if (word.Length == 0 || word.StartsWith('#')) continue;
            set.Add(word);
            var space = word.LastIndexOf(' ');
            if (space >= 0 && space < word.Length - 1)
                set.Add(word[(space + 1)..]);
        }
        return set;
    }
}
