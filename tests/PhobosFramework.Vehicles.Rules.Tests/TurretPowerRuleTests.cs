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
