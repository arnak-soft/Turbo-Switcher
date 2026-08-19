namespace TypoSwitch;

internal static class StartupShortcut
{
    private static string CmdPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Turbo Switcher.cmd");

    public static void Apply(bool enabled)
    {
        foreach (var name in new[] { "TypoSwitch.cmd", "Typo Switcher.cmd" })
        {
            var legacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), name);
            if (File.Exists(legacy))
                File.Delete(legacy);
        }

        if (enabled)
        {
            var exe = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "TurboSwitch.exe");
            File.WriteAllText(CmdPath, $"@echo off\r\nstart \"\" \"{exe}\"\r\n");
        }
        else if (File.Exists(CmdPath))
        {
            File.Delete(CmdPath);
        }
    }
}
