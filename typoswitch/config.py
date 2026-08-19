"""User configuration stored in %APPDATA%\\Typo Switcher\\config.json."""

from __future__ import annotations

import json
import os
from dataclasses import asdict, dataclass, field
from pathlib import Path


def appdata_dir() -> Path:
    root = os.environ.get("APPDATA") or str(Path.home() / "AppData" / "Roaming")
    path = Path(root) / "Typo Switcher"
    path.mkdir(parents=True, exist_ok=True)
    return path


def config_path() -> Path:
    return appdata_dir() / "config.json"


@dataclass
class Config:
    auto_switch: bool = True
    sound: bool = False
    min_word_length: int = 3
    convert_hotkey: str = "pause"
    convert_selection_hotkey: str = "shift+pause"
    exceptions: list[str] = field(default_factory=list)
    ignored_processes: list[str] = field(default_factory=list)

    def save(self, path: Path | None = None) -> Path:
        target = path or config_path()
        target.write_text(json.dumps(asdict(self), ensure_ascii=False, indent=2), encoding="utf-8")
        return target

    @classmethod
    def load(cls, path: Path | None = None) -> "Config":
        target = path or config_path()
        if not target.exists():
            cfg = cls()
            cfg.save(target)
            return cfg
        data = json.loads(target.read_text(encoding="utf-8"))
        known = {k: v for k, v in data.items() if k in cls.__dataclass_fields__}
        return cls(**known)
