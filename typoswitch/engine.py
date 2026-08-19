"""Typing engine: buffers the last word and fixes the layout when needed."""

from __future__ import annotations

import logging
import queue
import threading
import time
from dataclasses import dataclass

from .config import Config
from .detector import Detector
from .layouts import CONVERTIBLE, invert_layout
from . import winapi as w

log = logging.getLogger("typoswitch")

DELIMITERS = set(" \t\n")


@dataclass
class Committed:
    word: str
    delimiter: str


class Engine:
    def __init__(self, config: Config) -> None:
        self.config = config
        self.detector = Detector(
            min_length=config.min_word_length,
            extra_exceptions=set(config.exceptions),
        )
        self.enabled = True
        self.buffer = ""
        self.committed: Committed | None = None
        self._jobs: queue.Queue = queue.Queue()
        self._hook = None
        self._proc = None
        self._worker: threading.Thread | None = None
        self._running = False
        self._loop_tid: int | None = None

    def reload(self, config: Config) -> None:
        self.config = config
        self.detector = Detector(
            min_length=config.min_word_length,
            extra_exceptions=set(config.exceptions),
        )

    def run(self) -> None:
        self._running = True
        self._loop_tid = w.current_thread_id()
        self._worker = threading.Thread(target=self._worker_loop, name="typoswitch-jobs", daemon=True)
        self._worker.start()
        try:
            self._hook, self._proc = w.install_hook(self._on_key)
            log.info("Keyboard hook installed")
            w.message_loop()
        finally:
            self.stop()

    def stop(self) -> None:
        if not self._running:
            return
        self._running = False
        if self._hook:
            w.uninstall_hook(self._hook)
            self._hook = None
        self._jobs.put(None)
        try:
            w.quit_loop(self._loop_tid)
        except Exception:
            pass

    def _worker_loop(self) -> None:
        while True:
            job = self._jobs.get()
            if job is None:
                return
            try:
                job()
            except Exception:
                log.exception("Job failed")

    def _on_key(self, event: w.KeyboardEvent) -> bool:
        if event.vk == w.VK_PAUSE:
            shift = w.shift_down()
            self._jobs.put(lambda s=shift: self._hotkey(selection=s))
            return True

        if w.modifier_down():
            self.buffer = ""
            return False

        process = w.foreground_process_name()
        if process and process in {p.lower() for p in self.config.ignored_processes}:
            self.buffer = ""
            return False

        if event.vk == w.VK_BACK:
            if self.buffer:
                self.buffer = self.buffer[:-1]
            else:
                self.committed = None
            return False

        if event.vk == w.VK_ESCAPE:
            self.buffer = ""
            self.committed = None
            return False

        char = w.vk_to_char(event.vk, event.scan)
        if not char:
            if event.vk in (w.VK_TAB, w.VK_RETURN):
                return self._finish_word("\n" if event.vk == w.VK_RETURN else "\t")
            return False

        if char in CONVERTIBLE:
            self.buffer += char
            self.committed = None
            return False

        if char in DELIMITERS or event.vk in (w.VK_SPACE, w.VK_TAB, w.VK_RETURN):
            delim = "\n" if event.vk == w.VK_RETURN else ("\t" if event.vk == w.VK_TAB else char)
            return self._finish_word(delim)

        return self._finish_word(char)

    def _finish_word(self, delimiter: str) -> bool:
        word = self.buffer
        self.buffer = ""
        if not word:
            self.committed = None
            return False

        if not self.enabled or not self.config.auto_switch or w.caps_on():
            self.committed = Committed(word, delimiter)
            return False

        result = self.detector.analyze(word)
        if not result.should_switch:
            self.committed = Committed(word, delimiter)
            return False

        converted = result.converted
        self.committed = None
        self._jobs.put(lambda: self._replace(word, converted, delimiter, beep=True))
        return True

    def _hotkey(self, selection: bool) -> None:
        if selection or not self.buffer and not self.committed:
            self._convert_selection()
            return
        if self.buffer:
            word = self.buffer
            delim = ""
            self.buffer = ""
        else:
            assert self.committed is not None
            word = self.committed.word
            delim = self.committed.delimiter
            extra = len(delim)
            self.committed = None
            time.sleep(0.02)
            w.backspace(extra)
        converted = invert_layout(word)
        self._replace(word, converted, delim, beep=self.config.sound)

    def _replace(self, old: str, new: str, delimiter: str, beep: bool = False) -> None:
        time.sleep(0.02)
        w.backspace(len(old))
        w.type_text(new)
        if delimiter:
            w.type_text(delimiter)
        w.switch_to_script(new)
        if beep and self.config.sound:
            try:
                import winsound

                winsound.MessageBeep(-1)
            except Exception:
                pass
        log.info("Converted %r -> %r", old, new)

    def _convert_selection(self) -> None:
        time.sleep(0.02)
        w.send_ctrl_key(w.VK_C)
        time.sleep(0.08)
        text = w.get_clipboard()
        if not text.strip():
            return
        converted = invert_layout(text)
        if converted == text:
            return
        w.set_clipboard(converted)
        time.sleep(0.02)
        w.send_ctrl_key(w.VK_V)
        w.switch_to_script(converted)
        log.info("Converted selection (%s chars)", len(text))

    def convert_last_now(self) -> None:
        self._jobs.put(lambda: self._hotkey(selection=False))
