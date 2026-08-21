using System.Text.Json;
using System.Text.Json.Serialization;

namespace TypoSwitch;

public enum MemoryUpdate
{
    None,
    Tracked,
    Learned,
}

public sealed class WordMemory
{
    public const int DefaultUndoLearnAfter = 4;
    public const int DefaultWordLearnAfter = 5;
    public const int MinUndoLearnAfter = 3;
    public const int MaxUndoLearnAfter = 5;
    public const string FileName = "memory.json";

    private const int MaxTrackedTyped = 500;
    private const int MaxTrackedUndos = 200;

    private readonly object _sync = new();
    private readonly string _filePath;
    private readonly Dictionary<string, int> _typed = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _undos = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _learnedWords = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _learnedExceptions = new(StringComparer.OrdinalIgnoreCase);
    private bool _dirty;

    public WordMemory(string? directoryPath = null)
    {
        var dir = directoryPath ?? AppConfig.DirectoryPath;
        _filePath = Path.Combine(dir, FileName);
        Load();
    }

    public string FilePath => _filePath;

    public IReadOnlyCollection<string> LearnedWords
    {
        get { lock (_sync) return _learnedWords.ToArray(); }
    }

    public IReadOnlyCollection<string> LearnedExceptions
    {
        get { lock (_sync) return _learnedExceptions.ToArray(); }
    }

    public bool HasLearned
    {
        get
        {
            lock (_sync)
                return _learnedWords.Count > 0 || _learnedExceptions.Count > 0;
        }
    }

    public string Summary()
    {
        lock (_sync)
        {
            var items = _learnedExceptions.Concat(_learnedWords).Take(12).ToArray();
            if (items.Length == 0)
                return "";
            var text = string.Join(", ", items);
            var more = _learnedExceptions.Count + _learnedWords.Count - items.Length;
            return more > 0 ? $"{text}…" : text;
        }
    }

    public MemoryUpdate RecordKeptWord(string word, int minLength, int learnAfter = DefaultWordLearnAfter)
    {
        var key = Normalize(word);
        if (key.Length < minLength)
            return MemoryUpdate.None;
        if (IsBuiltIn(key))
            return MemoryUpdate.None;

        lock (_sync)
        {
            if (_learnedWords.Contains(key) || _learnedExceptions.Contains(key))
                return MemoryUpdate.None;

            _typed[key] = _typed.TryGetValue(key, out var n) ? n + 1 : 1;
            _dirty = true;
            Prune(_typed, _learnedWords, MaxTrackedTyped);

            if (_typed[key] < learnAfter)
                return MemoryUpdate.Tracked;

            _learnedWords.Add(key);
            return MemoryUpdate.Learned;
        }
    }

    public MemoryUpdate RecordUndo(string original, int learnAfter = DefaultUndoLearnAfter)
    {
        var key = Normalize(original);
        if (key.Length == 0)
            return MemoryUpdate.None;
        if (WordLists.Exceptions.Contains(key))
            return MemoryUpdate.None;

        var after = Math.Clamp(learnAfter, MinUndoLearnAfter, MaxUndoLearnAfter);

        lock (_sync)
        {
            if (_learnedExceptions.Contains(key))
                return MemoryUpdate.None;

            _undos[key] = _undos.TryGetValue(key, out var n) ? n + 1 : 1;
            _dirty = true;
            Prune(_undos, _learnedExceptions, MaxTrackedUndos);

            if (_undos[key] < after)
                return MemoryUpdate.Tracked;

            _learnedExceptions.Add(key);
            return MemoryUpdate.Learned;
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            _typed.Clear();
            _undos.Clear();
            _learnedWords.Clear();
            _learnedExceptions.Clear();
            _dirty = true;
        }
        Save();
    }

    public void Save()
    {
        lock (_sync)
        {
            if (!_dirty)
                return;

            try
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var state = new WordMemoryState
                {
                    Typed = new Dictionary<string, int>(_typed, StringComparer.OrdinalIgnoreCase),
                    Undos = new Dictionary<string, int>(_undos, StringComparer.OrdinalIgnoreCase),
                    LearnedWords = [.. _learnedWords],
                    LearnedExceptions = [.. _learnedExceptions],
                };
                var json = JsonSerializer.Serialize(state, MemoryJsonContext.Default.WordMemoryState);
                var tmp = _filePath + ".tmp";
                File.WriteAllText(tmp, json);
                File.Copy(tmp, _filePath, overwrite: true);
                File.Delete(tmp);
                _dirty = false;
            }
            catch
            {
                // Следующий вызов Save повторит запись.
            }
        }
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
            return;

        try
        {
            var json = File.ReadAllText(_filePath);
            var state = JsonSerializer.Deserialize(json, MemoryJsonContext.Default.WordMemoryState);
            if (state is null)
                return;

            foreach (var (word, count) in state.Typed)
            {
                var key = Normalize(word);
                if (key.Length == 0 || count <= 0) continue;
                _typed[key] = count;
            }

            foreach (var (word, count) in state.Undos)
            {
                var key = Normalize(word);
                if (key.Length == 0 || count <= 0) continue;
                _undos[key] = count;
            }

            foreach (var word in state.LearnedWords)
            {
                var key = Normalize(word);
                if (key.Length > 0)
                    _learnedWords.Add(key);
            }

            foreach (var word in state.LearnedExceptions)
            {
                var key = Normalize(word);
                if (key.Length > 0)
                    _learnedExceptions.Add(key);
            }
        }
        catch
        {
            _typed.Clear();
            _undos.Clear();
            _learnedWords.Clear();
            _learnedExceptions.Clear();
        }
    }

    private static bool IsBuiltIn(string key) =>
        WordLists.Russian.Contains(key) ||
        WordLists.English.Contains(key) ||
        WordLists.Exceptions.Contains(key);

    private static string Normalize(string word) => word.Trim().ToLowerInvariant();

    private static void Prune(Dictionary<string, int> counts, HashSet<string> keep, int max)
    {
        if (counts.Count <= max)
            return;

        var extra = counts.Count - max;
        var victims = counts
            .Where(kv => !keep.Contains(kv.Key))
            .OrderBy(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(extra)
            .Select(kv => kv.Key)
            .ToArray();

        foreach (var key in victims)
            counts.Remove(key);
    }
}

internal sealed class WordMemoryState
{
    [JsonPropertyName("typed")]
    public Dictionary<string, int> Typed { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("undos")]
    public Dictionary<string, int> Undos { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("learned_words")]
    public List<string> LearnedWords { get; set; } = [];

    [JsonPropertyName("learned_exceptions")]
    public List<string> LearnedExceptions { get; set; } = [];
}

[JsonSourceGenerationOptions(WriteIndented = true, AllowTrailingCommas = true)]
[JsonSerializable(typeof(WordMemoryState))]
internal partial class MemoryJsonContext : JsonSerializerContext;
