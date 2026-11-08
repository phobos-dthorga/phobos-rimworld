using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class GarageRuleTests
{
    [Theory]
    [InlineData(0, 3, 12, 0)]
    [InlineData(2, 3, 12, 6)]
    [InlineData(9, 3, 12, 12)]
    [InlineData(-1, 3, 12, 0)]
    public void BaysGrowWithStaffUpToTheLimit(int staff, int perPerson, int max, int expected) =>
        Assert.Equal(expected, GarageRule.Bays(staff, perPerson, max));

    [Fact]
    public void PointsComeFromTotalSkill() => Assert.Equal(50f, GarageRule.PointsPerCycle(25, 2f));

    [Fact]
    public void WellStockedRepairIsHeldBackOnlyByTheWorkAvailable()
    {
        GarageRepairBudget budget = GarageRule.Spend(needed: 500f, available: 100f, steelHeld: 100, componentsHeld: 10, 10f, 200f);
        Assert.Equal(100f, budget.Points);
        Assert.Equal(10, budget.Steel);
        Assert.Equal(1, budget.Components);
        Assert.Equal(GarageRepairLimit.None, budget.Limit);
    }

    [Fact]
    public void ARepairNeverSpendsMoreThanTheDamage()
    {
        GarageRepairBudget budget = GarageRule.Spend(needed: 15f, available: 100f, steelHeld: 100, componentsHeld: 10, 10f, 200f);
        Assert.Equal(15f, budget.Points);
        Assert.Equal(2, budget.Steel);
        Assert.Equal(1, budget.Components);
    }

    [Fact]
    public void RunningOutOfSteelLimitsTheCycleAndSaysSo()
    {
        GarageRepairBudget budget = GarageRule.Spend(needed: 500f, available: 100f, steelHeld: 3, componentsHeld: 10, 10f, 200f);
        Assert.Equal(30f, budget.Points);
        Assert.Equal(3, budget.Steel);
        Assert.Equal(GarageRepairLimit.Steel, budget.Limit);
    }

    [Fact]
    public void NoComponentsMeansNoRepairAndSaysSo()
    {
        GarageRepairBudget budget = GarageRule.Spend(needed: 500f, available: 100f, steelHeld: 100, componentsHeld: 0, 10f, 200f);
        Assert.Equal(0f, budget.Points);
        Assert.Equal(0, budget.Steel);
        Assert.Equal(0, budget.Components);
        Assert.Equal(GarageRepairLimit.Components, budget.Limit);
    }

    [Fact]
    public void AMaterialWithNoCostIsNotUsed()
    {
        GarageRepairBudget budget = GarageRule.Spend(needed: 50f, available: 100f, steelHeld: 0, componentsHeld: 0, 0f, 0f);
        Assert.Equal(50f, budget.Points);
        Assert.Equal(0, budget.Steel);
        Assert.Equal(0, budget.Components);
        Assert.Equal(GarageRepairLimit.None, budget.Limit);
    }
}
