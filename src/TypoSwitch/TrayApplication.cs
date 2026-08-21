namespace TypoSwitch;

internal sealed class TrayApplication : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly KeyboardEngine _engine;
    private readonly WordMemory _memory;
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
        _memory = new WordMemory();
        _engine = new KeyboardEngine(_config, _memory);
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
        StartUpdateChecker();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(_updateItem);
        menu.Items.Add(_updateSeparator);
        menu.Items.Add("Проверить обновления", null, (_, _) => _ = CheckUpdatesManuallyAsync());
        menu.Items.Add(new ToolStripSeparator());
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

    private async Task CheckUpdatesManuallyAsync(IWin32Window? owner = null)
    {
        UpdateFetchResult result;
        if (_updates is not null)
            result = await _updates.FetchLatestUpdateAsync().ConfigureAwait(true);
        else
        {
            using var checker = new UpdateChecker();
            result = await checker.FetchLatestUpdateAsync().ConfigureAwait(true);
        }

        switch (result.Status)
        {
            case UpdateFetchStatus.Available:
            {
                var update = result.Update!.Value;
                SetUpdateAvailable(update);
                MessageBox.Show(
                    owner,
                    $"Доступна версия {update.Version}.\n\nСкачать можно через пункт меню «Скачать обновление».",
                    "Turbo Switcher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                break;
            }

            case UpdateFetchStatus.UpToDate:
                SetUpdateAvailable(null);
                MessageBox.Show(
                    owner,
                    "У вас установлена последняя версия.",
                    "Turbo Switcher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                break;

            case UpdateFetchStatus.Busy:
                MessageBox.Show(
                    owner,
                    "Проверка обновлений уже выполняется.",
                    "Turbo Switcher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                break;

            default:
                MessageBox.Show(
                    owner,
                    "Не удалось проверить обновления. Проверьте подключение к интернету.",
                    "Turbo Switcher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                break;
        }
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

        using var form = new SettingsForm(_config, owner => CheckUpdatesManuallyAsync(owner), _memory, () => _engine.Reload(_config));
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

