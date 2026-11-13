using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class AvionicsRuleTests
{
    [Fact]
    public void TheDefaultFitIsInstrumentsAndBasicNavigation()
    {
        var fit = new AvionicsFitValues();
        Assert.Equal(4, AvionicsRule.EssentialAmps(fit));
        Assert.Equal(0, AvionicsRule.OtherAmps(fit));
    }

    [Fact]
    public void AFullFitKeepsItsBasicPartOnTheBatteryAndShedsTheRest()
    {
        var fit = new AvionicsFitValues { Navigation = AvionicsLevel.Full, Sight = AvionicsLevel.Full, Radar = true };
        Assert.Equal(2 + 2 + 1, AvionicsRule.EssentialAmps(fit));
        Assert.Equal(4 + 4 + 20, AvionicsRule.OtherAmps(fit));
    }

    [Fact]
    public void TheHarriersFitDrawsAboutFortyFiveAmps()
    {
        var harrier = new AvionicsFitValues
        {
            Navigation = AvionicsLevel.Full, Autopilot = true, Radar = true, Sight = AvionicsLevel.Full, NightSight = true,
            Warning = true, Countermeasures = true, Oxygen = true,
        };
        Assert.Equal(46, AvionicsRule.EssentialAmps(harrier) + AvionicsRule.OtherAmps(harrier));
    }

    [Fact]
    public void ParametersSayWhatIsCarried()
    {
        var fit = new AvionicsFitValues { Attitude = false, Navigation = AvionicsLevel.Full };
        var values = AvionicsRule.Parameters(fit).ToDictionary(p => p.name, p => p.value);
        Assert.Equal(0, values["attitude"]);
        Assert.Equal(1, values["navigation"]);
        Assert.Equal(1, values["navigationFull"]);
    }

    [Fact]
    public void ARatingIsOverItsLimitOnlyAfterItsMinutes()
    {
        Assert.False(RatingLimitRule.Over(200, 5, 2500.0 / 60));
        Assert.True(RatingLimitRule.Over(220, 5, 2500.0 / 60));
        Assert.False(RatingLimitRule.Over(100000, 0, 2500.0 / 60));
    }

    [Fact]
    public void PastTheLimitTheHotSectionWearsByTheFactor()
    {
        Assert.Equal(2, RatingLimitRule.LifeFactor(true, 2));
        Assert.Equal(1, RatingLimitRule.LifeFactor(false, 2));
        Assert.Equal(1, RatingLimitRule.LifeFactor(true, 0.5));
    }
}
