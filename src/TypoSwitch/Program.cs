namespace TypoSwitch;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try
        {
            if (!SingleInstance.TryLock())
            {
                if (!SingleInstance.ActivateExisting())
                {
                    System.Windows.Forms.MessageBox.Show(
                        "Turbo Switcher уже запущен.",
                        "Turbo Switcher",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Information);
                }
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new TrayApplication());
        }
        catch (Exception ex)
        {
            TryWriteStartupError(ex);

            System.Windows.Forms.MessageBox.Show(
                $"Turbo Switcher не смог запуститься.\n\n{ex.Message}\n\nПожалуйста, пришлите файл:\n{AppConfig.FilePath.Replace("config.json", "startup_error.txt")}",
                "Turbo Switcher",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }
    }

    private static void TryWriteStartupError(Exception ex)
    {
        try
        {
            var dir = AppConfig.DirectoryPath;
            System.IO.Directory.CreateDirectory(dir);
            var path = System.IO.Path.Combine(dir, "startup_error.txt");
            System.IO.File.WriteAllText(path, ex.ToString());
        }
        catch
        {
            // ignore logging failures
        }
    }
}
