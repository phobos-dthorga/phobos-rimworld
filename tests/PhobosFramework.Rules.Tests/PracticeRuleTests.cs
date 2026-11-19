using System.Collections.Generic;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class PracticeRuleTests
{
    [Fact]
    public void NoHoursGiveNothing() => Assert.Equal(0, PracticeRule.Share(0));

    [Fact]
    public void HalfTheGainAtTheHalfHours() => Assert.Equal(0.5, PracticeRule.Share(PracticeRule.HalfHours), 6);

    [Fact]
    public void ThreeQuartersAtTwiceThat() => Assert.Equal(0.75, PracticeRule.Share(2 * PracticeRule.HalfHours), 6);

    [Fact]
    public void NotQuiteAllAfterTenHalves() => Assert.Equal(1 - 1.0 / 1024, PracticeRule.Share(10 * PracticeRule.HalfHours), 9);

    [Fact]
    public void NegativeHoursGiveNothing() => Assert.Equal(0, PracticeRule.Share(-5));

    [Fact]
    public void ANewHandsFactorIsOne() => Assert.Equal(1, PracticeRule.Factor(0, 0.85), 6);

    [Fact]
    public void ASeasonedHandReachesTheBestFactor() => Assert.Equal(0.85, PracticeRule.Factor(1, 0.85), 6);

    [Fact]
    public void HalfwayIsHalfTheGain() => Assert.Equal(0.925, PracticeRule.Factor(0.5, 0.85), 6);

    [Fact]
    public void TheRegularHasTheMostHours()
    {
        var hours = new Dictionary<string, float> { ["a"] = 20f, ["b"] = 30f, ["c"] = 5f };
        Assert.Equal("b", PracticeRule.Regular(hours));
    }

    [Fact]
    public void NobodyIsRegularBelowTheThreshold()
    {
        var hours = new Dictionary<string, float> { ["a"] = 3f, ["b"] = 11f };
        Assert.Null(PracticeRule.Regular(hours));
    }

    [Fact]
    public void ATieGoesToTheFirstKey()
    {
        var hours = new Dictionary<string, float> { ["z"] = 20f, ["m"] = 20f };
        Assert.Equal("m", PracticeRule.Regular(hours));
    }
}
