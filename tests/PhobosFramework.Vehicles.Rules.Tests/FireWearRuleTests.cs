using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class FireWearRuleTests
{
    [Fact]
    public void AFullFireWearsAtTheRolesRate() => Assert.Equal(0.4, FireWearRule.SharePerHour(0.4, 1, 0.1), 6);

    [Fact]
    public void AHalfFireWearsHalfAsFast() => Assert.Equal(0.2, FireWearRule.SharePerHour(0.4, 0.5, 0.1), 6);

    [Fact]
    public void ASparkStillWearsAtTheLeastShare() => Assert.Equal(0.04, FireWearRule.SharePerHour(0.4, 0.01, 0.1), 6);

    [Fact]
    public void NoFireNoWear() => Assert.Equal(0, FireWearRule.SharePerHour(0.4, 0, 0.1));

    [Fact]
    public void ABiggerThanFullFireCountsAsFull() => Assert.Equal(0.4, FireWearRule.SharePerHour(0.4, 3, 0.1), 6);

    [Fact]
    public void AHardPartFeelsOnlyTheFireOnTheVehicle() =>
        Assert.Equal(0, FireWearRule.FireFelt(0, 0.8, soft: false, vehicleCanCatch: false, groundSettingOn: true));

    [Fact]
    public void ASoftPartOfAFireproofVehicleFeelsTheGroundFire() =>
        Assert.Equal(0.8, FireWearRule.FireFelt(0, 0.8, soft: true, vehicleCanCatch: false, groundSettingOn: true), 6);

    [Fact]
    public void AVehicleThatCanCatchFireIsLeftToItsOwnFire() =>
        Assert.Equal(0.2, FireWearRule.FireFelt(0.2, 0.8, soft: true, vehicleCanCatch: true, groundSettingOn: true), 6);

    [Fact]
    public void TheSettingOffLeavesFireproofVehiclesFireproof() =>
        Assert.Equal(0, FireWearRule.FireFelt(0, 0.8, soft: true, vehicleCanCatch: false, groundSettingOn: false));
}
