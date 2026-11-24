using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class RaidLandingRuleTests
{
    [Fact]
    public void TheWayIsChosenByTheWeights()
    {
        Assert.Equal(RaidLanding.OutsideTurrets, RaidLandingRule.Choose(0));
        Assert.Equal(RaidLanding.OutsideTurrets, RaidLandingRule.Choose(0.49));
        Assert.Equal(RaidLanding.RopeClose, RaidLandingRule.Choose(0.5));
        Assert.Equal(RaidLanding.RopeClose, RaidLandingRule.Choose(0.79));
        Assert.Equal(RaidLanding.Closest, RaidLandingRule.Choose(0.8));
        Assert.Equal(RaidLanding.Closest, RaidLandingRule.Choose(1));
    }

    [Fact]
    public void ACautiousLandingKeepsOutOfTurretReach()
    {
        Assert.True(double.IsPositiveInfinity(RaidLandingRule.Score(RaidLanding.OutsideTurrets, 20, inTurretReach: true)));
        Assert.Equal(20, RaidLandingRule.Score(RaidLanding.OutsideTurrets, 20, inTurretReach: false));
        // The bold ways do not care.
        Assert.Equal(20, RaidLandingRule.Score(RaidLanding.Closest, 20, inTurretReach: true));
        Assert.Equal(20, RaidLandingRule.Score(RaidLanding.RopeClose, 20, inTurretReach: true));
    }

    [Fact]
    public void NeverOnTopOfTheColonyNorTooFarOut()
    {
        Assert.True(double.IsPositiveInfinity(RaidLandingRule.Score(RaidLanding.Closest, 3, false)));
        Assert.True(double.IsPositiveInfinity(RaidLandingRule.Score(RaidLanding.Closest, 61, false)));
        Assert.True(RaidLandingRule.Score(RaidLanding.Closest, 8, false) < RaidLandingRule.Score(RaidLanding.Closest, 30, false));
    }

    [Fact]
    public void AParachuteDropKeepsOutOfTurretReach()
    {
        Assert.Equal(double.PositiveInfinity, RaidLandingRule.Score(RaidLanding.Parachute, 20, true));
        Assert.Equal(20, RaidLandingRule.Score(RaidLanding.Parachute, 20, false), 6);
        Assert.NotEqual(RaidLanding.Parachute, RaidLandingRule.Choose(0.999));
    }
}
