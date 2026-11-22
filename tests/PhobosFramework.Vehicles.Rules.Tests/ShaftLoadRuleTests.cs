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

    // ---- The alternator, right-sized, and the loads felt (Framework 0.243.0) ----

    [Fact]
    public void AnAlternatorDragsAShareOfItsRatedWatts()
    {
        // A car's 65 A at 14 V is 910 W; 8 % of it is about 73 W.
        Assert.Equal(0.0728, ShaftLoadRule.AlternatorDragKilowatts(65, 14, 0.08), 9);
        Assert.Equal(0, ShaftLoadRule.AlternatorDragKilowatts(0, 14, 0.08), 9);
    }

    [Fact]
    public void AtTheChargingVoltageTheAlternatorTakesASixthMore()
    {
        double atTwelve = ShaftLoadRule.AlternatorKilowatts(30, 12, 0.55);
        double atFourteen = ShaftLoadRule.AlternatorKilowatts(30, 12 * 14.0 / 12.0, 0.55);
        Assert.Equal(14.0 / 12.0, atFourteen / atTwelve, 9);
    }

    [Fact]
    public void IdleRisesWithTheLoadsUpToItsShare()
    {
        Assert.Equal(700, ShaftLoadRule.IdleRpm(700, 0, 200, 0.2, 0.05), 9);
        Assert.Equal(770, ShaftLoadRule.IdleRpm(700, 5, 200, 0.2, 0.05), 9); // 2.5 % of rated: half way, a tenth more
        Assert.Equal(840, ShaftLoadRule.IdleRpm(700, 10, 200, 0.2, 0.05), 9);
        Assert.Equal(840, ShaftLoadRule.IdleRpm(700, 50, 200, 0.2, 0.05), 9);
        Assert.Equal(700, ShaftLoadRule.IdleRpm(700, -5, 200, 0.2, 0.05), 9);
    }

    [Fact]
    public void EachShaftKilowattCostsOneOverTheIndicatedEfficiencyOfFuel()
    {
        // 2 kW at 40 % is 5 kW of fuel, a fifth of 25 kW burned.
        Assert.Equal(0.2, ShaftLoadRule.FuelShare(2, 25, 0.4), 9);
        Assert.Equal(1, ShaftLoadRule.FuelShare(50, 25, 0.4), 9);
        Assert.Equal(0, ShaftLoadRule.FuelShare(2, 0, 0.4), 9);
    }

    [Fact]
    public void AnElectricVehiclesConverterDrawsOnItsPack()
    {
        // 30 A at 14 V is 420 W, 457 W from the pack at 92 %: about 0.63 % of a 72 kWh pack an hour.
        Assert.Equal(420.0 / 0.92 / 1000.0 / 72.0, ShaftLoadRule.ConverterPackSharePerHour(30, 14, 0.92, 72), 12);
        Assert.Equal(0, ShaftLoadRule.ConverterPackSharePerHour(0, 14, 0.92, 72), 12);
    }
}
