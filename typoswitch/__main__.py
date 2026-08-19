"""CLI: run the tray app, or convert a word without installing a hook."""

from __future__ import annotations

import argparse
import logging
import sys

from . import __app_name__, __version__
from .config import Config
from .detector import Detector
from .layouts import invert_layout


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="typoswitch",
        description="Typo Switcher: исправляет текст, набранный не в той раскладке.",
    )
    parser.add_argument("--version", action="version", version=f"{__app_name__} {__version__}")
    sub = parser.add_subparsers(dest="command")

    conv = sub.add_parser("convert", help="Конвертировать строку EN↔RU без запуска хука")
    conv.add_argument("text", nargs="+", help="Текст, например ghbdtn")

    chk = sub.add_parser("check", help="Показать, сработало бы автоисправление")
    chk.add_argument("text", nargs="+", help="Слово для проверки")

    sub.add_parser("run", help="Запустить программу в трее (по умолчанию)")
    return parser


def main(argv: list[str] | None = None) -> int:
    if hasattr(sys.stdout, "reconfigure"):
        try:
            sys.stdout.reconfigure(encoding="utf-8")
            sys.stderr.reconfigure(encoding="utf-8")
        except Exception:
            pass
    parser = build_parser()
    args = parser.parse_args(argv)
    command = args.command or "run"

    if command == "convert":
        text = " ".join(args.text)
        print(invert_layout(text))
        return 0

    if command == "check":
        text = " ".join(args.text)
        cfg = Config.load()
        detector = Detector(min_length=cfg.min_word_length, extra_exceptions=set(cfg.exceptions))
        print(detector.analyze(text))
        return 0

    if sys.platform != "win32":
        print("Typo Switcher в режиме трея работает только на Windows.", file=sys.stderr)
        return 1

    logging.basicConfig(
        level=logging.INFO,
        format="%(asctime)s %(levelname)s %(message)s",
    )
    from .config import Config as Cfg
    from .engine import Engine
    from .tray import Tray

    config = Cfg.load()
    engine = Engine(config)
    tray = Tray(engine)
    tray.start()
    print(f"{__app_name__} запущен. Иконка в трее, Pause меняет последнее слово.")
    try:
        engine.run()
    except KeyboardInterrupt:
        engine.stop()
        tray.stop()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
