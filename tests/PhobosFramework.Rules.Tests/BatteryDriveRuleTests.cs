using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class BatteryDriveRuleTests
{
    [Fact]
    public void AFullBufferCarriesItsBudget() => Assert.Equal(300f, BatteryDriveRule.CellsLeft(150f, 0.5f), 3);

    [Fact]
    public void NoChargeCarriesNothing() => Assert.Equal(0f, BatteryDriveRule.CellsLeft(0f, 0.5f), 3);

    [Fact]
    public void MovingDrainsByTheCell() => Assert.Equal(145f, BatteryDriveRule.Drain(150f, 10f, 0.5f), 3);

    [Fact]
    public void DrainingNeverGoesBelowEmpty() => Assert.Equal(0f, BatteryDriveRule.Drain(2f, 100f, 0.5f), 3);

    [Fact]
    public void StandingStillDrainsNothing() => Assert.Equal(150f, BatteryDriveRule.Drain(150f, 0f, 0.5f), 3);

    [Fact]
    public void TheEngineChargesIt() => Assert.Equal(100f, BatteryDriveRule.Charge(0f, 150f, 400f, 0.25f), 3);

    [Fact]
    public void ChargingStopsAtCapacity() => Assert.Equal(150f, BatteryDriveRule.Charge(140f, 150f, 400f, 1f), 3);

    [Fact]
    public void LessThanACellCannotMove() => Assert.False(BatteryDriveRule.CanCreep(0.2f, 0.5f));

    [Fact]
    public void ACellsWorthCanMove() => Assert.True(BatteryDriveRule.CanCreep(0.5f, 0.5f));
}
