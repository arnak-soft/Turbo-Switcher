namespace TypoSwitch;

public static class Layouts
{
    private const string EnChars = "`qwertyuiop[]asdfghjkl;'zxcvbnm,./~QWERTYUIOP{}ASDFGHJKL:\"ZXCVBNM<>?";
    private const string RuChars = "ёйцукенгшщзхъфывапролджэячсмитьбю.ЁЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮ,";

    private static readonly Dictionary<char, char> EnToRu = Build(EnChars, RuChars);
    private static readonly Dictionary<char, char> RuToEn = Build(RuChars, EnChars);

    public static readonly HashSet<char> Convertible = [..EnChars, ..RuChars, '-'];

    private static readonly HashSet<char> Latin = [.."`qwertyuiop[]asdfghjkl;'zxcvbnm,./~QWERTYUIOP{}ASDFGHJKL:\"ZXCVBNM<>?abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"];
    private static readonly HashSet<char> Cyrillic = [.. "абвгдеёжзийклмнопрстуфхцчшщъыьэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ"];

    private static Dictionary<char, char> Build(string from, string to)
    {
        if (from.Length != to.Length)
            throw new InvalidOperationException("Layout maps must be the same length.");
        var map = new Dictionary<char, char>(from.Length);
        for (var i = 0; i < from.Length; i++)
            map[from[i]] = to[i];
        return map;
    }

    public static bool IsLatin(string text)
    {
        var any = false;
        foreach (var ch in text)
        {
            if (!char.IsLetter(ch)) continue;
            any = true;
            if (!Latin.Contains(ch)) return false;
        }
        return any;
    }

    public static bool IsCyrillic(string text)
    {
        var any = false;
        foreach (var ch in text)
        {
            if (!char.IsLetter(ch)) continue;
            any = true;
            if (!Cyrillic.Contains(ch)) return false;
        }
        return any;
    }

    public static string Invert(string text)
    {
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var ch = chars[i];
            if (EnToRu.TryGetValue(ch, out var ru)) chars[i] = ru;
            else if (RuToEn.TryGetValue(ch, out var en)) chars[i] = en;
        }
        return new string(chars);
    }

    public static string MajorityInvert(string text)
    {
        var latin = 0;
        var cyr = 0;
        foreach (var ch in text)
        {
            if (Latin.Contains(ch)) latin++;
            else if (Cyrillic.Contains(ch)) cyr++;
        }
        var map = cyr > latin ? RuToEn : EnToRu;
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (map.TryGetValue(chars[i], out var mapped))
                chars[i] = mapped;
        }
        return new string(chars);
    }
}
