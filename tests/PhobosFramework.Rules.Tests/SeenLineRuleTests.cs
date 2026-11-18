using System.Collections.Generic;
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

    [Fact]
    public void WhatItWasSeenOnItsCaseAndItsFiguresFollowTheDetail() => Assert.Equal(
        "{\"format\":1,\"time\":\"t\",\"key\":\"vehicles.grid-hook-up\",\"mod\":\"m\",\"version\":\"v\",\"detail\":\"Bradley: 960 W\","
        + "\"on\":\"Bradley412\",\"kind\":\"M2A3Bradley\",\"variant\":\"carrying loads\",\"values\":{\"W\":960.5,\"loadsA\":30,\"chargingA\":0.125}}",
        SeenLineRule.Line("t", "vehicles.grid-hook-up", "m", "v", "Bradley: 960 W", "Bradley412", "M2A3Bradley", "carrying loads",
            new[] { Pair("W", 960.5), Pair("loadsA", 30), Pair("chargingA", 0.125) }));

    [Fact]
    public void AFigureThatIsNotANumberIsLeftOut() => Assert.EndsWith("\"values\":{\"b\":1}}",
        SeenLineRule.Line("t", "k", "m", "v", null, values: new[] { Pair("a", double.NaN), Pair("b", 1), Pair("c", double.PositiveInfinity) }));

    [Fact]
    public void EmptyNewFieldsAreLeftOut()
    {
        string line = SeenLineRule.Line("t", "k", "m", "v", "d", "", null, "", new KeyValuePair<string, double>[0]);
        Assert.DoesNotContain("on", line.Replace("\"version\"", ""));
        Assert.DoesNotContain("values", line);
    }

    [Fact]
    public void ASightingIsNewPerKeyCaseAndThing()
    {
        string warning = SeenLineRule.Identity("vehicles.stage.turbineStart", "barely enough to light", "Abrams1");
        Assert.NotEqual(warning, SeenLineRule.Identity("vehicles.stage.turbineStart", "too weak to light", "Abrams1"));
        Assert.NotEqual(warning, SeenLineRule.Identity("vehicles.stage.turbineStart", "barely enough to light", "Abrams2"));
        Assert.Equal(warning, SeenLineRule.Identity("vehicles.stage.turbineStart", "barely enough to light", "Abrams1"));
        Assert.Equal(SeenLineRule.Identity("k", null, null), SeenLineRule.Identity("k", "", ""));
    }

    static KeyValuePair<string, double> Pair(string name, double value) => new(name, value);
}

public class TestRowsRuleTests
{
    static readonly string[] File =
    {
        "# written by scripts/deploy.py",
        "69\tThe battery's warnings\tvehicles.stage.turbineStart\tbarely enough to light\t",
        "121\tA thrown track\tvehicles.stopped-by-damage\t\t*track*",
        "140\tThe warning receiver\taircraft.warning-receiver\t\t",
        "broken line",
        "\tno row\tkey\t\t",
    };

    [Fact]
    public void LinesThatAreNotFiveFieldsWithARowAndAKeyAreSkipped() =>
        Assert.Equal(new[] { "69", "121", "140" }, TestRowsRule.Parse(File).ConvertAll(e => e.Row));

    [Fact]
    public void ACaseNarrowsTheMatch()
    {
        TestRowsRule.Entry row = TestRowsRule.Parse(File)[0];
        Assert.True(row.Matches("vehicles.stage.turbineStart", "barely enough to light", "Abrams: barely enough to light"));
        Assert.False(row.Matches("vehicles.stage.turbineStart", "too weak to light", null));
        Assert.False(row.Matches("vehicles.stage.startingPower", "barely enough to light", null));
    }

    [Fact]
    public void AWildcardOnTheDetailIgnoresCase()
    {
        TestRowsRule.Entry row = TestRowsRule.Parse(File)[1];
        Assert.True(row.Matches("vehicles.stopped-by-damage", null, "M1A2: Left TRACK thrown"));
        Assert.False(row.Matches("vehicles.stopped-by-damage", null, "L-ATV: Engine too badly damaged."));
    }

    [Fact]
    public void NoCaseOrDetailMatchesEveryNoteOfTheKey() =>
        Assert.True(TestRowsRule.Parse(File)[2].Matches("aircraft.warning-receiver", "LockedOn", null));

    [Theory]
    [InlineData("*track*", "a track here", true)]
    [InlineData("*: Tracked", "Harrier: Tracked", true)]
    [InlineData("*: Tracked", "Harrier: LockedOn", false)]
    [InlineData("a?c", "abc", true)]
    [InlineData("a?c", "ac", false)]
    [InlineData("*", "", true)]
    [InlineData("**x", "yx", true)]
    public void Wildcards(string pattern, string text, bool matches) => Assert.Equal(matches, TestRowsRule.Wildcard(pattern, text));
}
