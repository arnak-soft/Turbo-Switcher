using System.Collections.Concurrent;
using System.Media;
using System.Runtime.InteropServices;

namespace TypoSwitch;

internal sealed class KeyboardEngine : IDisposable
{
    private readonly BlockingCollection<Action> _jobs = new();
    private readonly object _sync = new();
    private Native.LowLevelKeyboardProc? _hookProc;
    private IntPtr _hook;
    private Thread? _worker;
    private AppConfig _config;
    private Detector _detector;
    private HashSet<string> _ignored;
    private string _buffer = "";
    private Committed? _committed;
    private bool _enabled = true;

    public KeyboardEngine(AppConfig config)
    {
        _config = config;
        _detector = new Detector(config.MinWordLength, extraExceptions: config.Exceptions);
        _ignored = ToSet(config.IgnoredProcesses);
    }

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public void Reload(AppConfig config)
    {
        lock (_sync)
        {
            _config = config;
            _detector = new Detector(config.MinWordLength, extraExceptions: config.Exceptions);
            _ignored = ToSet(config.IgnoredProcesses);
            _enabled = config.AutoSwitch;
        }
    }

    public void Start()
    {
        _worker = new Thread(WorkerLoop)
        {
            IsBackground = true,
            Name = "TypoSwitch.Jobs",
        };
        _worker.Start();
        _hookProc = HookCallback;
        _hook = Native.SetWindowsHookEx(Native.WhKeyboardLl, _hookProc, IntPtr.Zero, 0);
        if (_hook == IntPtr.Zero)
            throw new InvalidOperationException("Не удалось установить хук клавиатуры.");
    }

    public void ConvertLastWord()
    {
        Snapshot snapshot;
        lock (_sync)
            snapshot = TakeSnapshot();
        Enqueue(() => Hotkey(selection: false, snapshot));
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            Native.UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
        _jobs.CompleteAdding();
        _worker?.Join(500);
        _jobs.Dispose();
    }

    private void WorkerLoop()
    {
        foreach (var job in _jobs.GetConsumingEnumerable())
        {
            try { job(); }
            catch { /* never break the worker */ }
        }
    }

    private void Enqueue(Action job)
    {
        if (!_jobs.IsAddingCompleted)
            _jobs.Add(job);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode == Native.HcAction &&
            (wParam == Native.WmKeyDown || wParam == Native.WmSysKeyDown))
        {
            var kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var injected = (kb.flags & Native.LlkfInjected) != 0 || kb.dwExtraInfo == Native.MagicExtra;
            if (!injected && OnKey(kb))
                return 1;
        }
        return Native.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private bool OnKey(KBDLLHOOKSTRUCT kb)
    {
        lock (_sync)
        {
            if (kb.vkCode == Native.VkPause)
            {
                var selection = Native.ShiftDown();
                var snapshot = TakeSnapshot();
                Enqueue(() => Hotkey(selection, snapshot));
                return true;
            }

            if (Native.ModifierDown())
            {
                _buffer = "";
                return false;
            }

            var process = Native.ForegroundProcessName();
            if (process.Length > 0 && _ignored.Contains(process))
            {
                _buffer = "";
                return false;
            }

            if (kb.vkCode == Native.VkBack)
            {
                if (_buffer.Length > 0) _buffer = _buffer[..^1];
                else _committed = null;
                return false;
            }

            if (kb.vkCode == Native.VkEscape)
            {
                _buffer = "";
                _committed = null;
                return false;
            }

            var ch = Native.VkToChar(kb.vkCode, kb.scanCode);
            if (ch.Length == 0)
            {
                if (kb.vkCode is Native.VkTab or Native.VkReturn)
                    return FinishWord(kb.vkCode == Native.VkReturn ? "\n" : "\t");
                return false;
            }

            if (ch.Length == 1 && Layouts.Convertible.Contains(ch[0]))
            {
                _buffer += ch;
                _committed = null;
                return false;
            }

            if (ch is " " or "\t" or "\n" || kb.vkCode is Native.VkSpace or Native.VkTab or Native.VkReturn)
            {
                var delim = kb.vkCode == Native.VkReturn ? "\n" : kb.vkCode == Native.VkTab ? "\t" : ch;
                return FinishWord(delim);
            }

            return FinishWord(ch);
        }
    }

    private bool FinishWord(string delimiter)
    {
        var word = _buffer;
        _buffer = "";
        if (word.Length == 0)
        {
            _committed = null;
            return false;
        }

        if (!_enabled || !_config.AutoSwitch || Native.CapsOn())
        {
            _committed = new Committed(word, delimiter);
            return false;
        }

        var result = _detector.Analyze(word);
        if (!result.ShouldSwitch)
        {
            _committed = new Committed(word, delimiter);
            return false;
        }

        _committed = null;
        var converted = result.Converted;
        var sound = _config.Sound;
        Enqueue(() => Replace(word, converted, delimiter, sound));
        return true;
    }

    private Snapshot TakeSnapshot()
    {
        if (_buffer.Length > 0)
        {
            var snap = new Snapshot(_buffer, "", 0, true);
            _buffer = "";
            return snap;
        }
        if (_committed is { } committed)
        {
            var snap = new Snapshot(committed.Word, committed.Delimiter, committed.Delimiter.Length, true);
            _committed = null;
            return snap;
        }
        return new Snapshot("", "", 0, false);
    }

    private void Hotkey(bool selection, Snapshot snapshot)
    {
        if (selection || !snapshot.HasWord)
        {
            ConvertSelection();
            return;
        }

        if (snapshot.ExtraBackspaces > 0)
        {
            Thread.Sleep(20);
            Native.Backspace(snapshot.ExtraBackspaces);
        }

        Replace(snapshot.Word, Layouts.Invert(snapshot.Word), snapshot.Delimiter, _config.Sound);
    }

    private static void Replace(string old, string converted, string delimiter, bool beep)
    {
        Thread.Sleep(20);
        Native.Backspace(old.Length);
        Native.TypeText(converted);
        if (delimiter.Length > 0)
            Native.TypeText(delimiter);
        Native.SwitchToScript(converted);
        if (beep)
        {
            try { SystemSounds.Asterisk.Play(); }
            catch { /* ignore */ }
        }
    }

    private static void ConvertSelection()
    {
        Thread.Sleep(20);
        Native.SendCtrl(Native.VkC);
        Thread.Sleep(80);
        var text = Native.GetClipboardText();
        if (string.IsNullOrWhiteSpace(text)) return;
        var converted = Layouts.Invert(text);
        if (converted == text) return;
        Native.SetClipboardText(converted);
        Thread.Sleep(20);
        Native.SendCtrl(Native.VkV);
        Native.SwitchToScript(converted);
    }

    private static HashSet<string> ToSet(IEnumerable<string> items) =>
        items.Select(p => p.Trim().ToLowerInvariant()).Where(p => p.Length > 0).ToHashSet();

    private readonly record struct Committed(string Word, string Delimiter);
    private readonly record struct Snapshot(string Word, string Delimiter, int ExtraBackspaces, bool HasWord);
}
