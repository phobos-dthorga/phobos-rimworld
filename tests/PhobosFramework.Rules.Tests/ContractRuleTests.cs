using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class ContractRuleTests
{
    [Theory]
    [InlineData(0, 0.6f)]
    [InlineData(10, 1.0f)]
    [InlineData(20, 1.4f)]
    [InlineData(25, 1.4f)]
    [InlineData(-3, 0.6f)]
    public void ThePaceFollowsTheBestHandsSkill(int level, float expected) =>
        Assert.Equal(expected, ContractRule.Pace(level), 3);

    [Theory]
    [InlineData(1230f, 3000f, 50f, 1200f)]
    [InlineData(1230f, 800f, 50f, 800f)]
    [InlineData(1230f, 820f, 50f, 800f)]
    [InlineData(1230f, 40f, 50f, 0f)]
    [InlineData(37f, 100f, 1f, 37f)]
    [InlineData(0f, 100f, 1f, 0f)]
    public void TheAmountIsRoundAndFitsTheTruck(float wanted, float most, float step, float expected) =>
        Assert.Equal(expected, ContractRule.Amount(wanted, most, step), 3);

    [Theory]
    [InlineData(600f, 2.3f, 1380f)]
    [InlineData(0f, 2.3f, 0f)]
    [InlineData(10f, 0f, 0f)]
    public void TheValueIsTheUnitsAtTheirWorth(float units, float perUnit, float expected) =>
        Assert.Equal(expected, ContractRule.Value(units, perUnit), 3);

    [Theory]
    [InlineData(18f, false, 2)]
    [InlineData(9f, false, 1)]
    [InlineData(48f, true, 4)]
    [InlineData(0f, false, 0)]
    public void TheTimeLimitAllowsTheWorkAtTheSlowestPace(float hours, bool worksAtNight, int expected) =>
        Assert.Equal(expected, ContractRule.WorkDays(hours, worksAtNight));

    [Theory]
    [InlineData(1.1f, 3f, 1.0f)]
    [InlineData(1.13f, 3f, 1.25f)]
    [InlineData(0.1f, 3f, 0f)]
    [InlineData(3.4f, 3f, 3f)]
    public void TheFrequencyMovesInQuarters(float value, float most, float expected) =>
        Assert.Equal(expected, ContractRule.Frequency(value, most), 3);
}
