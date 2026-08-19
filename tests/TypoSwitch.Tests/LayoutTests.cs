using TypoSwitch;
using Xunit;

namespace TypoSwitch.Tests;

public class LayoutTests
{
    [Fact]
    public void EnglishTypedAsRussian()
    {
        Assert.Equal("привет", Layouts.Invert("ghbdtn"));
        Assert.Equal("Привет", Layouts.Invert("Ghbdtn"));
        Assert.Equal("ПРИВЕТ", Layouts.Invert("GHBDTN"));
        Assert.Equal("спасибо", Layouts.Invert("cgfcb,j"));
    }

    [Fact]
    public void RussianTypedAsEnglish()
    {
        Assert.Equal("hello", Layouts.Invert("руддщ"));
        Assert.Equal("Hello", Layouts.Invert("Руддщ"));
        Assert.Equal("please", Layouts.Invert("здуфыу"));
    }

    [Fact]
    public void RoundtripLetters()
    {
        const string sample = "HelloПриветLayoutРаскладка";
        Assert.Equal(sample, Layouts.Invert(Layouts.Invert(sample)));
    }

    [Fact]
    public void MajorityInvertSelection()
    {
        Assert.Equal("привет спасибо", Layouts.MajorityInvert("ghbdtn cgfcb,j"));
        Assert.Equal("hello please", Layouts.MajorityInvert("руддщ здуфыу"));
    }
}
