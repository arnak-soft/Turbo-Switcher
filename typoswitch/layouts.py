"""QWERTY ↔ ЙЦУКЕН character maps (physical key positions)."""

from __future__ import annotations

EN_CHARS = "`qwertyuiop[]asdfghjkl;'zxcvbnm,./~QWERTYUIOP{}ASDFGHJKL:\"ZXCVBNM<>?"
RU_CHARS = "ёйцукенгшщзхъфывапролджэячсмитьбю.ЁЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮ,"

if len(EN_CHARS) != len(RU_CHARS):
    raise RuntimeError("Layout maps must be the same length")

EN_TO_RU = str.maketrans(EN_CHARS, RU_CHARS)
RU_TO_EN = str.maketrans(RU_CHARS, EN_CHARS)

LATIN = set("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ`[];',./~{}:\"<>?")
CYRILLIC = set("абвгдеёжзийклмнопрстуфхцчшщъыьэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ")

WORD_CHARS = LATIN | CYRILLIC | set("-")
CONVERTIBLE = set(EN_CHARS) | set(RU_CHARS) | set("-")


def is_latin(text: str) -> bool:
    letters = [c for c in text if c.isalpha()]
    return bool(letters) and all(c in LATIN for c in letters)


def is_cyrillic(text: str) -> bool:
    letters = [c for c in text if c.isalpha()]
    return bool(letters) and all(c in CYRILLIC for c in letters)


def invert_layout(text: str) -> str:
    """Convert by opposite layout. Mixed script is converted char by char."""
    out = []
    for ch in text:
        if ch in LATIN:
            out.append(ch.translate(EN_TO_RU))
        elif ch in CYRILLIC:
            out.append(ch.translate(RU_TO_EN))
        else:
            out.append(ch)
    return "".join(out)


def majority_invert(text: str) -> str:
    """Invert the whole string toward the opposite of the dominant script."""
    latin = sum(1 for c in text if c in LATIN)
    cyr = sum(1 for c in text if c in CYRILLIC)
    if cyr > latin:
        return text.translate(RU_TO_EN)
    return text.translate(EN_TO_RU)
