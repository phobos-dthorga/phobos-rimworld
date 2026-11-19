using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class OverrunRuleTests
{
    [Fact]
    public void AStandingVehicleRollsNowhere() => Assert.Equal(0, OverrunRule.Cells(0, 5000));

    [Fact]
    public void AFiveTonneTruckAtTenCellsASecondRollsSix() => Assert.Equal(6, OverrunRule.Cells(10, 5000));

    [Fact]
    public void AHeavierVehicleRollsFurther() => Assert.True(OverrunRule.Cells(6, 20000) > OverrunRule.Cells(6, 5000));

    [Fact]
    public void NothingRollsPastTheLimit() => Assert.Equal(OverrunRule.MaxCells, OverrunRule.Cells(30, 60000));

    [Fact]
    public void AnythingMovingRollsAtLeastOneCell() => Assert.Equal(1, OverrunRule.Cells(0.5, 400));

    [Fact]
    public void TheBlowGrowsWithWeightAndSpeed()
    {
        Assert.Equal(200, OverrunRule.ImpactDamage(10, 5000), 6);
        Assert.Equal(OverrunRule.MaxDamage, OverrunRule.ImpactDamage(10, 40000), 6);
        Assert.Equal(OverrunRule.MinDamage, OverrunRule.ImpactDamage(1, 500), 6);
    }

    [Fact]
    public void ItRollsAtItsOwnPace()
    {
        Assert.Equal(6, OverrunRule.TicksPerCell(10));
        Assert.Equal(20, OverrunRule.TicksPerCell(1));
        Assert.Equal(3, OverrunRule.TicksPerCell(60));
    }
}
