using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class EngineStateRuleTests
{
    [Fact]
    public void TheWholeLoadIsDrivingAndTheLoadsTogether() => Assert.Equal(0.35, EngineStateRule.Load(0.3, 0.05), 9);

    [Fact]
    public void TheWholeLoadNeverPassesTheWholeEngine() => Assert.Equal(1, EngineStateRule.Load(0.98, 0.05), 9);

    [Fact]
    public void ANegativeShareCountsAsNothing() => Assert.Equal(0.2, EngineStateRule.Load(-0.5, 0.2), 9);

    [Fact]
    public void OnTheBatteryTheEngineIsStoppedWhateverElseHolds() =>
        Assert.Equal(EngineWhy.OnBattery, EngineStateRule.Why(true, false, true, true, false, true, false));

    [Fact]
    public void AskedForWithAnEmptyTankIsOutOfFuel() =>
        Assert.Equal(EngineWhy.NoFuel, EngineStateRule.Why(false, false, true, false, false, false, false));

    [Fact]
    public void NotAskedForIsOffEvenWithAnEmptyTank() =>
        Assert.Equal(EngineWhy.Off, EngineStateRule.Why(false, false, false, false, false, false, false));

    [Fact]
    public void MovingIsDrivingAndStandingIsIdling()
    {
        Assert.Equal(EngineWhy.Driving, EngineStateRule.Why(false, true, true, true, false, true, false));
        Assert.Equal(EngineWhy.Idling, EngineStateRule.Why(false, true, true, true, false, false, false));
    }

    [Fact]
    public void StandingWorkIsWorkingAndFlyingComesFirst()
    {
        Assert.Equal(EngineWhy.Working, EngineStateRule.Why(false, true, true, true, false, false, true));
        Assert.Equal(EngineWhy.Flying, EngineStateRule.Why(false, true, true, true, true, true, true));
    }
}
