using TypoSwitch;
using Xunit;

namespace TypoSwitch.Tests;

public class DetectorTests
{
    private readonly Detector _detector = new();

    [Theory]
    [InlineData("ghbdtn")]
    [InlineData("cgfcb,j")]
    [InlineData("rjytxyj")]
    public void FixesRussianTypedInEnglishLayout(string word)
    {
        var result = _detector.Analyze(word);
        Assert.True(result.ShouldSwitch, result.ToString());
        Assert.Contains(result.Converted, ch => ch is >= 'а' and <= 'я' or 'ё' or >= 'А' and <= 'Я' or 'Ё');
    }

    [Theory]
    [InlineData("руддщ")]
    [InlineData("здуфыу")]
    [InlineData("зкште")]
    public void FixesEnglishTypedInRussianLayout(string word)
    {
        Assert.True(_detector.ShouldSwitch(word), _detector.Analyze(word).ToString());
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("thanks")]
    [InlineData("please")]
    [InlineData("keyboard")]
    [InlineData("print")]
    [InlineData("because")]
    public void KeepsCorrectEnglish(string word) => Assert.False(_detector.ShouldSwitch(word), _detector.Analyze(word).ToString());

    [Theory]
    [InlineData("привет")]
    [InlineData("спасибо")]
    [InlineData("пожалуйста")]
    [InlineData("клавиатура")]
    [InlineData("хорошо")]
    public void KeepsCorrectRussian(string word) => Assert.False(_detector.ShouldSwitch(word), _detector.Analyze(word).ToString());

    [Fact]
    public void ExceptionsAndShortWords()
    {
        Assert.False(_detector.ShouldSwitch("ok"));
        Assert.False(_detector.ShouldSwitch("qwe"));
        Assert.False(_detector.ShouldSwitch("lol"));
        Assert.False(_detector.ShouldSwitch("github"));
    }

    [Fact]
    public void CommaIsPartOfRussianWord()
    {
        var result = _detector.Analyze("cgfcb,j");
        Assert.True(result.ShouldSwitch, result.ToString());
        Assert.Equal("спасибо", result.Converted);
    }

    [Fact]
    public void DigitsAreIgnored()
    {
        Assert.False(_detector.ShouldSwitch("win10"));
        Assert.False(_detector.ShouldSwitch("ghbdtn2"));
    }
}
