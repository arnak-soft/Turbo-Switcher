import unittest

from typoswitch.detector import Detector


class DetectorTests(unittest.TestCase):
    def setUp(self) -> None:
        self.detector = Detector(min_length=3)

    def test_fixes_russian_typed_in_english_layout(self) -> None:
        for word in ("ghbdtn", "cgfcb,j", "rjytxyj"):
            result = self.detector.analyze(word)
            self.assertTrue(result.should_switch, result)
            self.assertTrue(any("а" <= ch.lower() <= "я" or ch.lower() == "ё" for ch in result.converted), result)

    def test_fixes_english_typed_in_russian_layout(self) -> None:
        for word in ("руддщ", "здуфыу", "зкште"):
            if len(word) < 3:
                continue
            result = self.detector.analyze(word)
            self.assertTrue(result.should_switch, result)

    def test_keeps_correct_english(self) -> None:
        for word in ("hello", "thanks", "please", "keyboard", "print", "because"):
            result = self.detector.analyze(word)
            self.assertFalse(result.should_switch, result)

    def test_keeps_correct_russian(self) -> None:
        for word in ("привет", "спасибо", "пожалуйста", "клавиатура", "хорошо"):
            result = self.detector.analyze(word)
            self.assertFalse(result.should_switch, result)

    def test_exceptions_and_short_words(self) -> None:
        self.assertFalse(self.detector.should_switch("ok"))
        self.assertFalse(self.detector.should_switch("qwe"))
        self.assertFalse(self.detector.should_switch("lol"))
        self.assertFalse(self.detector.should_switch("github"))

    def test_comma_is_part_of_russian_word(self) -> None:
        result = self.detector.analyze("cgfcb,j")
        self.assertTrue(result.should_switch, result)
        self.assertEqual(result.converted, "спасибо")

    def test_digits_are_ignored(self) -> None:
        self.assertFalse(self.detector.should_switch("win10"))
        self.assertFalse(self.detector.should_switch("ghbdtn2"))


if __name__ == "__main__":
    unittest.main()
