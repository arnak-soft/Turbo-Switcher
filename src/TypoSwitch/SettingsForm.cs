namespace TypoSwitch;

internal sealed class SettingsForm : Form
{
    private readonly CheckBox _auto = new();
    private readonly CheckBox _undoBackspace = new();
    private readonly CheckBox _sound = new();
    private readonly ComboBox _soundStyle = new();
    private readonly Button _soundPreview = new();
    private readonly CheckBox _startup = new();
    private readonly CheckBox _checkUpdates = new();
    private readonly NumericUpDown _minLength = new();
    private readonly TextBox _exceptions = new();
    private readonly TextBox _ignored = new();
    private readonly TextBox _hotkeyText = new();
    private readonly Button _hotkeyButton = new();
    private readonly Label _hotkeyInfo = new();
    private readonly TextBox _lastWordText = new();
    private readonly Button _lastWordButton = new();
    private readonly Label _lastWordInfo = new();
    private readonly TextBox _selectionText = new();
    private readonly Button _selectionButton = new();
    private readonly Label _selectionInfo = new();
    private readonly Label _keysInfo = new();
    private bool _capturingHotkey;
    private HotkeyTarget _hotkeyTarget;
    private string _hotkeyDraft;
    private string _lastWordDraft;
    private string _selectionDraft;
    private readonly AppConfig _config;

    private enum HotkeyTarget
    {
        AutoSwitch,
        LastWord,
        Selection
    }

    public SettingsForm(AppConfig config)
    {
        _config = config;
        _hotkeyDraft = config.AutoSwitchHotkey;
        _lastWordDraft = config.HotkeyLastWord;
        _selectionDraft = config.HotkeySelection;
        _hotkeyTarget = HotkeyTarget.AutoSwitch;
        Text = "Turbo Switcher — настройки";
        Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(460, 732);
        AutoScaleMode = AutoScaleMode.Font;
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (!_capturingHotkey) return;
            CaptureHotkey(e);
        };

        var intro = new Label
        {
            Text = "Исправляет слово, если вы печатали не в той раскладке.",
            AutoSize = false,
            Location = new Point(16, 16),
            Size = new Size(428, 24),
        };

        ConfigureCheck(_auto, "Автоматически исправлять раскладку", 48, config.AutoSwitch);
        ConfigureCheck(_undoBackspace, "Отменять автозамену клавишей Backspace", 80, config.UndoBackspace);
        ConfigureCheck(_sound, "Звук при исправлении", 112, config.Sound);

        _soundStyle.DropDownStyle = ComboBoxStyle.DropDownList;
        _soundStyle.Items.AddRange(["Стандартный Windows", "Turbo Switcher"]);
        _soundStyle.SelectedIndex = config.SoundStyle == SwitchSound.Custom ? 1 : 0;
        _soundStyle.Location = new Point(36, 140);
        _soundStyle.Size = new Size(250, 27);
        _soundStyle.Enabled = config.Sound;

        _soundPreview.Text = "Прослушать";
        _soundPreview.Location = new Point(296, 138);
        _soundPreview.Size = new Size(110, 30);
        _soundPreview.Enabled = config.Sound;
        _soundPreview.Click += (_, _) => PreviewSound();

        _sound.CheckedChanged += (_, _) =>
        {
            _soundStyle.Enabled = _soundPreview.Enabled = _sound.Checked;
        };

        ConfigureCheck(_startup, "Запускать вместе с Windows", 180, config.RunAtStartup);
        ConfigureCheck(_checkUpdates, "Проверять обновления", 212, config.CheckUpdates);

        var minLabel = new Label
        {
            Text = "Минимальная длина слова:",
            AutoSize = true,
            Location = new Point(16, 250),
        };
        _minLength.Location = new Point(250, 246);
        _minLength.Size = new Size(60, 27);
        _minLength.Minimum = 2;
        _minLength.Maximum = 8;
        _minLength.Value = Math.Clamp(config.MinWordLength, 2, 8);

        var exLabel = new Label { Text = "Исключения (через запятую):", AutoSize = true, Location = new Point(16, 290) };
        _exceptions.Location = new Point(16, 314);
        _exceptions.Size = new Size(428, 27);
        _exceptions.Text = string.Join(", ", config.Exceptions);

        var ignLabel = new Label { Text = "Не работать в процессах (chrome.exe, …):", AutoSize = true, Location = new Point(16, 352) };
        _ignored.Location = new Point(16, 376);
        _ignored.Size = new Size(428, 27);
        _ignored.Text = string.Join(", ", config.IgnoredProcesses);

        _hotkeyInfo.Text = "Горячая клавиша (вкл/выкл автоисправление):";
        _hotkeyInfo.AutoSize = false;
        _hotkeyInfo.Location = new Point(16, 416);
        _hotkeyInfo.Size = new Size(220, 27);

        _hotkeyText.ReadOnly = true;
        _hotkeyText.Location = new Point(250, 412);
        _hotkeyText.Size = new Size(190, 27);
        _hotkeyText.Text = FormatHotkey(_hotkeyDraft);

        _hotkeyButton.Text = "Изменить";
        _hotkeyButton.Location = new Point(296, 440);
        _hotkeyButton.Size = new Size(144, 30);
        _hotkeyButton.Click += (_, _) =>
        {
            _hotkeyTarget = HotkeyTarget.AutoSwitch;
            _capturingHotkey = true;
            _hotkeyButton.Text = "Нажмите комбинацию…";
            _hotkeyText.Text = "";
            _hotkeyText.Focus();
        };

        _lastWordInfo.AutoSize = false;
        _lastWordInfo.Location = new Point(16, 472);
        _lastWordInfo.Size = new Size(220, 27);
        _lastWordInfo.Text = "Последнее слово:";

        _lastWordText.ReadOnly = true;
        _lastWordText.Location = new Point(250, 468);
        _lastWordText.Size = new Size(190, 27);
        _lastWordText.Text = FormatHotkey(_lastWordDraft);

        _lastWordButton.Text = "Изменить";
        _lastWordButton.Location = new Point(296, 496);
        _lastWordButton.Size = new Size(144, 30);
        _lastWordButton.Click += (_, _) =>
        {
            _hotkeyTarget = HotkeyTarget.LastWord;
            _capturingHotkey = true;
            _lastWordButton.Text = "Нажмите комбинацию…";
            _lastWordText.Text = "";
            _lastWordText.Focus();
        };

        _selectionInfo.AutoSize = false;
        _selectionInfo.Location = new Point(16, 532);
        _selectionInfo.Size = new Size(220, 27);
        _selectionInfo.Text = "Выделенный текст:";

        _selectionText.ReadOnly = true;
        _selectionText.Location = new Point(250, 528);
        _selectionText.Size = new Size(190, 27);
        _selectionText.Text = FormatHotkey(_selectionDraft);

        _selectionButton.Text = "Изменить";
        _selectionButton.Location = new Point(296, 556);
        _selectionButton.Size = new Size(144, 30);
        _selectionButton.Click += (_, _) =>
        {
            _hotkeyTarget = HotkeyTarget.Selection;
            _capturingHotkey = true;
            _selectionButton.Text = "Нажмите комбинацию…";
            _selectionText.Text = "";
            _selectionText.Focus();
        };

        _keysInfo.AutoSize = false;
        _keysInfo.Location = new Point(16, 592);
        _keysInfo.Size = new Size(428, 60);
        _keysInfo.Text =
            $"{FormatHotkey(_hotkeyDraft)} — автоисправление (вкл/выкл)\n" +
            $"{FormatHotkey(_lastWordDraft)} — сменить последнее слово\n" +
            $"{FormatHotkey(_selectionDraft)} — сменить выделенный текст";

        var save = new Button { Text = "Сохранить", Size = new Size(110, 32), Location = new Point(334, 672), DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Отмена", Size = new Size(110, 32), Location = new Point(214, 672), DialogResult = DialogResult.Cancel };
        var folder = new Button { Text = "Папка настроек", Size = new Size(140, 32), Location = new Point(16, 672) };
        folder.Click += (_, _) =>
        {
            Directory.CreateDirectory(AppConfig.DirectoryPath);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = AppConfig.DirectoryPath,
                UseShellExecute = true,
            });
        };

        save.Click += (_, _) => Apply();
        AcceptButton = save;
        CancelButton = cancel;

        Controls.AddRange([intro, _auto, _undoBackspace, _sound, _soundStyle, _soundPreview, _startup, _checkUpdates, minLabel, _minLength, exLabel, _exceptions, ignLabel, _ignored, _hotkeyInfo, _hotkeyText, _hotkeyButton, _lastWordInfo, _lastWordText, _lastWordButton, _selectionInfo, _selectionText, _selectionButton, _keysInfo, save, cancel, folder]);
    }

    private void CaptureHotkey(KeyEventArgs e)
    {
        var key = e.KeyCode;

        var isModifierOnly =
            key == Keys.ControlKey ||
            key == Keys.ShiftKey ||
            key == Keys.Menu ||
            key == Keys.LMenu ||
            key == Keys.RMenu ||
            key == Keys.LControlKey ||
            key == Keys.RControlKey ||
            key == Keys.LShiftKey ||
            key == Keys.RShiftKey ||
            key == Keys.LWin ||
            key == Keys.RWin;

        // Для автоисправления Pause оставляем под "сменить последнее слово".
        if (isModifierOnly || (_hotkeyTarget == HotkeyTarget.AutoSwitch && key == Keys.Pause))
        {
            return;
        }

        var parts = new List<string>(4);
        if (e.Control) parts.Add("Ctrl");
        if (e.Alt) parts.Add("Alt");
        if (e.Shift) parts.Add("Shift");

        if ((e.Modifiers & Keys.LWin) == Keys.LWin || (e.Modifiers & Keys.RWin) == Keys.RWin)
            parts.Add("Win");

        parts.Add(key.ToString());
        var draft = string.Join("+", parts);

        _capturingHotkey = false;
        switch (_hotkeyTarget)
        {
            case HotkeyTarget.AutoSwitch:
                _hotkeyDraft = draft;
                _hotkeyText.Text = FormatHotkey(_hotkeyDraft);
                _hotkeyButton.Text = "Изменить";
                break;

            case HotkeyTarget.LastWord:
                _lastWordDraft = draft;
                _lastWordText.Text = FormatHotkey(_lastWordDraft);
                _lastWordButton.Text = "Изменить";
                break;

            case HotkeyTarget.Selection:
                _selectionDraft = draft;
                _selectionText.Text = FormatHotkey(_selectionDraft);
                _selectionButton.Text = "Изменить";
                break;
        }

        _keysInfo.Text =
            $"{FormatHotkey(_hotkeyDraft)} — автоисправление (вкл/выкл)\n" +
            $"{FormatHotkey(_lastWordDraft)} — сменить последнее слово\n" +
            $"{FormatHotkey(_selectionDraft)} — сменить выделенный текст";
    }

    private static string FormatHotkey(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "—";

        var parts = raw.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p.Equals("Scroll", StringComparison.OrdinalIgnoreCase))
                parts[i] = "Scroll Lock";
            else if (p.Equals("Pause", StringComparison.OrdinalIgnoreCase))
                parts[i] = "Pause";
            else if (p.Equals("Escape", StringComparison.OrdinalIgnoreCase) || p.Equals("Esc", StringComparison.OrdinalIgnoreCase))
                parts[i] = "Esc";
        }
        return string.Join("+", parts);
    }

    private void ConfigureCheck(CheckBox box, string text, int top, bool value)
    {
        box.Text = text;
        box.AutoSize = true;
        box.Location = new Point(16, top);
        box.Checked = value;
    }

    private void Apply()
    {
        _config.AutoSwitch = _auto.Checked;
        _config.UndoBackspace = _undoBackspace.Checked;
        _config.AutoSwitchHotkey = _hotkeyDraft;
        _config.HotkeyLastWord = _lastWordDraft;
        _config.HotkeySelection = _selectionDraft;
        _config.Sound = _sound.Checked;
        _config.SoundStyle = _soundStyle.SelectedIndex == 1 ? SwitchSound.Custom : SwitchSound.Windows;
        _config.RunAtStartup = _startup.Checked;
        _config.CheckUpdates = _checkUpdates.Checked;
        _config.MinWordLength = (int)_minLength.Value;
        _config.Exceptions = Split(_exceptions.Text);
        _config.IgnoredProcesses = Split(_ignored.Text);
        _config.Save();
        StartupShortcut.Apply(_config.RunAtStartup);
    }

    private void PreviewSound()
    {
        if (!_sound.Checked) return;
        var style = _soundStyle.SelectedIndex == 1 ? SwitchSound.Custom : SwitchSound.Windows;
        SwitchSound.Play(true, style);
    }

    private static List<string> Split(string text) =>
        text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
}
