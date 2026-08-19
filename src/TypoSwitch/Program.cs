namespace TypoSwitch;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, @"Local\TurboSwitch.Singleton", out var created);
        if (!created)
        {
            MessageBox.Show("Turbo Switcher уже запущен.", "Turbo Switcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplication());
    }
}
