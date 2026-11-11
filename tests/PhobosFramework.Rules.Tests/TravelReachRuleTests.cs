using System;
using System.Collections.Generic;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class TravelReachRuleTests
{
    // A row of nodes 0..9, each step costing 1.
    static void Row(int node, List<int> around)
    {
        if (node > 0) around.Add(node - 1);
        if (node < 9) around.Add(node + 1);
    }

    // A 5 x 5 grid, node = y * 5 + x.
    static void Grid(int node, List<int> around)
    {
        int x = node % 5, y = node / 5;
        if (x > 0) around.Add(node - 1);
        if (x < 4) around.Add(node + 1);
        if (y > 0) around.Add(node - 5);
        if (y < 4) around.Add(node + 5);
    }

    [Fact]
    public void TheStartCostsNothing() => Assert.Equal(0, TravelReachRule.Within(3, 0, Row, (_, _) => 1, 100)[3]);

    [Fact]
    public void ABudgetReachesExactlyThatFar()
    {
        Dictionary<int, double> reached = TravelReachRule.Within(5, 2, Row, (_, _) => 1, 100);
        Assert.Equal(new[] { 3, 4, 5, 6, 7 }, Sorted(reached.Keys));
    }

    [Fact]
    public void TheCheaperWayIsTaken()
    {
        // Going right costs 1 a step, going left 3: three steps right fit a budget of 3, one step left does too.
        Dictionary<int, double> reached = TravelReachRule.Within(5, 3, Row, (from, to) => to > from ? 1 : 3, 100);
        Assert.Equal(new[] { 4, 5, 6, 7, 8 }, Sorted(reached.Keys));
        Assert.Equal(3, reached[8]);
    }

    [Fact]
    public void ARouteAroundAWallIsFound()
    {
        // A wall down column 2 except its bottom cell: from (0,0) to (4,0) the way is round the bottom.
        double Cost(int from, int to) => to % 5 == 2 && to / 5 < 4 ? -1 : 1;
        Dictionary<int, double> reached = TravelReachRule.Within(0, 100, Grid, Cost, 100);
        Assert.Equal(12, reached[4]);
        Assert.False(reached.ContainsKey(2));
    }

    [Fact]
    public void AnImpassableStepIsNotTaken() => Assert.Single(TravelReachRule.Within(5, 10, Row, (_, _) => double.PositiveInfinity, 100));

    [Fact]
    public void TheNodeLimitStopsTheSearch() => Assert.True(TravelReachRule.Within(0, 1000, Grid, (_, _) => 1, 4).Count < 25);

    [Fact]
    public void TheBoundaryIsWhereReachEnds()
    {
        Dictionary<int, double> reached = TravelReachRule.Within(5, 1, Row, (_, _) => 1, 100);
        List<(int inside, int outside)> edges = TravelReachRule.Boundary(reached, Row);
        Assert.Equal(2, edges.Count);
        Assert.Contains((4, 3), edges);
        Assert.Contains((6, 7), edges);
    }

    static int[] Sorted(IEnumerable<int> keys)
    {
        var list = new List<int>(keys);
        list.Sort();
        return list.ToArray();
    }
}
