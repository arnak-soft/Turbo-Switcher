namespace TypoSwitch;

internal sealed class TrayApplication : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly KeyboardEngine _engine;
    private readonly Control _ui = new();
    private readonly ToolStripMenuItem _updateItem;
    private readonly ToolStripSeparator _updateSeparator;
    private readonly ToolStripMenuItem _autoMenuItem;
    private AppConfig _config;
    private Icon _icon;
    private SettingsForm? _settings;
    private UpdateChecker? _updates;
    private UpdateInfo? _updateInfo;

    public TrayApplication()
    {
        _config = AppConfig.Load();
        StartupShortcut.Apply(_config.RunAtStartup);
        _engine = new KeyboardEngine(_config);
        _engine.Enabled = _config.AutoSwitch;
        _engine.AutoSwitchToggleRequested += () => _ui.BeginInvoke(ToggleAutoSwitch);
        _engine.Start();

        _updateItem = new ToolStripMenuItem("Скачать обновление") { Visible = false };
        _updateItem.Click += (_, _) => OpenUpdatePage();
        _updateSeparator = new ToolStripSeparator { Visible = false };
        _autoMenuItem = new ToolStripMenuItem("Автоисправление")
        {
            Checked = _config.AutoSwitch,
            CheckOnClick = true,
        };
        _autoMenuItem.CheckedChanged += (_, _) => SetAutoSwitch(_autoMenuItem.Checked, notify: false);

        _icon = AppIcon.Create(_config.AutoSwitch);
        _tray = new NotifyIcon
        {
            Icon = _icon,
            Visible = true,
            Text = TrayText(),
            ContextMenuStrip = BuildMenu(),
        };
        _tray.DoubleClick += (_, _) => ShowSettings();
        _ = _ui.Handle;
        SingleInstance.Listen(() => _ui.BeginInvoke(ShowSettings));
        _ui.BeginInvoke(ShowStartupNotification);
        StartUpdateChecker();
    }

    private void ShowStartupNotification()
    {
        _tray.ShowBalloonTip(
            4000,
            "Turbo Switcher",
            "Программа запущена. Иконка — в области уведомлений возле часов.",
            ToolTipIcon.Info);

        // Чтобы пользователь точно заметил запуск (особенно если balloon-уведомление скрывается).
        SwitchSound.PlayToggle(_config.AutoSwitch);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(_updateItem);
        menu.Items.Add(_updateSeparator);
        menu.Items.Add(_autoMenuItem);
        menu.Items.Add("Сменить последнее слово", null, (_, _) => _engine.ConvertLastWord());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Настройки…", null, (_, _) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => ExitThread());
        return menu;
    }

    private void ToggleAutoSwitch() => SetAutoSwitch(!_config.AutoSwitch, notify: true);

    private void SetAutoSwitch(bool enabled, bool notify)
    {
        _config.AutoSwitch = enabled;
        _engine.Enabled = enabled;
        _config.Save();
        _autoMenuItem.Checked = enabled;
        RefreshIcon();

        if (!notify) return;

        var status = enabled ? "включено" : "выключено";
        _tray.ShowBalloonTip(
            2500,
            "Turbo Switcher",
            $"Автоисправление {status}",
            enabled ? ToolTipIcon.Info : ToolTipIcon.Warning);
        SwitchSound.PlayToggle(enabled);
    }

    private void StartUpdateChecker()
    {
        _updates?.Dispose();
        _updates = null;

        if (!_config.CheckUpdates) return;

        _updates = new UpdateChecker();
        _updates.UpdateAvailable += info => _ui.BeginInvoke(() => SetUpdateAvailable(info));
        _updates.Start();
    }

    private void SetUpdateAvailable(UpdateInfo? info)
    {
        _updateInfo = info;
        var visible = info is not null;
        _updateItem.Visible = visible;
        _updateSeparator.Visible = visible;
        if (info is { } update)
            _updateItem.Text = $"Скачать обновление ({update.Version})";
        RefreshIcon();
    }

    private void OpenUpdatePage()
    {
        if (_updateInfo is not { Url: var url }) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true,
        });
    }

    private void ShowSettings()
    {
        if (_settings is { IsDisposed: false })
        {
            if (_settings.WindowState == FormWindowState.Minimized)
                _settings.WindowState = FormWindowState.Normal;
            _settings.Activate();
            _settings.BringToFront();
            return;
        }

        using var form = new SettingsForm(_config);
        _settings = form;
        try
        {
            if (form.ShowDialog() != DialogResult.OK) return;
            var checkUpdatesBefore = _config.CheckUpdates;
            _config = AppConfig.Load();
            _engine.Reload(_config);
            _autoMenuItem.Checked = _config.AutoSwitch;
            RefreshIcon();
            if (checkUpdatesBefore != _config.CheckUpdates)
                StartUpdateChecker();
        }
        finally
        {
            _settings = null;
        }
    }

    private string TrayText()
    {
        if (_updateInfo is { } update)
            return $"Turbo Switcher — доступно обновление {update.Version}";
        return _config.AutoSwitch
            ? "Turbo Switcher — автоисправление вкл"
            : "Turbo Switcher — автоисправление выкл";
    }

    private void RefreshIcon()
    {
        var next = AppIcon.Create(_config.AutoSwitch, _updateInfo is not null);
        _tray.Icon = next;
        _icon.Dispose();
        _icon = next;
        _tray.Text = TrayText();
    }

    protected override void ExitThreadCore()
    {
        _updates?.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _icon.Dispose();
        _engine.Dispose();
        _ui.Dispose();
        base.ExitThreadCore();
    }
}

