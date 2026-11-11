using System;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>The trailer's follow rule and the towed mass in the drivetrain model.</summary>
public class HitchRuleTests
{
    static double Distance(double hx, double hz, TrailerPose p) => Math.Sqrt((hx - p.AxleX) * (hx - p.AxleX) + (hz - p.AxleZ) * (hz - p.AxleZ));

    [Fact]
    public void AStraightPullStaysInLineAtTheDrawbarLength()
    {
        var pose = HitchRule.AxleFor(0, 0, 0, 5); // pointing north, axle 5 behind
        for (int step = 1; step <= 10; step++) pose = HitchRule.Follow(0, step * 0.5, pose, 5);
        Assert.Equal(0, pose.AxleX, 6);
        Assert.Equal(0, pose.Heading, 6);
        Assert.Equal(5, Distance(0, 5, pose), 6);
    }

    [Fact]
    public void ATurnLeavesTheTrailerBehindTheVehiclesHeading()
    {
        // The hitch drives north, then turns east: the trailer comes round more slowly than the vehicle did.
        var pose = HitchRule.AxleFor(0, 0, 0, 5);
        double x = 0, z = 0;
        for (int step = 0; step < 6; step++) pose = HitchRule.Follow(x += 0.5, z, pose, 5);
        Assert.InRange(pose.Heading, 1, 89);  // part way round, not yet east
        Assert.Equal(5, Distance(x, z, pose), 6);
    }

    [Fact]
    public void ALongTrailerStraightensMoreSlowlyThanAShortOne()
    {
        var longOne = HitchRule.AxleFor(0, 0, 0, 8);
        var shortOne = HitchRule.AxleFor(0, 0, 0, 3);
        for (int step = 1; step <= 4; step++)
        {
            longOne = HitchRule.Follow(step * 0.5, 0, longOne, 8);
            shortOne = HitchRule.Follow(step * 0.5, 0, shortOne, 3);
        }
        Assert.True(shortOne.Heading > longOne.Heading); // both turning toward 90, the short one further
    }

    [Fact]
    public void TheJackKnifeLimitHoldsTheTrailer()
    {
        var pose = HitchRule.AxleFor(0, 0, 170, 5);  // nearly folded back against a vehicle heading north
        var held = HitchRule.Limit(0, 0, pose, 0, 70, 5);
        Assert.Equal(70, held.Heading, 6);
        Assert.Equal(5, Distance(0, 0, held), 6);
        Assert.Equal(-60, HitchRule.Difference(300, 0), 6);
    }

    [Fact]
    public void ATowedMassSlowsTheClimbButAddsNoGrip()
    {
        var spec = new DrivetrainSpec();
        var alone = new DriveConditions { MassKg = 3000, RollingResistance = 0.015, Grade = 0.05 };
        var towing = alone;
        towing.TowedMassKg = 2000;
        Assert.True(DrivetrainModel.RoadLoad(spec, towing, 5) > DrivetrainModel.RoadLoad(spec, alone, 5));
        Assert.Equal(5000, DrivetrainModel.TrainKg(towing), 6);
    }
}
