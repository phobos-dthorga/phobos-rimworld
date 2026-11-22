using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class FireFightingRuleTests
{
    [Fact]
    public void AFireStartsWhereThePartJustHitIs() =>
        Assert.Equal(FireZones.CrewCompartment, FireFightingRule.StartZone(FireZones.CrewCompartment, 0.01, 0.05, FireZones.EngineBay));

    [Fact]
    public void AnOldHitDoesNotSayWhereAFireStarted() =>
        Assert.Equal(FireZones.EngineBay, FireFightingRule.StartZone(FireZones.CrewCompartment, 2, 0.05, FireZones.EngineBay));

    [Fact]
    public void NoHitMeansTheFallback() =>
        Assert.Equal(FireZones.RunningGear, FireFightingRule.StartZone(FireZones.None, 0, 0.05, FireZones.RunningGear));

    [Fact]
    public void AnEngineFireReachesTheCrewInTime()
    {
        Assert.Equal(FireZones.EngineBay, FireFightingRule.Spread(FireZones.EngineBay, 0, 0.1, 0.4, 0.25));
        Assert.Equal(FireZones.EngineBay | FireZones.CrewCompartment, FireFightingRule.Spread(FireZones.EngineBay, 0, 0.3, 0.4, 0.25));
    }

    [Fact]
    public void ATyreFireReachesTheEngineBeforeTheCrew() =>
        Assert.Equal(FireZones.RunningGear | FireZones.EngineBay, FireFightingRule.Spread(FireZones.RunningGear, 0.5, 0, 0.4, 0.25));

    [Fact]
    public void ASystemPutsOutTheOnlyZoneBurning() =>
        Assert.Equal((FireZones.None, 0.0), FireFightingRule.Suppress(FireZones.EngineBay, FireZones.EngineBay));

    [Fact]
    public void ASystemLeavesOtherZonesBurningAndTheFireSmaller()
    {
        (FireZones left, double share) = FireFightingRule.Suppress(FireZones.EngineBay | FireZones.RunningGear, FireZones.EngineBay);
        Assert.Equal(FireZones.RunningGear, left);
        Assert.Equal(0.5, share, 9);
    }

    [Fact]
    public void ASystemDoesNothingForAZoneItDoesNotCover() =>
        Assert.Equal((FireZones.RunningGear, 1.0), FireFightingRule.Suppress(FireZones.RunningGear, FireZones.EngineBay));

    [Fact]
    public void OnlyAReadySystemDischargesAndOnlyForItsZone()
    {
        Assert.True(FireFightingRule.Discharges(FireZones.CrewCompartment, FireZones.CrewCompartment, true));
        Assert.False(FireFightingRule.Discharges(FireZones.CrewCompartment, FireZones.CrewCompartment, false));
        Assert.False(FireFightingRule.Discharges(FireZones.RunningGear, FireZones.CrewCompartment, true));
    }

    [Fact]
    public void AnExtinguisherPutsOutASmallFire() => Assert.Equal(0, FireFightingRule.AfterExtinguisher(0.6, 0.75), 9);

    [Fact]
    public void AnExtinguisherOnlyKnocksDownABigFire() => Assert.Equal(1.0, FireFightingRule.AfterExtinguisher(1.75, 0.75), 9);

    [Fact]
    public void ChargesNeverGoBelowNone()
    {
        Assert.Equal(2, FireFightingRule.AfterUse(3));
        Assert.Equal(0, FireFightingRule.AfterUse(0));
    }

    [Fact]
    public void ArmourDecidesUnlessTheVehicleIsMarked()
    {
        Assert.True(FireFightingRule.Armoured(12, 8, null));
        Assert.False(FireFightingRule.Armoured(4, 8, null));
        Assert.False(FireFightingRule.Armoured(12, 8, false));
        Assert.True(FireFightingRule.Armoured(0, 8, true));
    }
}
