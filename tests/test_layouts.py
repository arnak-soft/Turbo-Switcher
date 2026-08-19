import unittest

from typoswitch.layouts import invert_layout, majority_invert


class LayoutTests(unittest.TestCase):
    def test_english_typed_as_russian(self) -> None:
        self.assertEqual(invert_layout("ghbdtn"), "привет")
        self.assertEqual(invert_layout("Ghbdtn"), "Привет")
        self.assertEqual(invert_layout("GHBDTN"), "ПРИВЕТ")
        self.assertEqual(invert_layout("cgfcb,j"), "спасибо")

    def test_russian_typed_as_english(self) -> None:
        self.assertEqual(invert_layout("руддщ"), "hello")
        self.assertEqual(invert_layout("Руддщ"), "Hello")
        self.assertEqual(invert_layout("здуфыу"), "please")

    def test_roundtrip_letters(self) -> None:
        sample = "HelloПриветLayoutРаскладка"
        self.assertEqual(invert_layout(invert_layout(sample)), sample)

    def test_selection_majority(self) -> None:
        self.assertEqual(majority_invert("ghbdtn cgfcb,j"), "привет спасибо")
        self.assertEqual(majority_invert("руддщ здуфыу"), "hello please")


if __name__ == "__main__":
    unittest.main()
