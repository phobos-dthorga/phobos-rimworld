using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class HookUpRuleTests
{
    [Fact]
    public void TheGridCarriesTheLoadsUpToItsRating()
    {
        Assert.Equal(40, HookUpRule.CarriedAmps(40, 60));
        Assert.Equal(60, HookUpRule.CarriedAmps(190, 60));
        Assert.Equal(0, HookUpRule.CarriedAmps(0, 60));
    }

    [Fact]
    public void TheChargerTapersNearFull()
    {
        Assert.Equal(10, HookUpRule.ChargeAmps(10, 0.5));
        Assert.Equal(5, HookUpRule.ChargeAmps(10, 0.9), 9);
        Assert.Equal(0, HookUpRule.ChargeAmps(10, 1));
    }

    [Fact]
    public void ATurretOnTwentyFourVoltsCostsAboutAKilowatt()
    {
        // 40 A of turret drive and 10 A of charging at 24 V through an 85 % supply.
        Assert.Equal(50 * 24 / 0.85, HookUpRule.GridWatts(40, 10, 24, 0.85), 6);
        Assert.Equal(0, HookUpRule.GridWatts(0, 0, 24, 0.85));
    }
}
