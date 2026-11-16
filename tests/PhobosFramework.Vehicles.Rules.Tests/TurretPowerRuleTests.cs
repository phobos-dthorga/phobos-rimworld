using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class TurretPowerRuleTests
{
    static readonly string[] Patterns = { "M2HB", "M240", "topMG", "supportMG" };

    [Fact]
    public void APintleMachineGunIsSwungByHand() => Assert.True(TurretPowerRule.HandSwung("M2HB_A", Patterns));

    [Fact]
    public void ACaseDifferenceDoesNotMatter() => Assert.True(TurretPowerRule.HandSwung("TopMg", Patterns));

    [Fact]
    public void AMainGunIsPowered() => Assert.False(TurretPowerRule.HandSwung("M256_A", Patterns));

    static readonly string[] HandSwung = { "M2HB", "M240", "Mk19", "supportMG", "pintle" };
    static readonly string[] Coaxial = { "coax" };
    static readonly string[] OwnMount = { "topMG", "topTurret", "topMounted", "MountedTurret", "pintle", "cupola" };

    static WeaponMountKind Kind(bool isFixed, bool rides, string key, string def, bool sameCrew) =>
        TurretPowerRule.Classify(isFixed, rides, key, def, sameCrew, HandSwung, Coaxial, OwnMount);

    [Fact]
    public void ASmokeLauncherIsFixed() => Assert.Equal(WeaponMountKind.Fixed, Kind(true, true, "leftSmoke", "RT_GrenadeLauncherUS_A", true));

    [Fact]
    public void AMainGunIsPoweredByItsOwnDrive() => Assert.Equal(WeaponMountKind.Powered, Kind(false, false, "BushMaster", "RT_M2A3_Tank_Turret", true));

    [Fact]
    public void AMachineGunStandingAloneIsSwungByHandByItsTurretDef() =>
        Assert.Equal(WeaponMountKind.HandSwung, Kind(false, false, "mainTurret", "RT_M2HB_MGTurret", true));

    [Fact]
    public void ACoaxialGunGoesWithItsTurret() => Assert.Equal(WeaponMountKind.WithTurret, Kind(false, true, "subCoaxialMG", "RT_M240_MGTurret", true));

    [Fact]
    public void AGunOfTheTurretsOwnCrewGoesWithIt() =>
        Assert.Equal(WeaponMountKind.WithTurret, Kind(false, true, "M240_A", "RT_M240_MGTurret", sameCrew: true));

    [Fact]
    public void ACommandersGunOnTheTurretIsSwungByHand() =>
        Assert.Equal(WeaponMountKind.HandSwung, Kind(false, true, "M2HB_A", "RT_M2HB_MGTurret", sameCrew: false));

    [Fact]
    public void AMountWordInTheKeyWinsOverASharedCrew() => Assert.Equal(WeaponMountKind.HandSwung, Kind(false, true, "topMG", "RT_M240_MGTurret", sameCrew: true));

    [Fact]
    public void ARunningLoadOnAChargedBatteryIsPowered() => Assert.True(TurretPowerRule.Powered(true, 0.6, 0.05));

    [Fact]
    public void AFlatBatteryIsNoPower() => Assert.False(TurretPowerRule.Powered(true, 0.01, 0.05));

    [Fact]
    public void AShedLoadIsNoPower() => Assert.False(TurretPowerRule.Powered(false, 1, 0.05));

    [Fact]
    public void WithPowerTheTurretIsFullPace()
    {
        TurretPowerRule.Paces(true, 0.2, 0.5, out double traverse, out double aim);
        Assert.Equal(1, traverse, 6);
        Assert.Equal(1, aim, 6);
    }

    [Fact]
    public void WithoutPowerItIsCrankedNeverDead()
    {
        TurretPowerRule.Paces(false, 0.2, 0.5, out double traverse, out double aim);
        Assert.Equal(0.2, traverse, 6);
        Assert.Equal(0.5, aim, 6);
        TurretPowerRule.Paces(false, 0, 0, out traverse, out aim);
        Assert.True(traverse > 0 && aim > 0);
    }
}
