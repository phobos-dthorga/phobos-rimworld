using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class AirCombatRuleTests
{
    [Fact]
    public void ArrivalFollowsVehicleFrameworksPace()
    {
        // Speed 20: 0.02 tiles a tick, so 10 tiles take 500 ticks.
        Assert.Equal(500, AirCombatRule.TicksToArrival(10, 20));
        Assert.Equal(0, AirCombatRule.TicksToArrival(0, 20));
        Assert.True(AirCombatRule.TicksToArrival(10, 0) > 1_000_000);
    }

    [Fact]
    public void AngularSpeedIsTilesPerTickOverTilesPerRadian()
    {
        Assert.Equal(0.02 / 100, AirCombatRule.AngularSpeed(20, 100), 9);
        Assert.Equal(0, AirCombatRule.AngularSpeed(20, 0));
    }

    [Fact]
    public void AnAircraftClimbsAwayAndComesDownAtTheEnds()
    {
        Assert.Equal(6000, AirCombatRule.Altitude(6000, 1500, low: false, tilesFlown: 10, tilesLeft: 10, climbTiles: 2));
        Assert.Equal(3000, AirCombatRule.Altitude(6000, 1500, low: false, tilesFlown: 1, tilesLeft: 10, climbTiles: 2));
        Assert.Equal(600, AirCombatRule.Altitude(6000, 1500, low: false, tilesFlown: 0, tilesLeft: 10, climbTiles: 2));
    }

    [Fact]
    public void WithoutOxygenTheCruiseIsLow()
    {
        Assert.Equal(1500, AirCombatRule.Altitude(6000, 1500, low: true, tilesFlown: 10, tilesLeft: 10, climbTiles: 2));
        // A helicopter already below the low cruise is not raised to it.
        Assert.Equal(300, AirCombatRule.Altitude(300, 1500, low: true, tilesFlown: 10, tilesLeft: 10, climbTiles: 2));
    }

    [Fact]
    public void CountermeasuresCutTheHitChanceMoreOnTheReceiversCue()
    {
        Assert.Equal(0.6, AirCombatRule.HitChanceFactor(true, true, warningWorks: true, 0.6, 0.85));
        Assert.Equal(0.85, AirCombatRule.HitChanceFactor(true, true, warningWorks: false, 0.6, 0.85));
        Assert.Equal(1, AirCombatRule.HitChanceFactor(true, cartridgesAboard: false, true, 0.6, 0.85));
        Assert.Equal(1, AirCombatRule.HitChanceFactor(dispenserWorks: false, true, true, 0.6, 0.85));
    }

    [Fact]
    public void ChaffSlowsALockOnlyOnTheReceiversCue()
    {
        Assert.Equal(1.5, AirCombatRule.LockTimeFactor(true, true, warningWorks: true, 1.5));
        Assert.Equal(1, AirCombatRule.LockTimeFactor(true, true, warningWorks: false, 1.5));
        Assert.Equal(1, AirCombatRule.LockTimeFactor(true, true, true, 0.5));
    }

    [Fact]
    public void ASoundHarrierDodgesAsItsAuthorSet()
    {
        Assert.Equal(0.4, AirCombatRule.Evasion(0.4, 1, warningWorks: true, 0.75, 0, 0.9), 9);
    }

    [Fact]
    public void ShotUpControlsAndADeadReceiverDodgeLess()
    {
        Assert.Equal(0.4 * 0.5 * 0.75, AirCombatRule.Evasion(0.4, 0.5, warningWorks: false, 0.75, 0, 0.9), 9);
    }

    [Fact]
    public void ASalvoAddsItsBonusUpToTheCap()
    {
        Assert.Equal(0.6, AirCombatRule.Evasion(0.4, 1, true, 0.75, 0.2, 0.9), 9);
        Assert.Equal(0.9, AirCombatRule.Evasion(0.8, 1, true, 0.75, 0.3, 0.9), 9);
        // An aircraft the author never lets dodge gains nothing.
        Assert.Equal(0, AirCombatRule.Evasion(0, 1, true, 0.75, 0.3, 0.9));
    }

    [Fact]
    public void AHitIsSharedOut()
    {
        Assert.Equal((4, 15.0), AirCombatRule.SplitDamage(60, 4));
        Assert.Equal((1, 60.0), AirCombatRule.SplitDamage(60, 0));
        Assert.Equal((0, 0.0), AirCombatRule.SplitDamage(0, 4));
    }

    [Fact]
    public void AnAircraftStaysUpWhileItCanFly()
    {
        Assert.True(AirCombatRule.StaysUp(0.6, 1, 2, airframeFailed: false));
        Assert.False(AirCombatRule.StaysUp(0.2, 2, 2, airframeFailed: false));
        Assert.False(AirCombatRule.StaysUp(1, 0, 2, airframeFailed: false));
        Assert.False(AirCombatRule.StaysUp(1, 2, 2, airframeFailed: true));
        // Thrust pods with no engine count read as having none to lose.
        Assert.True(AirCombatRule.StaysUp(1, 0, 0, airframeFailed: false));
    }

    [Fact]
    public void APoweredTurretLosesNothing()
    {
        Assert.Equal(1, TurretAimRule.Factor(cranked: false, moving: true, poorVisibility: true, 0.6, 0.7));
    }

    [Fact]
    public void ACrankedTurretShootsWorseOnTheMoveAndInTheDark()
    {
        Assert.Equal(0.6, TurretAimRule.Factor(true, moving: true, poorVisibility: false, 0.6, 0.7), 9);
        Assert.Equal(0.7, TurretAimRule.Factor(true, moving: false, poorVisibility: true, 0.6, 0.7), 9);
        Assert.Equal(0.42, TurretAimRule.Factor(true, moving: true, poorVisibility: true, 0.6, 0.7), 9);
        Assert.Equal(1, TurretAimRule.Factor(true, moving: false, poorVisibility: false, 0.6, 0.7));
    }

    [Fact]
    public void CombatExtendedSwayGrowsAsAccuracyFalls()
    {
        Assert.Equal(4, TurretAimRule.CombatExtendedSway(2, 0.5, 3), 9);
        Assert.Equal(6, TurretAimRule.CombatExtendedSway(2, 0.1, 3), 9);
        Assert.Equal(2, TurretAimRule.CombatExtendedSway(2, 1, 3), 9);
    }

    [Fact]
    public void CombatExtendedSwayWidensAWeaponWithNoneOfItsOwn()
    {
        Assert.Equal(2, TurretAimRule.CombatExtendedSway(0, 0.5, 3, least: 1), 9);
        Assert.Equal(4, TurretAimRule.CombatExtendedSway(2, 0.5, 3, least: 1), 9);
        Assert.Equal(0, TurretAimRule.CombatExtendedSway(0, 1, 3, least: 1), 9);
    }
}
