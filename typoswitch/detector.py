"""Decide whether a typed word was entered in the wrong keyboard layout."""

from __future__ import annotations

from . import dictionary
from .layouts import invert_layout, is_cyrillic, is_latin

# Frequent bigrams. Unknown pairs score 0.
EN_BIGRAMS = {
    "th": 9, "he": 8, "in": 8, "er": 8, "an": 7, "re": 7, "on": 7, "at": 7,
    "en": 7, "nd": 7, "ti": 6, "es": 6, "or": 6, "te": 6, "of": 6, "ed": 6,
    "is": 6, "it": 6, "al": 6, "ar": 6, "st": 6, "to": 6, "nt": 6, "ng": 6,
    "se": 5, "ha": 5, "as": 5, "ou": 5, "io": 5, "le": 5, "ve": 5, "co": 5,
    "me": 5, "de": 5, "hi": 5, "ri": 5, "ro": 5, "ic": 5, "ne": 4, "ea": 4,
    "ra": 4, "ce": 4, "li": 4, "ch": 4, "ll": 4, "be": 4, "ma": 4, "si": 4,
    "om": 4, "ur": 4, "ca": 4, "el": 4, "ta": 4, "la": 4, "ns": 4, "ho": 4,
    "wh": 4, "tr": 3, "ss": 3, "un": 3, "qu": 3, "ck": 3, "gh": 2, "ly": 4,
}

RU_BIGRAMS = {
    "ст": 9, "но": 8, "ен": 8, "то": 8, "на": 8, "ни": 7, "ко": 7, "ра": 7,
    "во": 7, "ро": 7, "ан": 6, "ов": 8, "ер": 6, "ли": 7, "ор": 6, "го": 7,
    "ал": 6, "не": 8, "пр": 8, "по": 8, "ре": 7, "ка": 7, "ел": 6, "ть": 8,
    "ое": 6, "ой": 6, "ие": 6, "ия": 6, "ам": 5, "ом": 6, "ем": 6, "ет": 7,
    "ла": 6, "ло": 6, "ль": 6, "ск": 6, "тр": 6, "че": 6, "ши": 5, "жи": 5,
    "вы": 6, "за": 7, "от": 7, "об": 6, "со": 6, "до": 6, "мо": 6, "бо": 5,
    "да": 6, "та": 6, "те": 6, "ти": 6, "си": 5, "ми": 5, "ри": 6, "ви": 5,
    "дн": 5, "чн": 5, "жн": 5, "сн": 5, "пр": 8, "бл": 4, "кл": 4, "сл": 5,
}

EN_SUFFIXES = ("ing", "tion", "sion", "ness", "ment", "able", "ible", "ful", "ous", "ally", "ed", "ly", "er", "est")
RU_SUFFIXES = ("ого", "ему", "ами", "ями", "ить", "ать", "еть", "ение", "ения", "ский", "ться", "тся", "ешь", "ишь")


class Detector:
    def __init__(
        self,
        min_length: int = 3,
        margin: float = 2.5,
        extra_exceptions: set[str] | None = None,
    ) -> None:
        self.min_length = min_length
        self.margin = margin
        self.ru = dictionary.russian_words()
        self.en = dictionary.english_words()
        self.exceptions = dictionary.exceptions()
        if extra_exceptions:
            self.exceptions |= {w.lower() for w in extra_exceptions}

    def should_switch(self, word: str) -> bool:
        return self.analyze(word).should_switch

    def analyze(self, word: str) -> "Detection":
        cleaned = word.strip()
        letters = "".join(c for c in cleaned if c.isalpha())
        key = cleaned.lower()

        if len(letters) < self.min_length:
            return Detection(False, cleaned, cleaned, 0, 0, "too_short")
        if any(c.isdigit() for c in cleaned):
            return Detection(False, cleaned, cleaned, 0, 0, "has_digits")
        if key in self.exceptions or letters.lower() in self.exceptions:
            return Detection(False, cleaned, cleaned, 0, 0, "exception")
        if not (is_latin(letters) or is_cyrillic(letters)):
            return Detection(False, cleaned, cleaned, 0, 0, "mixed_script")

        converted = invert_layout(cleaned)
        original_score = self.score(cleaned)
        converted_score = self.score(converted)

        # Dictionary words in the original language are left alone.
        if key in self.en or key in self.ru:
            if converted.lower() not in self.en and converted.lower() not in self.ru:
                return Detection(False, cleaned, converted, original_score, converted_score, "known_word")
            if original_score >= converted_score:
                return Detection(False, cleaned, converted, original_score, converted_score, "known_word")

        if converted_score >= original_score + self.margin:
            return Detection(True, cleaned, converted, original_score, converted_score, "wrong_layout")
        return Detection(False, cleaned, converted, original_score, converted_score, "keep")

    def score(self, word: str) -> float:
        letters = "".join(c for c in word.lower() if c.isalpha())
        if not letters:
            return 0.0

        points = 0.0
        low = word.lower()
        if low in self.ru or letters in self.ru:
            points += 18 + min(len(letters), 8)
        if low in self.en or letters in self.en:
            points += 18 + min(len(letters), 8)

        if is_cyrillic(letters):
            points += self._bigram_score(letters, RU_BIGRAMS)
            points += self._suffix_bonus(letters, RU_SUFFIXES)
            if any(ch in letters for ch in "ыьъэюяёщ"):
                points += 1.5
        elif is_latin(letters):
            points += self._bigram_score(letters, EN_BIGRAMS)
            points += self._suffix_bonus(letters, EN_SUFFIXES)
            if any(ch in letters for ch in "wqjx"):
                points += 0.8

        return points

    @staticmethod
    def _bigram_score(letters: str, table: dict[str, int]) -> float:
        if len(letters) < 2:
            return 0.0
        total = 0.0
        for i in range(len(letters) - 1):
            total += table.get(letters[i : i + 2], 0)
        return total / (len(letters) - 1)

    @staticmethod
    def _suffix_bonus(letters: str, suffixes: tuple[str, ...]) -> float:
        for suffix in suffixes:
            if letters.endswith(suffix) and len(letters) > len(suffix) + 1:
                return 2.2
        return 0.0


class Detection:
    __slots__ = ("should_switch", "original", "converted", "original_score", "converted_score", "reason")

    def __init__(
        self,
        should_switch: bool,
        original: str,
        converted: str,
        original_score: float,
        converted_score: float,
        reason: str,
    ) -> None:
        self.should_switch = should_switch
        self.original = original
        self.converted = converted
        self.original_score = original_score
        self.converted_score = converted_score
        self.reason = reason

    def __repr__(self) -> str:
        return (
            f"Detection(switch={self.should_switch!s}, {self.original!r} -> {self.converted!r}, "
            f"scores={self.original_score:.1f}/{self.converted_score:.1f}, reason={self.reason})"
        )
