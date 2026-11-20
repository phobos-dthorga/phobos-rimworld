using System;
using System.Collections.Generic;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class GridPathRuleTests
{
    /// <summary>A map from rows of text, the first row the highest z: '#' is blocked, anything else open; outside it is blocked.</summary>
    static Func<int, int, bool> Map(params string[] rows) => (x, z) =>
    {
        int row = rows.Length - 1 - z;
        return row >= 0 && row < rows.Length && x >= 0 && x < rows[row].Length && rows[row][x] != '#';
    };

    [Fact]
    public void StandingOnTheGoalIsAnEmptyWay() =>
        Assert.Empty(GridPathRule.Find(2, 2, 2, 2, (_, _) => false, 100)!);

    [Fact]
    public void AnOpenFieldGivesAShortestWayEndingOnTheGoal()
    {
        List<(int X, int Z)>? path = GridPathRule.Find(0, 0, 3, 2, Map("....", "....", "...."), 100);
        Assert.NotNull(path);
        Assert.Equal(5, path!.Count);
        Assert.Equal((3, 2), path[^1]);
    }

    [Fact]
    public void EveryStepIsOneCellNorthSouthEastOrWest()
    {
        List<(int X, int Z)> path = GridPathRule.Find(0, 0, 4, 0, Map(".....", ".###.", "..#.."), 200)!;
        (int X, int Z) at = (0, 0);
        foreach ((int X, int Z) cell in path)
        {
            Assert.Equal(1, Math.Abs(cell.X - at.X) + Math.Abs(cell.Z - at.Z));
            at = cell;
        }
        Assert.Equal((4, 0), at);
    }

    [Fact]
    public void AWallIsGoneRound()
    {
        // Straight across is 4 steps; the wall in the bottom row forces the way up and over: 8.
        List<(int X, int Z)> path = GridPathRule.Find(0, 0, 4, 0, Map(".....", ".###.", "..#.."), 200)!;
        Assert.Equal(8, path.Count);
        Assert.DoesNotContain((2, 0), path);
    }

    [Fact]
    public void AWalledInGoalHasNoWay() =>
        Assert.Null(GridPathRule.Find(0, 0, 4, 0, Map("...#.", "...#."), 200));

    [Fact]
    public void ABlockedGoalHasNoWay() =>
        Assert.Null(GridPathRule.Find(0, 0, 2, 0, Map("..#"), 200));

    [Fact]
    public void TheSearchGivesUpPastItsLimit()
    {
        Func<int, int, bool> open = (_, _) => true;
        Assert.Null(GridPathRule.Find(0, 0, 60, 60, open, 20));
        Assert.NotNull(GridPathRule.Find(0, 0, 3, 0, open, 20));
    }

    [Fact]
    public void TheStartIsNeverTestedAndEachOtherCellAtMostOnce()
    {
        var asked = new Dictionary<(int, int), int>();
        Func<int, int, bool> map = Map(".....", ".###.", "..#..");
        GridPathRule.Find(0, 0, 4, 0, (x, z) =>
        {
            asked[(x, z)] = asked.GetValueOrDefault((x, z)) + 1;
            return map(x, z);
        }, 200);
        Assert.False(asked.ContainsKey((0, 0)));
        Assert.All(asked.Values, count => Assert.Equal(1, count));
    }
}
