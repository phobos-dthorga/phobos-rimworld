using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class SeenLineRuleTests
{
    [Fact]
    public void ALineHoldsEveryFieldInOrder() => Assert.Equal(
        "{\"format\":1,\"time\":\"2026-11-17T12:00:00Z\",\"key\":\"vehicles.field-refit\",\"mod\":\"D'thorga Framework\",\"version\":\"0.118.0\",\"detail\":\"M1A2: left track\"}",
        SeenLineRule.Line("2026-11-17T12:00:00Z", "vehicles.field-refit", "D'thorga Framework", "0.118.0", "M1A2: left track"));

    [Fact]
    public void NoDetailLeavesTheFieldOut() =>
        Assert.DoesNotContain("detail", SeenLineRule.Line("t", "k", "m", "v", null));

    [Fact]
    public void QuotesBackslashesAndLineBreaksAreEscaped() =>
        Assert.EndsWith("\"detail\":\"a \\\"b\\\" c\\\\d\\ne\"}", SeenLineRule.Line("t", "k", "m", "v", "a \"b\" c\\d\ne"));

    [Fact]
    public void OtherControlCharactersAreEscapedByNumber() =>
        Assert.EndsWith("\"detail\":\"\\u0001\"}", SeenLineRule.Line("t", "k", "m", "v", "\u0001"));

    [Fact]
    public void TheLineIsOneLine() => Assert.DoesNotContain("\n", SeenLineRule.Line("t", "k", "m", "v", "x\ny"));
}
