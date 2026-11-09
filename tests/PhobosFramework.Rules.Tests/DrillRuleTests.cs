using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class DrillRuleTests
{
    [Fact]
    public void AtTheGamesRateTheOddsAreTheGames() => Assert.Equal(30f, DrillRule.InsectMtbDays(30f, 1f));

    [Fact]
    public void TenTimesFasterMeetsInsectsTenTimesSooner() => Assert.Equal(3f, DrillRule.InsectMtbDays(30f, 10f));

    [Fact]
    public void AStoppedRigKeepsTheGamesFigure() => Assert.Equal(30f, DrillRule.InsectMtbDays(30f, 0f));

    [Fact]
    public void TheHammerIsFasterOnStoneThanOnOre()
    {
        const float hammerOre = 18f;
        Assert.Equal(30f, DrillRule.StoneRate(hammerOre, 1.667f), 1);
        Assert.True(DrillRule.StoneRate(hammerOre, 1.667f) > hammerOre);
    }
}
