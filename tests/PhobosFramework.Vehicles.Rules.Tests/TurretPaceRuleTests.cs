using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class TurretPaceRuleTests
{
    [Fact]
    public void AWholeMountGivesFullPace() => Assert.Equal(1, TurretPaceRule.DamagePace(1, 0.1, 0.55), 6);

    [Fact]
    public void AtTheStopLineThePaceIsTheSlowest() => Assert.Equal(0.55, TurretPaceRule.DamagePace(0.1, 0.1, 0.55), 6);

    [Fact]
    public void HalfwayIsHalfwayBetween() => Assert.Equal(0.775, TurretPaceRule.DamagePace(0.55, 0.1, 0.55), 6);

    [Fact]
    public void BelowTheStopLineTheSlowestHolds() => Assert.Equal(0.55, TurretPaceRule.DamagePace(0, 0.1, 0.55), 6);

    [Fact]
    public void AHealthyGunnerIsFullPace() => Assert.Equal(1, TurretPaceRule.CrewPace(1, 0.5), 6);

    [Fact]
    public void ABionicGunnerIsNoFasterThanTheGun() => Assert.Equal(1, TurretPaceRule.CrewPace(1.3, 0.5), 6);

    [Fact]
    public void AHurtGunnerSlowsTheGun() => Assert.Equal(0.7, TurretPaceRule.CrewPace(0.7, 0.5), 6);

    [Fact]
    public void ABadlyHurtGunnerStopsAtTheFloor() => Assert.Equal(0.5, TurretPaceRule.CrewPace(0.1, 0.5), 6);

    [Fact]
    public void NobodyAtTheGunChangesNothing() => Assert.Equal(1, TurretPaceRule.CrewPace(null, 0.5), 6);

    [Fact]
    public void HalfPaceDoublesATime() => Assert.Equal(2, TurretPaceRule.TimeFactor(0.5), 6);

    [Fact]
    public void ATimeNeverBecomesEndless() => Assert.Equal(20, TurretPaceRule.TimeFactor(0), 6);
}
