namespace TypoSwitch;

internal sealed class TrayApplication : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly KeyboardEngine _engine;
    private AppConfig _config;
    private Icon _icon;

    public TrayApplication()
    {
        _config = AppConfig.Load();
        StartupShortcut.Apply(_config.RunAtStartup);
        _engine = new KeyboardEngine(_config);
        _engine.Enabled = _config.AutoSwitch;
        _engine.Start();

        _icon = AppIcon.Create(_config.AutoSwitch);
        _tray = new NotifyIcon
        {
            Icon = _icon,
            Visible = true,
            Text = "Turbo Switcher",
            ContextMenuStrip = BuildMenu(),
        };
        _tray.DoubleClick += (_, _) => ShowSettings();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        var auto = new ToolStripMenuItem("Автоисправление") { Checked = _config.AutoSwitch, CheckOnClick = true };
        auto.CheckedChanged += (_, _) =>
        {
            _config.AutoSwitch = auto.Checked;
            _engine.Enabled = auto.Checked;
            _config.Save();
            RefreshIcon();
        };
        menu.Items.Add(auto);
        menu.Items.Add("Сменить последнее слово (Pause)", null, (_, _) => _engine.ConvertLastWord());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Настройки…", null, (_, _) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => ExitThread());
        return menu;
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_config);
        if (form.ShowDialog() != DialogResult.OK) return;
        _config = AppConfig.Load();
        _engine.Reload(_config);
        RefreshIcon();
        if (_tray.ContextMenuStrip is { } menu && menu.Items[0] is ToolStripMenuItem item)
            item.Checked = _config.AutoSwitch;
    }

    private void RefreshIcon()
    {
        var next = AppIcon.Create(_config.AutoSwitch);
        _tray.Icon = next;
        _icon.Dispose();
        _icon = next;
        _tray.Text = _config.AutoSwitch ? "Turbo Switcher — вкл" : "Turbo Switcher — выкл";
    }

    protected override void ExitThreadCore()
    {
        _tray.Visible = false;
        _tray.Dispose();
        _icon.Dispose();
        _engine.Dispose();
        base.ExitThreadCore();
    }
}
