"""Minimal Win32 helpers: keyboard hook, SendInput, clipboard, layout switch."""

from __future__ import annotations

import ctypes
from ctypes import wintypes
from typing import Callable

user32 = ctypes.WinDLL("user32", use_last_error=True)
kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)

ULONG_PTR = ctypes.c_ulonglong if ctypes.sizeof(ctypes.c_void_p) == 8 else ctypes.c_ulong
LRESULT = ctypes.c_ssize_t
HHOOK = wintypes.HANDLE
HKL = wintypes.HANDLE

WH_KEYBOARD_LL = 13
HC_ACTION = 0
WM_KEYDOWN = 0x0100
WM_SYSKEYDOWN = 0x0104
WM_INPUTLANGCHANGEREQUEST = 0x0050
LLKHF_INJECTED = 0x10
LLKHF_UP = 0x80

VK_BACK = 0x08
VK_TAB = 0x09
VK_RETURN = 0x0D
VK_SHIFT = 0x10
VK_CONTROL = 0x11
VK_MENU = 0x12  # Alt
VK_PAUSE = 0x13
VK_CAPITAL = 0x14
VK_ESCAPE = 0x1B
VK_SPACE = 0x20
VK_LWIN = 0x5B
VK_RWIN = 0x5C
VK_C = 0x43
VK_V = 0x56
VK_LSHIFT = 0xA0
VK_RSHIFT = 0xA1
VK_LCONTROL = 0xA2
VK_RCONTROL = 0xA3
VK_LMENU = 0xA4
VK_RMENU = 0xA5

INPUT_KEYBOARD = 1
KEYEVENTF_KEYUP = 0x0002
KEYEVENTF_UNICODE = 0x0004

CF_UNICODETEXT = 13
GMEM_MOVEABLE = 0x0002

LANG_EN = 0x0409
LANG_RU = 0x0419

# Marker so the hook ignores keys we inject ourselves.
MAGIC_EXTRA = 0x54795053  # 'TyPS'

HOOKPROC = ctypes.WINFUNCTYPE(LRESULT, ctypes.c_int, wintypes.WPARAM, wintypes.LPARAM)


class KBDLLHOOKSTRUCT(ctypes.Structure):
    _fields_ = [
        ("vkCode", wintypes.DWORD),
        ("scanCode", wintypes.DWORD),
        ("flags", wintypes.DWORD),
        ("time", wintypes.DWORD),
        ("dwExtraInfo", ULONG_PTR),
    ]


class KEYBDINPUT(ctypes.Structure):
    _fields_ = [
        ("wVk", wintypes.WORD),
        ("wScan", wintypes.WORD),
        ("dwFlags", wintypes.DWORD),
        ("time", wintypes.DWORD),
        ("dwExtraInfo", ULONG_PTR),
    ]


class INPUTUNION(ctypes.Union):
    _fields_ = [("ki", KEYBDINPUT)]


class INPUT(ctypes.Structure):
    _fields_ = [("type", wintypes.DWORD), ("union", INPUTUNION)]


user32.SetWindowsHookExW.argtypes = [ctypes.c_int, HOOKPROC, wintypes.HINSTANCE, wintypes.DWORD]
user32.SetWindowsHookExW.restype = HHOOK
user32.CallNextHookEx.argtypes = [HHOOK, ctypes.c_int, wintypes.WPARAM, wintypes.LPARAM]
user32.CallNextHookEx.restype = LRESULT
user32.UnhookWindowsHookEx.argtypes = [HHOOK]
user32.UnhookWindowsHookEx.restype = wintypes.BOOL
user32.GetMessageW.argtypes = [wintypes.LPMSG, wintypes.HWND, wintypes.UINT, wintypes.UINT]
user32.GetMessageW.restype = ctypes.c_int
user32.TranslateMessage.argtypes = [wintypes.LPMSG]
user32.DispatchMessageW.argtypes = [wintypes.LPMSG]
user32.PostThreadMessageW.argtypes = [wintypes.DWORD, wintypes.UINT, wintypes.WPARAM, wintypes.LPARAM]
user32.SendInput.argtypes = [wintypes.UINT, ctypes.POINTER(INPUT), ctypes.c_int]
user32.SendInput.restype = wintypes.UINT
user32.ToUnicodeEx.argtypes = [
    wintypes.UINT,
    wintypes.UINT,
    ctypes.POINTER(ctypes.c_ubyte),
    wintypes.LPWSTR,
    ctypes.c_int,
    wintypes.UINT,
    HKL,
]
user32.ToUnicodeEx.restype = ctypes.c_int
user32.GetKeyboardState.argtypes = [ctypes.POINTER(ctypes.c_ubyte)]
user32.GetKeyboardState.restype = wintypes.BOOL
user32.GetKeyState.argtypes = [ctypes.c_int]
user32.GetKeyState.restype = ctypes.c_short
user32.GetForegroundWindow.restype = wintypes.HWND
user32.GetWindowThreadProcessId.argtypes = [wintypes.HWND, ctypes.POINTER(wintypes.DWORD)]
user32.GetWindowThreadProcessId.restype = wintypes.DWORD
user32.GetKeyboardLayout.argtypes = [wintypes.DWORD]
user32.GetKeyboardLayout.restype = HKL
user32.GetKeyboardLayoutList.argtypes = [ctypes.c_int, ctypes.POINTER(HKL)]
user32.GetKeyboardLayoutList.restype = ctypes.c_int
user32.PostMessageW.argtypes = [wintypes.HWND, wintypes.UINT, wintypes.WPARAM, wintypes.LPARAM]
user32.PostMessageW.restype = wintypes.BOOL
user32.LoadKeyboardLayoutW.argtypes = [wintypes.LPCWSTR, wintypes.UINT]
user32.LoadKeyboardLayoutW.restype = HKL
kernel32.QueryFullProcessImageNameW.argtypes = [
    wintypes.HANDLE,
    wintypes.DWORD,
    wintypes.LPWSTR,
    ctypes.POINTER(wintypes.DWORD),
]
kernel32.QueryFullProcessImageNameW.restype = wintypes.BOOL
kernel32.OpenProcess.argtypes = [wintypes.DWORD, wintypes.BOOL, wintypes.DWORD]
kernel32.OpenProcess.restype = wintypes.HANDLE
kernel32.CloseHandle.argtypes = [wintypes.HANDLE]
kernel32.GetCurrentThreadId.restype = wintypes.DWORD
kernel32.GlobalAlloc.argtypes = [wintypes.UINT, ctypes.c_size_t]
kernel32.GlobalAlloc.restype = wintypes.HANDLE
kernel32.GlobalLock.argtypes = [wintypes.HANDLE]
kernel32.GlobalLock.restype = ctypes.c_void_p
kernel32.GlobalUnlock.argtypes = [wintypes.HANDLE]
user32.OpenClipboard.argtypes = [wintypes.HWND]
user32.OpenClipboard.restype = wintypes.BOOL
user32.CloseClipboard.restype = wintypes.BOOL
user32.EmptyClipboard.restype = wintypes.BOOL
user32.GetClipboardData.argtypes = [wintypes.UINT]
user32.GetClipboardData.restype = wintypes.HANDLE
user32.SetClipboardData.argtypes = [wintypes.UINT, wintypes.HANDLE]
user32.SetClipboardData.restype = wintypes.HANDLE

PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
WM_QUIT = 0x0012


class KeyboardEvent:
    __slots__ = ("vk", "scan", "flags", "injected", "extra")

    def __init__(self, kb: KBDLLHOOKSTRUCT) -> None:
        self.vk = int(kb.vkCode)
        self.scan = int(kb.scanCode)
        self.flags = int(kb.flags)
        self.injected = bool(kb.flags & LLKHF_INJECTED) or int(kb.dwExtraInfo) == MAGIC_EXTRA
        self.extra = int(kb.dwExtraInfo)


def install_hook(callback: Callable[[KeyboardEvent], bool]) -> tuple[HHOOK, object]:
    """Install a low-level keyboard hook.

    `callback` returns True to swallow the key. The HOOKPROC object must be
    kept alive for as long as the hook is installed.
    """

    def _handler(nCode: int, wParam: int, lParam: int) -> int:
        if nCode == HC_ACTION and wParam in (WM_KEYDOWN, WM_SYSKEYDOWN):
            kb = ctypes.cast(lParam, ctypes.POINTER(KBDLLHOOKSTRUCT)).contents
            event = KeyboardEvent(kb)
            if not event.injected:
                try:
                    if callback(event):
                        return 1
                except Exception:
                    pass
        return user32.CallNextHookEx(None, nCode, wParam, lParam)

    proc = HOOKPROC(_handler)
    hook = user32.SetWindowsHookExW(WH_KEYBOARD_LL, proc, None, 0)
    if not hook:
        raise ctypes.WinError(ctypes.get_last_error())
    return hook, proc


def uninstall_hook(hook: HHOOK) -> None:
    if hook:
        user32.UnhookWindowsHookEx(hook)


def message_loop() -> None:
    msg = wintypes.MSG()
    while user32.GetMessageW(ctypes.byref(msg), None, 0, 0) != 0:
        user32.TranslateMessage(ctypes.byref(msg))
        user32.DispatchMessageW(ctypes.byref(msg))


def current_thread_id() -> int:
    return int(kernel32.GetCurrentThreadId())


def quit_loop(thread_id: int | None = None) -> None:
    tid = thread_id if thread_id is not None else current_thread_id()
    user32.PostThreadMessageW(tid, WM_QUIT, 0, 0)


def key_down(vk: int) -> bool:
    return user32.GetKeyState(vk) < 0


def caps_on() -> bool:
    return bool(user32.GetKeyState(VK_CAPITAL) & 1)


def shift_down() -> bool:
    return key_down(VK_SHIFT) or key_down(VK_LSHIFT) or key_down(VK_RSHIFT)


def modifier_down() -> bool:
    return (
        key_down(VK_CONTROL)
        or key_down(VK_LCONTROL)
        or key_down(VK_RCONTROL)
        or key_down(VK_MENU)
        or key_down(VK_LMENU)
        or key_down(VK_RMENU)
        or key_down(VK_LWIN)
        or key_down(VK_RWIN)
    )


def current_layout() -> int:
    hwnd = user32.GetForegroundWindow()
    tid = user32.GetWindowThreadProcessId(hwnd, None)
    return int(user32.GetKeyboardLayout(tid) or 0)


def vk_to_char(vk: int, scan: int) -> str:
    state = (ctypes.c_ubyte * 256)()
    for i in range(256):
        ks = user32.GetKeyState(i)
        # High bit = currently down; low bit = toggle (Caps Lock).
        state[i] = ((ks >> 8) & 0x80) | (ks & 1)
    state[vk] = ctypes.c_ubyte(state[vk] | 0x80)
    buf = ctypes.create_unicode_buffer(8)
    hkl = current_layout()
    n = user32.ToUnicodeEx(vk, scan, state, buf, 8, 0, hkl)
    if n > 0:
        return buf.value[:n]
    return ""


def _send(inputs: list[INPUT]) -> None:
    arr = (INPUT * len(inputs))(*inputs)
    sent = user32.SendInput(len(inputs), arr, ctypes.sizeof(INPUT))
    if sent != len(inputs):
        raise ctypes.WinError(ctypes.get_last_error())


def _key_input(vk: int, up: bool = False) -> INPUT:
    flags = KEYEVENTF_KEYUP if up else 0
    inp = INPUT()
    inp.type = INPUT_KEYBOARD
    inp.union.ki = KEYBDINPUT(vk, 0, flags, 0, MAGIC_EXTRA)
    return inp


def _unicode_input(char: str, up: bool = False) -> INPUT:
    flags = KEYEVENTF_UNICODE | (KEYEVENTF_KEYUP if up else 0)
    inp = INPUT()
    inp.type = INPUT_KEYBOARD
    inp.union.ki = KEYBDINPUT(0, ord(char), flags, 0, MAGIC_EXTRA)
    return inp


def tap_vk(vk: int) -> None:
    _send([_key_input(vk, False), _key_input(vk, True)])


def type_text(text: str) -> None:
    events: list[INPUT] = []
    for ch in text:
        if ch == "\n":
            events.extend([_key_input(VK_RETURN, False), _key_input(VK_RETURN, True)])
        else:
            events.extend([_unicode_input(ch, False), _unicode_input(ch, True)])
    if events:
        _send(events)


def send_ctrl_key(vk: int) -> None:
    _send(
        [
            _key_input(VK_CONTROL, False),
            _key_input(vk, False),
            _key_input(vk, True),
            _key_input(VK_CONTROL, True),
        ]
    )


def backspace(count: int) -> None:
    for _ in range(max(0, count)):
        tap_vk(VK_BACK)


def _find_layout(lang_id: int) -> int | None:
    n = user32.GetKeyboardLayoutList(0, None)
    if n <= 0:
        return None
    buf = (HKL * n)()
    user32.GetKeyboardLayoutList(n, buf)
    for hkl in buf:
        if (int(hkl) & 0xFFFF) == lang_id:
            return int(hkl)
    return None


def switch_layout(lang_id: int) -> None:
    hkl = _find_layout(lang_id)
    if hkl is None:
        klid = f"{lang_id:08x}"
        user32.LoadKeyboardLayoutW(klid, 0)
        hkl = _find_layout(lang_id)
    if hkl is None:
        return
    hwnd = user32.GetForegroundWindow()
    user32.PostMessageW(hwnd, WM_INPUTLANGCHANGEREQUEST, 0, hkl)


def switch_to_script(text: str) -> None:
    for ch in text:
        if "а" <= ch.lower() <= "я" or ch.lower() == "ё":
            switch_layout(LANG_RU)
            return
        if "a" <= ch.lower() <= "z":
            switch_layout(LANG_EN)
            return


def foreground_process_name() -> str:
    hwnd = user32.GetForegroundWindow()
    pid = wintypes.DWORD()
    user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
    handle = kernel32.OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, False, pid.value)
    if not handle:
        return ""
    try:
        size = wintypes.DWORD(260)
        buf = ctypes.create_unicode_buffer(260)
        if kernel32.QueryFullProcessImageNameW(handle, 0, buf, ctypes.byref(size)):
            return buf.value.split("\\")[-1].lower()
        return ""
    finally:
        kernel32.CloseHandle(handle)


def get_clipboard() -> str:
    if not user32.OpenClipboard(None):
        return ""
    try:
        handle = user32.GetClipboardData(CF_UNICODETEXT)
        if not handle:
            return ""
        locked = kernel32.GlobalLock(handle)
        if not locked:
            return ""
        try:
            return ctypes.wstring_at(locked)
        finally:
            kernel32.GlobalUnlock(handle)
    finally:
        user32.CloseClipboard()


def set_clipboard(text: str) -> None:
    if not user32.OpenClipboard(None):
        raise ctypes.WinError(ctypes.get_last_error())
    try:
        user32.EmptyClipboard()
        encoded = text.encode("utf-16-le") + b"\x00\x00"
        handle = kernel32.GlobalAlloc(GMEM_MOVEABLE, len(encoded))
        if not handle:
            raise ctypes.WinError(ctypes.get_last_error())
        locked = kernel32.GlobalLock(handle)
        ctypes.memmove(locked, encoded, len(encoded))
        kernel32.GlobalUnlock(handle)
        if not user32.SetClipboardData(CF_UNICODETEXT, handle):
            raise ctypes.WinError(ctypes.get_last_error())
    finally:
        user32.CloseClipboard()
