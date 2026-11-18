using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class FlightPathRuleTests
{
    [Fact]
    public void HeadingsAreClockwiseFromNorth()
    {
        Assert.Equal(0, FlightPathRule.HeadingTo(0, 5, 123), 9);
        Assert.Equal(90, FlightPathRule.HeadingTo(5, 0, 123), 9);
        Assert.Equal(180, FlightPathRule.HeadingTo(0, -5, 123), 9);
        Assert.Equal(270, FlightPathRule.HeadingTo(-5, 0, 123), 9);
        Assert.Equal(45, FlightPathRule.HeadingTo(3, 3, 0), 9);
        // At the point itself the heading it has is kept.
        Assert.Equal(123, FlightPathRule.HeadingTo(0, 0, 123), 9);
    }

    [Fact]
    public void ItTurnsTheShortWayRoundAndNoFurtherThanAllowed()
    {
        Assert.Equal(20, FlightPathRule.Turn(350, 10), 9);
        Assert.Equal(-20, FlightPathRule.Turn(10, 350), 9);
        Assert.Equal(180, FlightPathRule.Turn(0, 180), 9);
        Assert.Equal(355, FlightPathRule.TurnToward(10, 270, 15), 9);
        Assert.Equal(270, FlightPathRule.TurnToward(265, 270, 15), 9);
    }

    [Fact]
    public void TheNearestHeadingsAreFoundAcrossNorth()
    {
        Assert.Equal(0, FlightPathRule.NearestOfEight(350), 9);
        Assert.Equal(45, FlightPathRule.NearestOfEight(30), 9);
        Assert.Equal(315, FlightPathRule.NearestOfEight(300), 9);
        Assert.Equal(0, FlightPathRule.NearestOfFour(316), 9);
        Assert.Equal(270, FlightPathRule.NearestOfFour(260), 9);
    }

    [Fact]
    public void ItSpeedsUpThenBrakesToArriveAtRest()
    {
        // From rest, a step of a second at 2 a second squared.
        Assert.Equal(2, FlightPathRule.NextSpeed(0, 100, 8, 2, 1), 9);
        // Cruising, never above its top speed.
        Assert.Equal(8, FlightPathRule.NextSpeed(8, 100, 8, 2, 1), 9);
        // Near the end it brakes: from 8 with 4 cells left at 2 it may go no faster than 4.
        Assert.Equal(6, FlightPathRule.NextSpeed(8, 4, 8, 2, 1), 9);
        Assert.Equal(4, FlightPathRule.NextSpeed(5, 4, 8, 2, 1), 9);
        Assert.Equal(0, FlightPathRule.NextSpeed(3, 0, 8, 2, 1), 9);
        // Simulated: it gets there and stops, never overshooting by more than a step.
        double position = 0, speed = 0;
        for (int i = 0; i < 6000 && position < 50; i++)
        {
            speed = FlightPathRule.NextSpeed(speed, 50 - position, 8, 2, 1 / 60.0);
            position += speed / 60.0;
            if (speed <= 0) break;
        }
        Assert.InRange(position, 49.5, 50.2);
    }

    [Fact]
    public void ItTurnsBeforeItRuns()
    {
        Assert.Equal(1, FlightPathRule.TurnSpeedShare(10), 9);
        Assert.Equal(0.25, FlightPathRule.TurnSpeedShare(-120), 9);
        Assert.InRange(FlightPathRule.TurnSpeedShare(50), 0.6, 0.7);
    }

    [Fact]
    public void TheHoverMarginDecidesWhatItMayDo()
    {
        Assert.Equal(FlightHold.Hover, FlightPathRule.Judge(1.0, 0.6));
        Assert.Equal(FlightHold.Hover, FlightPathRule.Judge(1.3, 0.6));
        Assert.Equal(FlightHold.ForwardOnly, FlightPathRule.Judge(0.8, 0.6));
        Assert.Equal(FlightHold.ForwardOnly, FlightPathRule.Judge(0.6, 0.6));
        Assert.Equal(FlightHold.Falling, FlightPathRule.Judge(0.55, 0.6));
        // An Osprey on one engine at a middling weight, the record's 0.55 to 0.65: light it flies on, heavy it comes down.
        Assert.Equal(FlightHold.ForwardOnly, FlightPathRule.Judge(0.65, 0.6));
        Assert.Equal(FlightHold.Falling, FlightPathRule.Judge(0.55, 0.6));
        // A margin that cannot be judged leaves it flying.
        Assert.Equal(FlightHold.Hover, FlightPathRule.Judge(double.NaN, 0.6));
    }

    [Fact]
    public void AForcedLandingCostsMoreTheFurtherShortItWas()
    {
        Assert.Equal(FlightPathRule.ForcedLandingLeastShare, FlightPathRule.ForcedLandingShare(1), 9);
        Assert.Equal(FlightPathRule.ForcedLandingMostShare, FlightPathRule.ForcedLandingShare(0), 9);
        Assert.True(FlightPathRule.ForcedLandingShare(0.5) > FlightPathRule.ForcedLandingShare(0.9));
        Assert.Equal(FlightPathRule.ForcedLandingMostShare, FlightPathRule.ForcedLandingShare(double.NaN), 9);
        Assert.Equal(2, FlightPathRule.ForcedLandingHarm(1.2), 9);
        Assert.Equal(10, FlightPathRule.ForcedLandingHarm(0), 9);
    }

    [Fact]
    public void TheShadowSlidesAwayAsItClimbs()
    {
        (double x0, double z0) = FlightPathRule.ShadowOffset(0, 12);
        Assert.Equal(0, x0, 9);
        Assert.Equal(0, z0, 9);
        (double x1, double z1) = FlightPathRule.ShadowOffset(1, 12);
        Assert.True(x1 > 0 && z1 < 0);
        Assert.Equal(3, System.Math.Sqrt(x1 * x1 + z1 * z1), 9);
    }

    [Fact]
    public void ItLeavesAndEntersByTheEdgeOnTheSideOfTheJourney()
    {
        // A 250 by 250 map held 6 cells in from each side; from its centre.
        (double x, double z) = FlightPathRule.EdgePoint(125, 125, 0, 6, 244, 6, 244);
        Assert.Equal(125, x, 6);
        Assert.Equal(244, z, 6);
        (x, z) = FlightPathRule.EdgePoint(125, 125, 90, 6, 244, 6, 244);
        Assert.Equal(244, x, 6);
        Assert.Equal(125, z, 6);
        (x, z) = FlightPathRule.EdgePoint(125, 125, 225, 6, 244, 6, 244);
        Assert.Equal(6, x, 6);
        Assert.Equal(6, z, 6);
        // From off centre, the nearer side is met first.
        (x, z) = FlightPathRule.EdgePoint(200, 125, 60, 6, 244, 6, 244);
        Assert.Equal(244, x, 6);
        Assert.InRange(z, 125, 244);
        // A point outside is brought in first.
        (x, z) = FlightPathRule.EdgePoint(-50, 125, 270, 6, 244, 6, 244);
        Assert.Equal(6, x, 6);
    }

    [Fact]
    public void APointIsHeldInsideTheMap()
    {
        Assert.Equal((5.0, 200.0), FlightPathRule.Clamp(-3, 400, 5, 245, 5, 200));
        Assert.Equal((50.0, 60.0), FlightPathRule.Clamp(50, 60, 5, 245, 5, 200));
    }
}
