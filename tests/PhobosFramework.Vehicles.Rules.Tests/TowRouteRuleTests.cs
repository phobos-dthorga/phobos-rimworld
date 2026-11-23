using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>The wider way round for a vehicle that tows: the clearance costs near ground a trailer cannot stand on.</summary>
public class TowRouteRuleTests
{
    static bool[] Open(int width, int height) => new bool[width * height];

    [Fact]
    public void DistanceCountsADiagonalStepAsOne()
    {
        // A 9 x 9 field with one wall cell in the middle: the eight around it are 1 away, the ring beyond 2.
        bool[] blocked = Open(9, 9);
        blocked[4 + 4 * 9] = true;
        int[] d = TowRouteRule.Distances(blocked, 9, 9, 10);
        Assert.Equal(0, d[4 + 4 * 9]);
        Assert.Equal(1, d[5 + 5 * 9]);
        Assert.Equal(1, d[3 + 4 * 9]);
        Assert.Equal(2, d[6 + 6 * 9]);
        Assert.Equal(2, d[4 + 2 * 9]);
    }

    [Fact]
    public void TheMapsEdgeCountsAsGroundATrailerCannotStandOn()
    {
        int[] d = TowRouteRule.Distances(Open(9, 9), 9, 9, 10);
        Assert.Equal(1, d[0]);
        Assert.Equal(1, d[8 + 4 * 9]);
        Assert.Equal(5, d[4 + 4 * 9]);
    }

    [Fact]
    public void DistancesStopAtTheCap()
    {
        int[] d = TowRouteRule.Distances(Open(21, 21), 21, 21, 4);
        Assert.Equal(4, d[10 + 10 * 21]);
        Assert.Equal(3, d[2 + 10 * 21]);
    }

    [Fact]
    public void TheCostFallsAwayFromAWallAndNothingIsAddedOnIt()
    {
        // A wall along x = 0 of a 12 x 12 field (and the map's edge all round).
        bool[] blocked = Open(12, 12);
        for (int z = 0; z < 12; z++) blocked[z * 12] = true;
        short[] cost = TowRouteRule.ClearanceCosts(blocked, 12, 12, new[] { 3, 2, 1 });
        int row = 6 * 12;
        Assert.Equal(0, cost[row + 0]);
        Assert.Equal(3, cost[row + 1]);
        Assert.Equal(2, cost[row + 2]);
        Assert.Equal(1, cost[row + 3]);
        Assert.Equal(0, cost[row + 4]);
        Assert.Equal(0, cost[row + 6]);
    }

    [Fact]
    public void NoCostsLeaveTheMapFree()
    {
        bool[] blocked = Open(5, 5);
        blocked[12] = true;
        Assert.All(TowRouteRule.ClearanceCosts(blocked, 5, 5, new int[0]), c => Assert.Equal(0, c));
    }

    [Fact]
    public void TheStepsBetweenTwoPointsEndOnTheSecond()
    {
        var steps = TowRouteRule.Between(0, 0, 1, 2, 4).ToList();
        Assert.Equal(4, steps.Count);
        Assert.Equal((0.25, 0.5), steps[0]);
        Assert.Equal((1.0, 2.0), steps[3]);
    }

    [Fact]
    public void TheHitchStandsBehindTheMiddleAlongTheHeading()
    {
        (double x, double z) = HitchRule.BehindPoint(10, 10, 90, 2);
        Assert.Equal(8, x, 6);
        Assert.Equal(10, z, 6);
        (x, z) = HitchRule.BehindPoint(10, 10, 0, 2);
        Assert.Equal(10, x, 6);
        Assert.Equal(8, z, 6);
    }
}
