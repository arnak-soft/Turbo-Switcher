"""Load word lists shipped with the package."""

from __future__ import annotations

from functools import lru_cache
from pathlib import Path

DATA_DIR = Path(__file__).resolve().parent / "data"


def _read_words(name: str) -> set[str]:
    path = DATA_DIR / name
    words: set[str] = set()
    text = path.read_text(encoding="utf-8")
    for raw in text.splitlines():
        word = raw.strip().lower()
        if word and not word.startswith("#"):
            words.add(word)
            # also index the last token of multi-word phrases
            if " " in word:
                words.add(word.split()[-1])
    return words


@lru_cache(maxsize=1)
def russian_words() -> set[str]:
    return _read_words("ru_words.txt")


@lru_cache(maxsize=1)
def english_words() -> set[str]:
    return _read_words("en_words.txt")


@lru_cache(maxsize=1)
def exceptions() -> set[str]:
    return _read_words("exceptions.txt")
