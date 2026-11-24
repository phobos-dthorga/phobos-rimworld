using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class VersionLedgerRuleTests
{
    [Fact]
    public void ALineReadsBackAsWritten()
    {
        string line = VersionLedgerRule.Line(52261916, "D'thorga Framework", "0.287.0");
        Assert.Equal("52261916|D'thorga Framework|0.287.0", line);
        Assert.True(VersionLedgerRule.TryRead(line, out int tick, out string mod, out string version));
        Assert.Equal((52261916, "D'thorga Framework", "0.287.0"), (tick, mod, version));
    }

    [Fact]
    public void TheSeparatorInANameCannotBreakALine()
    {
        Assert.True(VersionLedgerRule.TryRead(VersionLedgerRule.Line(5, "A|B", "1|2"), out _, out string mod, out string version));
        Assert.Equal(("A/B", "1/2"), (mod, version));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12|only two")]
    [InlineData("soon|D'thorga Framework|0.1.0")]
    [InlineData("12||0.1.0")]
    public void AnythingElseIsNotALine(string text) => Assert.False(VersionLedgerRule.TryRead(text, out _, out _, out _));

    [Fact]
    public void ALineIsNeededOnlyWhenTheVersionChanged()
    {
        var lines = new[] { "10|D'thorga Framework|0.286.0", "20|D'thorga Medical|0.26.0", "30|D'thorga Framework|0.286.2" };
        Assert.Equal("0.286.2", VersionLedgerRule.LastVersion(lines, "D'thorga Framework"));
        Assert.False(VersionLedgerRule.NeedsLine(lines, "D'thorga Framework", "0.286.2"));
        Assert.True(VersionLedgerRule.NeedsLine(lines, "D'thorga Framework", "0.287.0"));
        Assert.True(VersionLedgerRule.NeedsLine(lines, "D'thorga Farming", "0.10.0"));
        Assert.Null(VersionLedgerRule.LastVersion(new string[0], "D'thorga Framework"));
    }
}
