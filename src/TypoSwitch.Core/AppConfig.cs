using System.Text.Json;
using System.Text.Json.Serialization;

namespace TypoSwitch;

public sealed class AppConfig
{
    [JsonPropertyName("auto_switch")]
    public bool AutoSwitch { get; set; } = true;

    // Горячая клавиша переключения автозамены (AutoSwitch).
    // Формат: "Ctrl+Shift+L", "Scroll", "Alt+K" и т.п.
    [JsonPropertyName("auto_switch_hotkey")]
    public string AutoSwitchHotkey { get; set; } = "Scroll";

    // Hotkeys для ручных действий (замена последнего слова / замена выделенного текста)
    [JsonPropertyName("hotkey_last_word")]
    public string HotkeyLastWord { get; set; } = "Pause";

    [JsonPropertyName("hotkey_selection")]
    public string HotkeySelection { get; set; } = "Shift+Pause";

    [JsonPropertyName("sound")]
    public bool Sound { get; set; }

    [JsonPropertyName("sound_style")]
    public string SoundStyle { get; set; } = "windows";

    [JsonPropertyName("min_word_length")]
    public int MinWordLength { get; set; } = 3;

    [JsonPropertyName("exceptions")]
    public List<string> Exceptions { get; set; } = [];

    [JsonPropertyName("ignored_processes")]
    public List<string> IgnoredProcesses { get; set; } = [];

    [JsonPropertyName("run_at_startup")]
    public bool RunAtStartup { get; set; }

    [JsonPropertyName("check_updates")]
    public bool CheckUpdates { get; set; } = true;

    public static string DirectoryPath
    {
        get
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(root, "Turbo Switcher");
        }
    }

    public static string FilePath => Path.Combine(DirectoryPath, "config.json");

    public static AppConfig Load()
    {
        Directory.CreateDirectory(DirectoryPath);
        if (!File.Exists(FilePath))
        {
            var legacy = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Typo Switcher",
                "config.json");
            if (File.Exists(legacy))
                File.Copy(legacy, FilePath);
        }

        if (!File.Exists(FilePath))
        {
            var created = new AppConfig();
            created.Save();
            return created;
        }

        var json = File.ReadAllText(FilePath);

        try
        {
            return JsonSerializer.Deserialize(json, ConfigJsonContext.Default.AppConfig) ?? new AppConfig();
        }
        catch (Exception)
        {
            // Битый или несовместимый config.json — пересоздаём, чтобы приложение могло запуститься.
            var created = new AppConfig();
            created.Save();
            return created;
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(DirectoryPath);
        var json = JsonSerializer.Serialize(this, ConfigJsonContext.Default.AppConfig);
        File.WriteAllText(FilePath, json);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, AllowTrailingCommas = true)]
[JsonSerializable(typeof(AppConfig))]
internal partial class ConfigJsonContext : JsonSerializerContext;
