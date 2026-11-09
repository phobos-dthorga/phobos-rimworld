using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class FittingRuleTests
{
    [Fact]
    public void OnePartGivesTheWholeFactor() => Assert.Equal(1.2, FittingRule.PartShare(1.2, 1), 6);

    [Fact]
    public void NoPartsCountedStillGivesTheWholeFactor() => Assert.Equal(1.2, FittingRule.PartShare(1.2, 0), 6);

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(8)]
    public void FittingEveryPartGivesExactlyTheListedFactor(int parts)
    {
        double total = 1;
        for (int i = 0; i < parts; i++) total *= FittingRule.PartShare(1.15, parts);
        Assert.Equal(1.15, total, 6);
    }

    [Fact]
    public void OnePartOfSeveralGivesOnlyAShare()
    {
        double share = FittingRule.PartShare(1.5, 4);
        Assert.True(share > 1 && share < 1.5);
    }

    [Fact]
    public void AFactorBelowOneShrinksTheSameWay()
    {
        double share = FittingRule.PartShare(0.96, 2);
        Assert.True(share < 1 && share > 0.96);
    }

    [Fact]
    public void ANonsenseFactorGivesNothing() => Assert.Equal(0, FittingRule.PartShare(0, 3));

    [Fact]
    public void TheMinimumSkillIsEnough()
    {
        Assert.True(FittingRule.SkilledEnough(8, 8));
        Assert.True(FittingRule.SkilledEnough(20, 8));
        Assert.False(FittingRule.SkilledEnough(7, 8));
    }

    [Theory]
    [InlineData(100, 0.7, -30)]
    [InlineData(100, 1.3, 30)]
    [InlineData(100, 1, 0)]
    public void AGradeChangesMaximumHealthByTheDifference(double baseHealth, double factor, double expected) =>
        Assert.Equal(expected, FittingRule.GradeHealthModifier(baseHealth, factor), 6);

    [Fact]
    public void APoorerPartTakesWorseFaults()
    {
        Assert.Equal(0.3, FittingRule.FaultSeverity(0.2, 1.5), 6);
        Assert.Equal(0.12, FittingRule.FaultSeverity(0.2, 0.6), 6);
    }

    [Fact]
    public void ATuneScalesTheFuelShare() => Assert.Equal(1.15, FittingRule.TunedFuelShare(1, 1.15), 6);
}
