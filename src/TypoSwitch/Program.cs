namespace TypoSwitch;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, @"Local\TypoSwitch.Singleton", out var created);
        if (!created)
        {
            MessageBox.Show("Typo Switcher уже запущен.", "Typo Switcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplication());
    }
}
