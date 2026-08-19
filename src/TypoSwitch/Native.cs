using System.Runtime.InteropServices;
using System.Text;

namespace TypoSwitch;

internal static class Native
{
    public const int WhKeyboardLl = 13;
    public const int HcAction = 0;
    public const int WmKeyDown = 0x0100;
    public const int WmSysKeyDown = 0x0104;
    public const int WmInputLangChangeRequest = 0x0050;

    public const uint LlkfInjected = 0x10;
    public const uint MagicExtra = 0x54795053;

    public const int VkBack = 0x08;
    public const int VkTab = 0x09;
    public const int VkReturn = 0x0D;
    public const int VkShift = 0x10;
    public const int VkControl = 0x11;
    public const int VkMenu = 0x12;
    public const int VkPause = 0x13;
    public const int VkScroll = 0x91;
    public const int VkCapital = 0x14;
    public const int VkEscape = 0x1B;
    public const int VkSpace = 0x20;
    public const int VkLwin = 0x5B;
    public const int VkRwin = 0x5C;
    public const int VkC = 0x43;
    public const int VkV = 0x56;

    public const uint InputKeyboard = 1;
    public const uint KeyeventfKeyup = 0x0002;
    public const uint KeyeventfUnicode = 0x0004;
    public const uint UnicodeText = 13;
    public const uint GmemMoveable = 0x0002;
    public const ushort LangEn = 0x0409;
    public const ushort LangRu = 0x0419;
    public const uint ProcessQueryLimited = 0x1000;

    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    public static extern short GetKeyState(int nVirtKey);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    public static extern int GetKeyboardLayoutList(int nBuff, [Out] IntPtr[]? lpList);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out] char[] pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    public static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll")]
    public static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    public static extern bool EmptyClipboard();

    [DllImport("user32.dll")]
    public static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll")]
    public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

    [DllImport("kernel32.dll")]
    public static extern IntPtr GlobalAlloc(uint uFlags, nuint dwBytes);

    [DllImport("kernel32.dll")]
    public static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    public static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    public static extern IntPtr GlobalFree(IntPtr hMem);

    public static bool KeyDown(int vk) => GetKeyState(vk) < 0;

    public static bool CapsOn() => (GetKeyState(VkCapital) & 1) != 0;

    public static bool ShiftDown() => KeyDown(VkShift);

    public static bool ModifierDown() =>
        KeyDown(VkControl) || KeyDown(VkMenu) || KeyDown(VkLwin) || KeyDown(VkRwin);

    public static IntPtr CurrentLayout()
    {
        var hwnd = GetForegroundWindow();
        var tid = GetWindowThreadProcessId(hwnd, out _);
        return GetKeyboardLayout(tid);
    }

    public static string VkToChar(uint vk, uint scan)
    {
        var state = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            var ks = GetKeyState(i);
            state[i] = (byte)(((ks >> 8) & 0x80) | (ks & 1));
        }
        state[vk] |= 0x80;
        var buffer = new char[8];
        var n = ToUnicodeEx(vk, scan, state, buffer, buffer.Length, 0, CurrentLayout());
        return n > 0 ? new string(buffer, 0, n) : "";
    }

    public static void TapVk(ushort vk)
    {
        Send(Key(vk, false), Key(vk, true));
    }

    public static void TypeText(string text)
    {
        if (text.Length == 0) return;
        var events = new List<INPUT>(text.Length * 2);
        foreach (var ch in text)
        {
            if (ch == '\n')
            {
                events.Add(Key(VkReturn, false));
                events.Add(Key(VkReturn, true));
            }
            else
            {
                events.Add(Unicode(ch, false));
                events.Add(Unicode(ch, true));
            }
        }
        Send(events.ToArray());
    }

    public static void SendCtrl(ushort vk)
    {
        Send(
            Key(VkControl, false),
            Key(vk, false),
            Key(vk, true),
            Key(VkControl, true));
    }

    public static void Backspace(int count)
    {
        for (var i = 0; i < count; i++)
            TapVk(VkBack);
    }

    public static void SwitchToScript(string text)
    {
        foreach (var ch in text)
        {
            var lower = char.ToLowerInvariant(ch);
            if (lower is >= 'а' and <= 'я' or 'ё')
            {
                SwitchLayout(LangRu);
                return;
            }
            if (lower is >= 'a' and <= 'z')
            {
                SwitchLayout(LangEn);
                return;
            }
        }
    }

    public static void SwitchLayout(ushort langId)
    {
        var hkl = FindLayout(langId);
        if (hkl == IntPtr.Zero)
        {
            LoadKeyboardLayout(langId.ToString("x8"), 0);
            hkl = FindLayout(langId);
        }
        if (hkl == IntPtr.Zero) return;
        PostMessage(GetForegroundWindow(), WmInputLangChangeRequest, IntPtr.Zero, hkl);
    }

    public static string ForegroundProcessName()
    {
        var hwnd = GetForegroundWindow();
        GetWindowThreadProcessId(hwnd, out var pid);
        var handle = OpenProcess(ProcessQueryLimited, false, pid);
        if (handle == IntPtr.Zero) return "";
        try
        {
            var size = 260;
            var buffer = new StringBuilder(size);
            return QueryFullProcessImageName(handle, 0, buffer, ref size)
                ? Path.GetFileName(buffer.ToString()).ToLowerInvariant()
                : "";
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    public static string GetClipboardText()
    {
        if (!OpenClipboard(IntPtr.Zero)) return "";
        try
        {
            var handle = GetClipboardData(UnicodeText);
            if (handle == IntPtr.Zero) return "";
            var locked = GlobalLock(handle);
            if (locked == IntPtr.Zero) return "";
            try
            {
                return Marshal.PtrToStringUni(locked) ?? "";
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    public static void SetClipboardText(string text)
    {
        if (!OpenClipboard(IntPtr.Zero)) return;
        try
        {
            EmptyClipboard();
            var bytes = Encoding.Unicode.GetBytes(text + '\0');
            var handle = GlobalAlloc(GmemMoveable, (nuint)bytes.Length);
            if (handle == IntPtr.Zero) return;
            var locked = GlobalLock(handle);
            Marshal.Copy(bytes, 0, locked, bytes.Length);
            GlobalUnlock(handle);
            if (SetClipboardData(UnicodeText, handle) == IntPtr.Zero)
                GlobalFree(handle);
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static IntPtr FindLayout(ushort langId)
    {
        var count = GetKeyboardLayoutList(0, null);
        if (count <= 0) return IntPtr.Zero;
        var list = new IntPtr[count];
        GetKeyboardLayoutList(count, list);
        foreach (var hkl in list)
        {
            if ((hkl.ToInt64() & 0xFFFF) == langId)
                return hkl;
        }
        return IntPtr.Zero;
    }

    private static INPUT Key(int vk, bool up) => new()
    {
        Type = InputKeyboard,
        Union = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = (ushort)vk,
                dwFlags = up ? KeyeventfKeyup : 0,
                dwExtraInfo = MagicExtra,
            },
        },
    };

    private static INPUT Unicode(char ch, bool up) => new()
    {
        Type = InputKeyboard,
        Union = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wScan = ch,
                dwFlags = KeyeventfUnicode | (up ? KeyeventfKeyup : 0),
                dwExtraInfo = MagicExtra,
            },
        },
    };

    private static void Send(params INPUT[] inputs)
    {
        if (inputs.Length == 0) return;
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct KBDLLHOOKSTRUCT
{
    public uint vkCode;
    public uint scanCode;
    public uint flags;
    public uint time;
    public nuint dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct INPUT
{
    public uint Type;
    public InputUnion Union;
}

[StructLayout(LayoutKind.Explicit)]
internal struct InputUnion
{
    [FieldOffset(0)] public MOUSEINPUT mi;
    [FieldOffset(0)] public KEYBDINPUT ki;
}

[StructLayout(LayoutKind.Sequential)]
internal struct KEYBDINPUT
{
    public ushort wVk;
    public ushort wScan;
    public uint dwFlags;
    public uint time;
    public nuint dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MOUSEINPUT
{
    public int dx;
    public int dy;
    public uint mouseData;
    public uint dwFlags;
    public uint time;
    public nuint dwExtraInfo;
}
