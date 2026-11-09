using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class OverdriveRuleTests
{
    [Fact]
    public void WorkBurnsItsOwnFuelWithNobodyAtTheWheel() => Assert.Equal(8f, OverdriveRule.WorkFuel(8f, 3f, engineHeldOn: false));

    [Fact]
    public void WithADriverOnlyWhatTheWorkBurnsAboveIdlingIsCharged()
    {
        Assert.Equal(5f, OverdriveRule.WorkFuel(8f, 3f, engineHeldOn: true));
        Assert.Equal(0f, OverdriveRule.WorkFuel(2f, 3f, engineHeldOn: true));
    }

    [Fact]
    public void OffChangesNothing()
    {
        Assert.Equal(10f, OverdriveRule.Rate(10f, false, 2f));
        Assert.Equal(40f, OverdriveRule.FuelPerDay(40f, false, 2.5f));
    }

    [Fact]
    public void OnDoublesTheRateAndMoreThanDoublesTheFuel()
    {
        Assert.Equal(20f, OverdriveRule.Rate(10f, true, 2f));
        Assert.Equal(100f, OverdriveRule.FuelPerDay(40f, true, 2.5f));
    }

    [Fact]
    public void WearFollowsTheHours() => Assert.Equal(1f, OverdriveRule.Wear(2f, 0.5f));

    [Fact]
    public void RunsWhilePartsAreSoundAndTheEngineCool() => Assert.Equal(OverdriveStop.None, OverdriveRule.MustStop(0.9f, 0.5f, 95f, 120f));

    [Fact]
    public void StopsOnAWornPart() => Assert.Equal(OverdriveStop.PartWorn, OverdriveRule.MustStop(0.49f, 0.5f, 95f, 120f));

    [Fact]
    public void StopsOnAHotEngine() => Assert.Equal(OverdriveStop.EngineHot, OverdriveRule.MustStop(0.9f, 0.5f, 120f, 120f));

    [Fact]
    public void AVehicleWithNoCoolingNeverStopsForHeat() => Assert.Equal(OverdriveStop.None, OverdriveRule.MustStop(0.9f, 0.5f, float.NaN, 120f));
}
