using System;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class OrbitRuleTests
{
    [Fact]
    public void TheTightestCircleIsTheSpeedOverTheTurnRate()
    {
        // 5 cells a second turning 35 degrees a second: 5 / 0.6109 = 8.18 cells.
        Assert.Equal(8.185, OrbitRule.TightestRadius(5, 35), 3);
        Assert.True(double.IsPositiveInfinity(OrbitRule.TightestRadius(5, 0)));
    }

    [Fact]
    public void OnTheCircleItSteersAlongTheTangentTheWayRound()
    {
        // North of the centre, clockwise is east and anticlockwise west.
        Assert.Equal(90, OrbitRule.Heading(0, 10, 0, 0, 10, clockwise: true, current: 0), 6);
        Assert.Equal(270, OrbitRule.Heading(0, 10, 0, 0, 10, clockwise: false, current: 0), 6);
        // East of the centre, clockwise is south.
        Assert.Equal(180, OrbitRule.Heading(10, 0, 0, 0, 10, clockwise: true, current: 0), 6);
    }

    [Fact]
    public void OffTheCircleItTurnsBackOntoIt()
    {
        // Far outside, north of the centre going clockwise: nearly straight at the centre (south), offset toward the way round.
        Assert.Equal(90 + OrbitRule.MaxCorrectionDegrees, OrbitRule.Heading(0, 100, 0, 0, 10, true, 0), 6);
        // Just inside: turned out from the tangent.
        Assert.True(OrbitRule.Heading(0, 8, 0, 0, 10, true, 0) < 90);
        // At the centre it keeps its heading.
        Assert.Equal(42, OrbitRule.Heading(0, 0, 0, 0, 10, true, 42), 6);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FlownWithATurnLimitItSettlesOntoTheCircle(bool clockwise)
    {
        // The Osprey's figures: 5 cells a second, 35 degrees a second, a circle of 12 against its tightest of 8.2; from 40 cells away.
        double x = 0, z = -40, heading = 0, speed = 5, turn = 35, radius = 12, dt = 1.0 / 60;
        double lastBearing = double.NaN, travelled = 0;
        for (int i = 0; i < 60 * 120; i++)
        {
            double wanted = OrbitRule.Heading(x, z, 0, 0, radius, clockwise, heading);
            heading = FlightPathRule.TurnToward(heading, wanted, turn * dt);
            double rad = heading * Math.PI / 180;
            x += Math.Sin(rad) * speed * dt;
            z += Math.Cos(rad) * speed * dt;
            if (i >= 60 * 100)
            {
                double bearing = Math.Atan2(x, z) * 180 / Math.PI;
                if (!double.IsNaN(lastBearing)) travelled += FlightPathRule.Turn(lastBearing, bearing);
                lastBearing = bearing;
                Assert.InRange(Math.Sqrt(x * x + z * z), radius - 1, radius + 1);
            }
        }
        // Over the last 20 seconds it went round the way it was told: clockwise is a rising bearing.
        Assert.True(clockwise ? travelled > 90 : travelled < -90, $"travelled {travelled}");
    }

    [Fact]
    public void ACircleBegunOnTheMoveHasItsCentreBesideTheAircraft()
    {
        // Flying north, a clockwise circle's centre is to the east; the aircraft is then on the circle, on its tangent.
        (double cx, double cz) = OrbitRule.CentreFrom(0, 0, 0, 12, clockwise: true);
        Assert.Equal(12, cx, 6);
        Assert.Equal(0, cz, 6);
        Assert.Equal(0, FlightPathRule.Turn(0, OrbitRule.Heading(0, 0, cx, cz, 12, true, 0)), 6);
        (double ax, _) = OrbitRule.CentreFrom(0, 0, 0, 12, clockwise: false);
        Assert.Equal(-12, ax, 6);
    }

    [Fact]
    public void TheCircleIsKeptInsideTheMap()
    {
        Assert.Equal((20.0, 50.0), OrbitRule.FitCentre(5, 50, 15, 5, 245, 5, 245));
        Assert.Equal((230.0, 230.0), OrbitRule.FitCentre(250, 250, 15, 5, 245, 5, 245));
        Assert.Equal(15, OrbitRule.FitRadius(15, 5, 245, 5, 245), 6);
        Assert.Equal(10, OrbitRule.FitRadius(40, 0, 20, 0, 100), 6);
    }
}
