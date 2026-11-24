using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class ParachuteRuleTests
{
    [Fact]
    public void TheHandbooksWorkedExampleDrifts315Metres()
    {
        // The pathfinder handbook: K 3.0, 1,500 ft (A = 15), 7 kt: 315 m.
        Assert.Equal(315, ParachuteRule.DriftMetres(3.0, 1500, 7), 6);
        Assert.Equal(0, ParachuteRule.DriftMetres(3.0, 1500, 0), 6);
        Assert.Equal(0, ParachuteRule.DriftMetres(3.0, -10, 7), 6);
    }

    [Fact]
    public void AJumpFromAThousandFeetTakesAboutAMinute()
    {
        Assert.Equal(52.6, ParachuteRule.DescentSeconds(1000, ParachuteRule.Person.DescentFeetPerSecond), 1);
        Assert.Equal(0, ParachuteRule.DescentSeconds(0, 19), 6);
    }

    [Fact]
    public void EachLoadHasItsOwnFigures()
    {
        Assert.Equal(1000, ParachuteRule.For(ParachuteLoad.Person).PlannedFeet);
        Assert.Equal(600, ParachuteRule.For(ParachuteLoad.Bundle).PlannedFeet);
        Assert.Equal(1100, ParachuteRule.For(ParachuteLoad.Vehicle).PlannedFeet);
        Assert.True(ParachuteRule.Person.MinimumFeet < ParachuteRule.Person.PlannedFeet);
        Assert.True(ParachuteRule.Bundle.MinimumFeet < ParachuteRule.Bundle.PlannedFeet);
        Assert.True(ParachuteRule.Vehicle.MinimumFeet < ParachuteRule.Vehicle.PlannedFeet);
    }

    [Fact]
    public void AnInjuryIsRareInGoodConditions()
    {
        double day = ParachuteRule.InjuryChance(false, 3, 13, false, false, 1000, 800, false);
        Assert.Equal(0.0053, day, 6);
        double night = ParachuteRule.InjuryChance(true, 3, 13, false, false, 1000, 800, false);
        Assert.Equal(0.0053 * 1.76, night, 6);
    }

    [Fact]
    public void WindRaisesTheRiskInBands()
    {
        Assert.Equal(1, ParachuteRule.WindFactor(5, 13), 6);
        Assert.Equal(1.55, ParachuteRule.WindFactor(8, 13), 6);
        Assert.Equal(1.88, ParachuteRule.WindFactor(12, 13), 6);
        Assert.Equal(1.88 + 0.15 * 7, ParachuteRule.WindFactor(20, 13), 6);
    }

    [Fact]
    public void TooLowRaisesTheRiskUpToTenTimes()
    {
        Assert.Equal(1, ParachuteRule.LowFactor(1000, 800), 6);
        Assert.Equal(10, ParachuteRule.LowFactor(150, 800), 6);
        Assert.Equal(10, ParachuteRule.LowFactor(50, 800), 6);
        Assert.True(ParachuteRule.Opens(150));
        Assert.False(ParachuteRule.Opens(149));
    }

    [Fact]
    public void TheChanceNeverPassesOne()
    {
        Assert.Equal(1, ParachuteRule.InjuryChance(true, 60, 13, true, true, 160, 800, true), 6);
    }

    [Fact]
    public void ScatterGrowsWithTheDrift()
    {
        Assert.Equal(1.5, ParachuteRule.ScatterCells(0), 6);
        Assert.Equal(4.5, ParachuteRule.ScatterCells(30), 6);
    }
}
