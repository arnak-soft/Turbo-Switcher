using System.Diagnostics;

namespace TypoSwitch;

internal static class SingleInstance
{
    private const string MutexName = @"Local\TurboSwitcher.SingleInstance.v1";
    private const string WindowTitle = "TurboSwitcher.SingleInstance.hwnd";

    private static readonly int ActivateMessage = (int)Native.RegisterWindowMessage("TurboSwitcher.Activate");
    private static Mutex? _mutex;
    private static HiddenWindow? _window;

    public static bool TryLock()
    {
        try
        {
            _mutex = new Mutex(true, MutexName, out var created);
            if (created) return true;
            if (_mutex.WaitOne(TimeSpan.Zero, true)) return true;
        }
        catch (AbandonedMutexException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return !OtherProcessExists();
        }

        return false;
    }

    public static void Listen(Action onActivate)
    {
        _window = new HiddenWindow(onActivate);
    }

    public static bool ActivateExisting()
    {
        var hwnd = Native.FindWindow(null, WindowTitle);
        if (hwnd == IntPtr.Zero) return false;
        Native.PostMessage(hwnd, ActivateMessage, IntPtr.Zero, IntPtr.Zero);
        return true;
    }

    private static bool OtherProcessExists()
    {
        var self = Process.GetCurrentProcess().Id;
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.Id == self) continue;
                var name = process.ProcessName;
                if (name.StartsWith("TurboSwitcher", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("TurboSwitch", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("TypoSwitch", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch
            {
                // процесс без доступа пропускаем
            }
            finally
            {
                process.Dispose();
            }
        }

        return false;
    }

    private sealed class HiddenWindow : NativeWindow
    {
        private readonly Action _onActivate;

        public HiddenWindow(Action onActivate)
        {
            _onActivate = onActivate;
            CreateHandle(new CreateParams
            {
                Caption = WindowTitle,
                Style = unchecked((int)0x80000000),
            });
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == ActivateMessage)
                _onActivate();
            base.WndProc(ref m);
        }
    }
}
