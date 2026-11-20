using System.Collections.Generic;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class BayTripRuleTests
{
    [Theory]
    [InlineData(BayTripTrigger.BayOnly, BayTripWorkKind.BayOnly, true)]
    [InlineData(BayTripTrigger.BayOnly, BayTripWorkKind.Broken, false)]
    [InlineData(BayTripTrigger.BayOnly, BayTripWorkKind.Routine, false)]
    [InlineData(BayTripTrigger.Broken, BayTripWorkKind.BayOnly, true)]
    [InlineData(BayTripTrigger.Broken, BayTripWorkKind.Broken, true)]
    [InlineData(BayTripTrigger.Broken, BayTripWorkKind.Routine, false)]
    [InlineData(BayTripTrigger.Any, BayTripWorkKind.BayOnly, true)]
    [InlineData(BayTripTrigger.Any, BayTripWorkKind.Broken, true)]
    [InlineData(BayTripTrigger.Any, BayTripWorkKind.Routine, true)]
    public void EachLevelSendsWhatTheOneBelowDoesAndMore(BayTripTrigger level, BayTripWorkKind kind, bool sends) =>
        Assert.Equal(sends, BayTripRule.Sends(level, kind));

    static readonly Dictionary<string, int> Stock = new() { ["battery"] = 1, ["steel"] = 40 };

    static bool Met((string what, int count) line) => Stock.TryGetValue(line.what, out int have) && have >= line.count;

    [Fact]
    public void WorkThatAsksForNothingCanBePaidFor() =>
        Assert.True(BayTripRule.Payable(new List<List<(string, int)>>(), Met));

    [Fact]
    public void AnEmptyWayOfPayingCanBePaidFor() =>
        Assert.True(BayTripRule.Payable(new List<List<(string, int)>> { new() }, Met));

    [Fact]
    public void OneWayOfPayingWithEveryLineMetIsEnough()
    {
        var options = new List<List<(string, int)>> { new() { ("starter", 1) }, new() { ("steel", 30), ("battery", 1) } };
        Assert.True(BayTripRule.Payable(options, Met));
    }

    [Fact]
    public void AWayOfPayingShortOfOneLineDoesNotCount()
    {
        var options = new List<List<(string, int)>> { new() { ("steel", 30), ("starter", 1) }, new() { ("steel", 50) } };
        Assert.False(BayTripRule.Payable(options, Met));
    }

    [Fact]
    public void ALookIsDueOnItsTickAndNotBefore()
    {
        Assert.False(BayTripRule.LookDue(999, 1000));
        Assert.True(BayTripRule.LookDue(1000, 1000));
    }

    [Fact]
    public void TheNextLookIsAnIntervalOnMovedByTheVehiclesId()
    {
        Assert.Equal(2000, BayTripRule.NextLook(1000, 1000, 500, 250));
        Assert.Equal(2007, BayTripRule.NextLook(1000, 1000, 507, 250));
        Assert.Equal(2007, BayTripRule.NextLook(1000, 1000, -507, 250));
        Assert.Equal(2000, BayTripRule.NextLook(1000, 1000, 507, 0));
    }

    [Fact]
    public void AWaitIsOverOnlyPastItsLimitAndNeverWithNone()
    {
        Assert.False(BayTripRule.Overstayed(60000, 60000));
        Assert.True(BayTripRule.Overstayed(60001, 60000));
        Assert.False(BayTripRule.Overstayed(999999, 0));
    }

    [Theory]
    [InlineData(true, true, BayTripHome.DepotBay)]
    [InlineData(true, false, BayTripHome.DepotBay)]
    [InlineData(false, true, BayTripHome.Park)]
    [InlineData(false, false, BayTripHome.Nearby)]
    public void HomeIsTheDepotBayThenTheParkThenNearby(bool depotBay, bool park, BayTripHome home) =>
        Assert.Equal(home, BayTripRule.Home(depotBay, park));
}
