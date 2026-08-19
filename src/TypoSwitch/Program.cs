namespace TypoSwitch;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        if (!SingleInstance.TryLock())
        {
            if (!SingleInstance.ActivateExisting())
            {
                MessageBox.Show(
                    "Turbo Switcher уже запущен.",
                    "Turbo Switcher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplication());
    }
}
