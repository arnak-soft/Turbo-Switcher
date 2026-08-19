using System.Text.Json;
using System.Text.Json.Serialization;

namespace TypoSwitch;

public sealed class AppConfig
{
    [JsonPropertyName("auto_switch")]
    public bool AutoSwitch { get; set; } = true;

    [JsonPropertyName("sound")]
    public bool Sound { get; set; }

    [JsonPropertyName("min_word_length")]
    public int MinWordLength { get; set; } = 3;

    [JsonPropertyName("exceptions")]
    public List<string> Exceptions { get; set; } = [];

    [JsonPropertyName("ignored_processes")]
    public List<string> IgnoredProcesses { get; set; } = [];

    [JsonPropertyName("run_at_startup")]
    public bool RunAtStartup { get; set; }

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
        return JsonSerializer.Deserialize(json, ConfigJsonContext.Default.AppConfig) ?? new AppConfig();
    }

    public void Save()
    {
        Directory.CreateDirectory(DirectoryPath);
        var json = JsonSerializer.Serialize(this, ConfigJsonContext.Default.AppConfig);
        File.WriteAllText(FilePath, json);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppConfig))]
internal partial class ConfigJsonContext : JsonSerializerContext;
