using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class RouteRuleTests
{
    [Fact]
    public void AFollowerAimsBehindTheVehicleAhead()
    {
        // Ahead at (50, 50) facing north (z up): the target is south of it.
        Assert.Equal((50, 42), RouteRule.FollowTarget((50, 50), (0, 1), 8));
        // Facing east: the target is west of it.
        Assert.Equal((42, 50), RouteRule.FollowTarget((50, 50), (1, 0), 8));
    }

    [Fact]
    public void TheGapCountsBothVehiclesHalfLengths() => Assert.Equal(5 + 4 + 2, RouteRule.FollowGap(10, 8, 2));

    [Fact]
    public void ANewOrderOnlyWhenOneIsNeeded()
    {
        Assert.False(RouteRule.NeedsOrder(1, driving: false, 0, 2, 3));   // close enough: stay
        Assert.True(RouteRule.NeedsOrder(10, driving: false, 0, 2, 3));   // far, no drive: go
        Assert.False(RouteRule.NeedsOrder(10, driving: true, 2, 2, 3));   // far, driving near the target already: leave it
        Assert.True(RouteRule.NeedsOrder(10, driving: true, 6, 2, 3));    // the leader moved on: a fresh order
    }

    [Fact]
    public void AtMostFourFollow()
    {
        Assert.True(RouteRule.MayJoin(3, 4));
        Assert.False(RouteRule.MayJoin(4, 4));
    }

    [Fact]
    public void ALowBatteryKeepsTheEngineOnAtAStopUntilCharged()
    {
        // Framework 0.286.0: on below a quarter, off again above half; nothing in between changes it.
        Assert.False(RouteRule.ChargesAtStop(false, 0.6, 0.25, 0.5));
        Assert.False(RouteRule.ChargesAtStop(false, 0.3, 0.25, 0.5));
        Assert.True(RouteRule.ChargesAtStop(false, 0.2, 0.25, 0.5));
        Assert.True(RouteRule.ChargesAtStop(true, 0.4, 0.25, 0.5));
        Assert.False(RouteRule.ChargesAtStop(true, 0.55, 0.25, 0.5));
        Assert.False(RouteRule.ChargesAtStop(false, double.NaN, 0.25, 0.5));
    }

    [Fact]
    public void ALeaderJoiningBringsItsFollowersIntoTheCount()
    {
        // Framework 0.284.0: two following one leader, joining a convoy of two, makes five behind it: refused.
        Assert.False(RouteRule.MayJoin(2, 3, 4));
        Assert.True(RouteRule.MayJoin(1, 3, 4));
        Assert.True(RouteRule.MayJoin(3, 0, 4));    // nobody joins as fewer than one
        Assert.False(RouteRule.MayJoin(4, 0, 4));
    }

    [Fact]
    public void ALoopComesBackToItsFirstPoint()
    {
        Assert.Equal(1, RouteRule.Next(0, 3));
        Assert.Equal(0, RouteRule.Next(2, 3));
        Assert.True(RouteRule.Reached(2.5, 3));
        Assert.False(RouteRule.Reached(4, 3));
    }

    [Fact]
    public void AWaypointFallsOnTheLegItLengthensLeast()
    {
        // Pick-up at (0, 0), drop-off at (100, 0): a point beside the way out goes between them...
        var points = new List<(int, int)> { (0, 0), (100, 0) };
        Assert.Equal(1, RouteRule.InsertAt(points, (50, 10)));
        // ...and with a second waypoint far to the south on the way back, a point near it goes after the drop-off.
        var loop = new List<(int, int)> { (0, 0), (50, 10), (100, 0), (50, -60) };
        Assert.Equal(4, RouteRule.InsertAt(loop, (20, -40)));
    }
}
