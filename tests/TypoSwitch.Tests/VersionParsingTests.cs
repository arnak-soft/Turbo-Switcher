using TypoSwitch;
using Xunit;

namespace TypoSwitch.Tests;

public class VersionParsingTests
{
    [Theory]
    [InlineData("v1.2.0", 1, 2, 0)]
    [InlineData("1.0.0", 1, 0, 0)]
    [InlineData("V2.3.4", 2, 3, 4)]
    public void TryParseTag_parses_release_tags(string tag, int major, int minor, int build)
    {
        Assert.True(VersionParsing.TryParseTag(tag, out var version));
        Assert.Equal(new Version(major, minor, build), version);
    }

    [Fact]
    public void IsNewer_detects_newer_release()
    {
        var latest = new Version(1, 1, 0);
        var current = new Version(1, 0, 0);
        Assert.True(VersionParsing.IsNewer(latest, current));
        Assert.False(VersionParsing.IsNewer(current, latest));
        Assert.False(VersionParsing.IsNewer(current, current));
    }
}
