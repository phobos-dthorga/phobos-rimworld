using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>The engine-load ledger's arithmetic (Framework 0.131.0).</summary>
public class ShaftLoadRuleTests
{
    [Fact]
    public void AShareOfRatedPowerAtRatedSpeedIsThatShare() =>
        Assert.Equal(10, ShaftLoadRule.Kilowatts(0, 0.05, 200, 3000, 3000, 3, 1), 9);

    [Fact]
    public void AFanTakesAnEighthAtHalfSpeed() =>
        Assert.Equal(1.25, ShaftLoadRule.Kilowatts(0, 0.05, 200, 1500, 3000, 3, 1), 9);

    [Fact]
    public void ASteadyLoadIgnoresEngineSpeed()
    {
        Assert.Equal(5.6, ShaftLoadRule.Kilowatts(5.6, 0, 200, 700, 3000, 0, 1), 9);
        Assert.Equal(5.6, ShaftLoadRule.Kilowatts(5.6, 0, 200, 3000, 3000, 0, 1), 9);
    }

    [Fact]
    public void ItsInputsScaleItAndAStoppedEngineDrivesNothing()
    {
        Assert.Equal(2.632, ShaftLoadRule.Kilowatts(5.6, 0, 200, 2000, 3000, 0, 0.47), 9);
        Assert.Equal(0, ShaftLoadRule.Kilowatts(5.6, 0.05, 200, 0, 3000, 0, 1), 9);
        Assert.Equal(0, ShaftLoadRule.Kilowatts(5.6, 0.05, 200, 2000, 3000, 3, 0), 9);
    }

    [Fact]
    public void TheAlternatorMakesWhatIsAskedOfItUpToWhatItCanGive()
    {
        // 30 A of loads and 10 A into the battery, with 65 A to give: it makes 40.
        Assert.Equal(40, ShaftLoadRule.AlternatorOutputAmps(65, 30, 10), 9);
        // A winch asks 150 A at idle, where it gives 33: it makes 33, and the battery finds the rest.
        Assert.Equal(33, ShaftLoadRule.AlternatorOutputAmps(33, 150, 0), 9);
        Assert.Equal(0, ShaftLoadRule.AlternatorOutputAmps(0, 30, 10), 9);
    }

    [Fact]
    public void TheAlternatorIsChargedForChargingTheBatteryToo()
    {
        double loadsOnly = ShaftLoadRule.AlternatorKilowatts(ShaftLoadRule.AlternatorOutputAmps(65, 20, 0), 12, 0.55);
        double charging = ShaftLoadRule.AlternatorKilowatts(ShaftLoadRule.AlternatorOutputAmps(65, 20, 30), 12, 0.55);
        Assert.Equal(20 * 12 / 0.55 / 1000, loadsOnly, 9);
        Assert.True(charging > loadsOnly * 2);
    }

    [Fact]
    public void ASoundBeltGripsEverythingOnIt() => Assert.Equal(1, ShaftLoadRule.BeltGrip(20, 0.3, 200, 1), 9);

    [Fact]
    public void AWornBeltSlipsUnderAHeavyLoad()
    {
        // A belt at a quarter of its condition carries 15 kW on a 200 kW engine: asked for 20, it passes three quarters.
        Assert.Equal(0.75, ShaftLoadRule.BeltGrip(20, 0.3, 200, 0.25), 9);
        // The same belt holds a light load.
        Assert.Equal(1, ShaftLoadRule.BeltGrip(10, 0.3, 200, 0.25), 9);
    }

    [Fact]
    public void ASnappedBeltDrivesNothingAndAnIdleOneReadsAsGripping()
    {
        Assert.Equal(0, ShaftLoadRule.BeltGrip(20, 0.3, 200, 0), 9);
        Assert.Equal(1, ShaftLoadRule.BeltGrip(0, 0.3, 200, 0.5), 9);
    }
}
