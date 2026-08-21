namespace TypoSwitch;

public sealed class Detection
{
    public bool ShouldSwitch { get; init; }
    public string Original { get; init; } = "";
    public string Converted { get; init; } = "";
    public double OriginalScore { get; init; }
    public double ConvertedScore { get; init; }
    public string Reason { get; init; } = "";

    public override string ToString() =>
        $"Detection(switch={ShouldSwitch}, '{Original}' -> '{Converted}', scores={OriginalScore:0.0}/{ConvertedScore:0.0}, reason={Reason})";
}

public sealed class Detector
{
    private static readonly Dictionary<string, int> EnBigrams = new()
    {
        ["th"] = 9, ["he"] = 8, ["in"] = 8, ["er"] = 8, ["an"] = 7, ["re"] = 7, ["on"] = 7, ["at"] = 7,
        ["en"] = 7, ["nd"] = 7, ["ti"] = 6, ["es"] = 6, ["or"] = 6, ["te"] = 6, ["of"] = 6, ["ed"] = 6,
        ["is"] = 6, ["it"] = 6, ["al"] = 6, ["ar"] = 6, ["st"] = 6, ["to"] = 6, ["nt"] = 6, ["ng"] = 6,
        ["se"] = 5, ["ha"] = 5, ["as"] = 5, ["ou"] = 5, ["io"] = 5, ["le"] = 5, ["ve"] = 5, ["co"] = 5,
        ["me"] = 5, ["de"] = 5, ["hi"] = 5, ["ri"] = 5, ["ro"] = 5, ["ic"] = 5, ["ne"] = 4, ["ea"] = 4,
        ["ra"] = 4, ["ce"] = 4, ["li"] = 4, ["ch"] = 4, ["ll"] = 4, ["be"] = 4, ["ma"] = 4, ["si"] = 4,
        ["om"] = 4, ["ur"] = 4, ["ca"] = 4, ["el"] = 4, ["ta"] = 4, ["la"] = 4, ["ns"] = 4, ["ho"] = 4,
        ["wh"] = 4, ["tr"] = 3, ["ss"] = 3, ["un"] = 3, ["qu"] = 3, ["ck"] = 3, ["gh"] = 2, ["ly"] = 4,
    };

    private static readonly Dictionary<string, int> RuBigrams = new()
    {
        ["ст"] = 9, ["но"] = 8, ["ен"] = 8, ["то"] = 8, ["на"] = 8, ["ни"] = 7, ["ко"] = 7, ["ра"] = 7,
        ["во"] = 7, ["ро"] = 7, ["ан"] = 6, ["ов"] = 8, ["ер"] = 6, ["ли"] = 7, ["ор"] = 6, ["го"] = 7,
        ["ал"] = 6, ["не"] = 8, ["пр"] = 8, ["по"] = 8, ["ре"] = 7, ["ка"] = 7, ["ел"] = 6, ["ть"] = 8,
        ["ое"] = 6, ["ой"] = 6, ["ие"] = 6, ["ия"] = 6, ["ам"] = 5, ["ом"] = 6, ["ем"] = 6, ["ет"] = 7,
        ["ла"] = 6, ["ло"] = 6, ["ль"] = 6, ["ск"] = 6, ["тр"] = 6, ["че"] = 6, ["ши"] = 5, ["жи"] = 5,
        ["вы"] = 6, ["за"] = 7, ["от"] = 7, ["об"] = 6, ["со"] = 6, ["до"] = 6, ["мо"] = 6, ["бо"] = 5,
        ["да"] = 6, ["та"] = 6, ["те"] = 6, ["ти"] = 6, ["си"] = 5, ["ми"] = 5, ["ри"] = 6, ["ви"] = 5,
        ["дн"] = 5, ["чн"] = 5, ["жн"] = 5, ["сн"] = 5, ["бл"] = 4, ["кл"] = 4, ["сл"] = 5,
    };

    private static readonly string[] EnSuffixes = ["ing", "tion", "sion", "ness", "ment", "able", "ible", "ful", "ous", "ally", "ed", "ly", "er", "est"];
    private static readonly string[] RuSuffixes = ["ого", "ему", "ами", "ями", "ить", "ать", "еть", "ение", "ения", "ский", "ться", "тся", "ешь", "ишь"];

    private readonly HashSet<string> _ru = WordLists.Russian;
    private readonly HashSet<string> _en = WordLists.English;
    private readonly HashSet<string> _exceptions;
    private readonly HashSet<string> _known;
    private readonly int _minLength;
    private readonly double _margin;

    public Detector(
        int minLength = 3,
        double margin = 2.5,
        IEnumerable<string>? extraExceptions = null,
        IEnumerable<string>? extraKnownWords = null)
    {
        _minLength = minLength;
        _margin = margin;
        _exceptions = new HashSet<string>(WordLists.Exceptions, StringComparer.OrdinalIgnoreCase);
        _known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddAll(_exceptions, extraExceptions);
        AddAll(_known, extraKnownWords);
    }

    public bool ShouldSwitch(string word) => Analyze(word).ShouldSwitch;

    public Detection Analyze(string word)
    {
        var cleaned = word.Trim();
        var letters = LettersOnly(cleaned);
        var key = cleaned.ToLowerInvariant();

        Detection Keep(string reason, string converted = "", double orig = 0, double conv = 0) =>
            new()
            {
                ShouldSwitch = false,
                Original = cleaned,
                Converted = converted.Length == 0 ? cleaned : converted,
                OriginalScore = orig,
                ConvertedScore = conv,
                Reason = reason,
            };

        if (letters.Length < _minLength)
            return Keep("too_short");
        if (cleaned.Any(char.IsDigit))
            return Keep("has_digits");
        if (_exceptions.Contains(key) || _exceptions.Contains(letters.ToLowerInvariant()))
            return Keep("exception");
        if (!Layouts.IsLatin(letters) && !Layouts.IsCyrillic(letters))
            return Keep("mixed_script");

        var converted = Layouts.Invert(cleaned);
        var originalScore = Score(cleaned);
        var convertedScore = Score(converted);
        var convKey = converted.ToLowerInvariant();
        var convLetters = LettersOnly(convKey);

        if (IsKnown(key) || IsKnown(letters.ToLowerInvariant()))
            return Keep("known_word", converted, originalScore, convertedScore);

        if (!IsKnownInTarget(convKey, letters) && !IsKnownInTarget(convLetters, letters))
            return Keep("converted_unknown", converted, originalScore, convertedScore);

        if (convertedScore >= originalScore + _margin)
        {
            return new Detection
            {
                ShouldSwitch = true,
                Original = cleaned,
                Converted = converted,
                OriginalScore = originalScore,
                ConvertedScore = convertedScore,
                Reason = "wrong_layout",
            };
        }

        return Keep("keep", converted, originalScore, convertedScore);
    }

    public double Score(string word)
    {
        var letters = LettersOnly(word.ToLowerInvariant());
        if (letters.Length == 0) return 0;

        var points = 0.0;
        var low = word.ToLowerInvariant();
        if (_ru.Contains(low) || _ru.Contains(letters))
            points += 18 + Math.Min(letters.Length, 8);
        if (_en.Contains(low) || _en.Contains(letters))
            points += 18 + Math.Min(letters.Length, 8);
        if (_known.Contains(low) || _known.Contains(letters))
            points += 18 + Math.Min(letters.Length, 8);

        if (Layouts.IsCyrillic(letters))
        {
            points += BigramScore(letters, RuBigrams);
            points += SuffixBonus(letters, RuSuffixes);
            if (letters.IndexOfAny(['ы', 'ь', 'ъ', 'э', 'ю', 'я', 'ё', 'щ']) >= 0)
                points += 1.5;
        }
        else if (Layouts.IsLatin(letters))
        {
            points += BigramScore(letters, EnBigrams);
            points += SuffixBonus(letters, EnSuffixes);
            if (letters.IndexOfAny(['w', 'q', 'j', 'x']) >= 0)
                points += 0.8;
        }

        return points;
    }

    private static string LettersOnly(string text)
    {
        var buffer = new char[text.Length];
        var n = 0;
        foreach (var ch in text)
        {
            if (char.IsLetter(ch))
                buffer[n++] = ch;
        }
        return n == 0 ? "" : new string(buffer, 0, n);
    }

    private static double BigramScore(string letters, Dictionary<string, int> table)
    {
        if (letters.Length < 2) return 0;
        var total = 0.0;
        for (var i = 0; i < letters.Length - 1; i++)
        {
            if (table.TryGetValue(letters.Substring(i, 2), out var w))
                total += w;
        }
        return total / (letters.Length - 1);
    }

    private static double SuffixBonus(string letters, string[] suffixes)
    {
        foreach (var suffix in suffixes)
        {
            if (letters.EndsWith(suffix, StringComparison.Ordinal) && letters.Length > suffix.Length + 1)
                return 2.2;
        }
        return 0;
    }

    private bool IsKnown(string word) =>
        word.Length > 0 && (_en.Contains(word) || _ru.Contains(word) || _known.Contains(word));

    private static void AddAll(HashSet<string> target, IEnumerable<string>? items)
    {
        if (items is null) return;
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item))
                target.Add(item.Trim().ToLowerInvariant());
        }
    }

    private bool IsKnownInTarget(string word, string originalLetters)
    {
        if (word.Length == 0) return false;
        if (_known.Contains(word)) return true;
        if (Layouts.IsLatin(originalLetters))
            return _ru.Contains(word);
        if (Layouts.IsCyrillic(originalLetters))
            return _en.Contains(word);
        return false;
    }
}
