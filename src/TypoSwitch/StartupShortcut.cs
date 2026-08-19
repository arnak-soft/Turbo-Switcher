namespace TypoSwitch;

internal static class StartupShortcut
{
    private static string CmdPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Typo Switcher.cmd");

    public static void Apply(bool enabled)
    {
        var legacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "TypoSwitch.cmd");
        if (File.Exists(legacy))
            File.Delete(legacy);

        if (enabled)
        {
            var exe = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "TypoSwitch.exe");
            File.WriteAllText(CmdPath, $"@echo off\r\nstart \"\" \"{exe}\"\r\n");
        }
        else if (File.Exists(CmdPath))
        {
            File.Delete(CmdPath);
        }
    }
}
