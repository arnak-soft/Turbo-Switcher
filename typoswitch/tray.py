"""System tray icon and simple settings window."""

from __future__ import annotations

import os
import subprocess
import threading
import tkinter as tk
from tkinter import messagebox, ttk

import pystray

from . import __app_name__, __version__
from .config import Config, appdata_dir, config_path
from .engine import Engine
from .icon import make_icon


class Tray:
    def __init__(self, engine: Engine) -> None:
        self.engine = engine
        self.icon = pystray.Icon(
            __app_name__,
            make_icon(enabled=engine.enabled),
            f"{__app_name__} {__version__}",
            menu=self._menu(),
        )

    def start(self) -> None:
        if hasattr(self.icon, "run_detached"):
            self.icon.run_detached()
        else:
            threading.Thread(target=self.icon.run, daemon=True).start()

    def stop(self) -> None:
        try:
            self.icon.stop()
        except Exception:
            pass

    def _menu(self) -> pystray.Menu:
        return pystray.Menu(
            pystray.MenuItem(
                "Автоисправление",
                self._toggle,
                checked=lambda _: self.engine.enabled and self.engine.config.auto_switch,
            ),
            pystray.MenuItem("Сменить последнее слово (Pause)", self._convert),
            pystray.Menu.SEPARATOR,
            pystray.MenuItem("Настройки…", self._settings),
            pystray.MenuItem("Открыть папку настроек", self._open_folder),
            pystray.Menu.SEPARATOR,
            pystray.MenuItem("Выход", self._exit),
        )

    def _toggle(self, _icon=None, _item=None) -> None:
        cfg = self.engine.config
        cfg.auto_switch = not cfg.auto_switch
        self.engine.enabled = cfg.auto_switch
        cfg.save()
        self.icon.icon = make_icon(enabled=cfg.auto_switch)
        self.icon.title = f"{__app_name__} — {'вкл' if cfg.auto_switch else 'выкл'}"

    def _convert(self, _icon=None, _item=None) -> None:
        self.engine.convert_last_now()

    def _settings(self, _icon=None, _item=None) -> None:
        threading.Thread(target=open_settings, args=(self.engine,), daemon=True).start()

    def _open_folder(self, _icon=None, _item=None) -> None:
        os.startfile(appdata_dir())  # noqa: S606

    def _exit(self, _icon=None, _item=None) -> None:
        self.stop()
        self.engine.stop()


def open_settings(engine: Engine) -> None:
    cfg = engine.config
    root = tk.Tk()
    root.title(f"{__app_name__} — настройки")
    root.resizable(False, False)
    root.attributes("-topmost", True)

    auto = tk.BooleanVar(value=cfg.auto_switch)
    sound = tk.BooleanVar(value=cfg.sound)
    min_len = tk.IntVar(value=cfg.min_word_length)
    exceptions = tk.StringVar(value=", ".join(cfg.exceptions))
    ignored = tk.StringVar(value=", ".join(cfg.ignored_processes))

    pad = {"padx": 12, "pady": 6}
    ttk.Label(root, text="Typo Switcher сам чинит слово, если вы печатали не в той раскладке.").pack(**pad)
    ttk.Checkbutton(root, text="Автоматически исправлять раскладку", variable=auto).pack(anchor="w", **pad)
    ttk.Checkbutton(root, text="Звук при автоисправлении", variable=sound).pack(anchor="w", **pad)

    row = ttk.Frame(root)
    row.pack(fill="x", **pad)
    ttk.Label(row, text="Минимальная длина слова:").pack(side="left")
    ttk.Spinbox(row, from_=2, to=8, textvariable=min_len, width=5).pack(side="left", padx=8)

    ttk.Label(root, text="Исключения (через запятую):").pack(anchor="w", **pad)
    ttk.Entry(root, textvariable=exceptions, width=48).pack(fill="x", padx=12)

    ttk.Label(root, text="Не работать в процессах (chrome.exe, …):").pack(anchor="w", **pad)
    ttk.Entry(root, textvariable=ignored, width=48).pack(fill="x", padx=12)

    ttk.Label(
        root,
        text="Pause — сменить последнее слово\nShift+Pause — сменить выделенный текст",
    ).pack(anchor="w", **pad)

    def save() -> None:
        cfg.auto_switch = auto.get()
        cfg.sound = sound.get()
        cfg.min_word_length = int(min_len.get())
        cfg.exceptions = [p.strip() for p in exceptions.get().split(",") if p.strip()]
        cfg.ignored_processes = [p.strip().lower() for p in ignored.get().split(",") if p.strip()]
        cfg.save()
        engine.reload(cfg)
        engine.enabled = cfg.auto_switch
        messagebox.showinfo(__app_name__, f"Сохранено:\n{config_path()}")
        root.destroy()

    buttons = ttk.Frame(root)
    buttons.pack(fill="x", **pad)
    ttk.Button(buttons, text="Сохранить", command=save).pack(side="right")
    ttk.Button(buttons, text="Отмена", command=root.destroy).pack(side="right", padx=8)
    ttk.Button(buttons, text="Открыть JSON", command=lambda: subprocess.Popen(["notepad.exe", str(config_path())])).pack(
        side="left"
    )

    root.mainloop()
