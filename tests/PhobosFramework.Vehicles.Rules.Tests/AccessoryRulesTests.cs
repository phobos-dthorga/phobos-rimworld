using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class AccessoryRulesTests
{
    [Fact]
    public void NothingSwitchedOnCostsNothing() => Assert.Equal(0, AccessoryRules.EngineShare(0, 12, 100), 9);

    [Fact]
    public void ALightTrucksLightsAndHeaterTakeAboutOnePercent()
    {
        // 9 A of headlamps, 12 A of heater blower and 4 A of instruments at 12 V: 300 W delivered, 500 W at the shaft of a 50 kW engine.
        Assert.Equal(0.01, AccessoryRules.EngineShare(25, 12, 50), 6);
    }

    [Fact]
    public void ThePowerAtTheShaftIsMoreThanTheLoadsDraw() =>
        Assert.True(AccessoryRules.EngineShare(10, 24, 100) > 10 * 24 / 1000.0 / 100);

    [Fact]
    public void ItNeverExceedsTheWholeEngine() => Assert.Equal(1, AccessoryRules.EngineShare(10000, 24, 1), 9);

    [Fact]
    public void AnEngineOfUnknownPowerIsNotCharged() => Assert.Equal(0, AccessoryRules.EngineShare(25, 12, 0), 9);
}
