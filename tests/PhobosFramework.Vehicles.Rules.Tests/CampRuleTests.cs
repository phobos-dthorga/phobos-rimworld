using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class CampRuleTests
{
    [Fact]
    public void ATwentyFourVoltOutletIsTwiceTheSize() => Assert.Equal(600, CampRule.OutletRating(300, 24), 6);

    [Fact]
    public void TheOutletGivesWhatTheCampAsksUpToItsRating()
    {
        Assert.Equal(120, CampRule.OutletWatts(120, 300), 6);
        Assert.Equal(300, CampRule.OutletWatts(900, 300), 6);
        Assert.Equal(0, CampRule.OutletWatts(-50, 300), 6);
    }

    [Fact]
    public void ARadioOnTwelveVoltsDrawsAboutTwentyAmps() => Assert.Equal(19.6, CampRule.OutletAmps(200, 12, 0.85), 1);

    [Fact]
    public void NothingGivenDrawsNothing() => Assert.Equal(0, CampRule.OutletAmps(0, 12, 0.85), 6);

    [Fact]
    public void TheFridgeTakesWhatSpoilsFirst()
    {
        var stacks = new List<(double, double)> { (100, 10), (5, 10), (50, 15), (20, 10) };
        // Closest to spoiling: index 1 (5 h), then 3 (20 h), then 2 (50 h) would pass 30 kg; index 0 (100 h) still fits.
        Assert.Equal(new List<int> { 0, 1, 3 }, CampRule.FridgeTakes(stacks, 30));
    }

    [Fact]
    public void AStackTooBigForTheFridgeIsLeftWhole() => Assert.Empty(CampRule.FridgeTakes(new List<(double, double)> { (1, 40) }, 30));

    [Fact]
    public void AFridgeNeverWarmsColdAir() => Assert.Equal(-10, CampRule.FridgeTemperature(4, -10), 6);

    [Fact]
    public void TheHouseBatteryCutsOffLow()
    {
        Assert.True(CampRule.HouseBatteryRuns(0.5, 0.1));
        Assert.False(CampRule.HouseBatteryRuns(0.1, 0.1));
    }
}
