namespace TypoSwitch;

internal sealed class SettingsForm : Form
{
    private readonly CheckBox _auto = new();
    private readonly CheckBox _sound = new();
    private readonly ComboBox _soundStyle = new();
    private readonly Button _soundPreview = new();
    private readonly CheckBox _startup = new();
    private readonly CheckBox _checkUpdates = new();
    private readonly NumericUpDown _minLength = new();
    private readonly TextBox _exceptions = new();
    private readonly TextBox _ignored = new();
    private readonly AppConfig _config;

    public SettingsForm(AppConfig config)
    {
        _config = config;
        Text = "Turbo Switcher — настройки";
        Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(460, 500);
        AutoScaleMode = AutoScaleMode.Font;

        var intro = new Label
        {
            Text = "Исправляет слово, если вы печатали не в той раскладке.",
            AutoSize = false,
            Location = new Point(16, 16),
            Size = new Size(428, 24),
        };

        ConfigureCheck(_auto, "Автоматически исправлять раскладку", 48, config.AutoSwitch);
        ConfigureCheck(_sound, "Звук при исправлении", 80, config.Sound);

        _soundStyle.DropDownStyle = ComboBoxStyle.DropDownList;
        _soundStyle.Items.AddRange(["Стандартный Windows", "Turbo Switcher"]);
        _soundStyle.SelectedIndex = config.SoundStyle == SwitchSound.Custom ? 1 : 0;
        _soundStyle.Location = new Point(36, 108);
        _soundStyle.Size = new Size(250, 27);
        _soundStyle.Enabled = config.Sound;

        _soundPreview.Text = "Прослушать";
        _soundPreview.Location = new Point(296, 106);
        _soundPreview.Size = new Size(110, 30);
        _soundPreview.Enabled = config.Sound;
        _soundPreview.Click += (_, _) => PreviewSound();

        _sound.CheckedChanged += (_, _) =>
        {
            _soundStyle.Enabled = _soundPreview.Enabled = _sound.Checked;
        };

        ConfigureCheck(_startup, "Запускать вместе с Windows", 148, config.RunAtStartup);
        ConfigureCheck(_checkUpdates, "Проверять обновления", 180, config.CheckUpdates);

        var minLabel = new Label
        {
            Text = "Минимальная длина слова:",
            AutoSize = true,
            Location = new Point(16, 218),
        };
        _minLength.Location = new Point(250, 214);
        _minLength.Size = new Size(60, 27);
        _minLength.Minimum = 2;
        _minLength.Maximum = 8;
        _minLength.Value = Math.Clamp(config.MinWordLength, 2, 8);

        var exLabel = new Label { Text = "Исключения (через запятую):", AutoSize = true, Location = new Point(16, 258) };
        _exceptions.Location = new Point(16, 282);
        _exceptions.Size = new Size(428, 27);
        _exceptions.Text = string.Join(", ", config.Exceptions);

        var ignLabel = new Label { Text = "Не работать в процессах (chrome.exe, …):", AutoSize = true, Location = new Point(16, 320) };
        _ignored.Location = new Point(16, 344);
        _ignored.Size = new Size(428, 27);
        _ignored.Text = string.Join(", ", config.IgnoredProcesses);

        var keys = new Label
        {
            Text = "Pause — сменить последнее слово\nShift+Pause — сменить выделенный текст",
            AutoSize = false,
            Location = new Point(16, 384),
            Size = new Size(428, 44),
        };

        var save = new Button { Text = "Сохранить", Size = new Size(110, 32), Location = new Point(334, 448), DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Отмена", Size = new Size(110, 32), Location = new Point(214, 448), DialogResult = DialogResult.Cancel };
        var folder = new Button { Text = "Папка настроек", Size = new Size(140, 32), Location = new Point(16, 448) };
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

        Controls.AddRange([intro, _auto, _sound, _soundStyle, _soundPreview, _startup, _checkUpdates, minLabel, _minLength, exLabel, _exceptions, ignLabel, _ignored, keys, save, cancel, folder]);
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
