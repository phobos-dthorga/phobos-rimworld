using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class RoundRuleTests
{
    [Fact]
    public void AStopsWaitIsInGameHours()
    {
        Assert.Equal(2500, RoundRule.WaitTicks(1f));
        Assert.Equal(1250, RoundRule.WaitTicks(0.5f));
        Assert.Equal(0, RoundRule.WaitTicks(-3f));
        Assert.Equal(RoundRule.WaitTicks(RoundRule.DefaultWaitHours), RoundRule.WaitTicks(1f));
        Assert.Contains(RoundRule.DefaultWaitHours, RoundRule.WaitChoices);
    }

    [Fact]
    public void ALegNeedsExactlyItsFuel()
    {
        // The owner's answer (26 Nov 2026): exactly the leg's fuel, no margin.
        Assert.True(RoundRule.HasFuelFor(aboard: 100, needed: 100));
        Assert.False(RoundRule.HasFuelFor(aboard: 99, needed: 100));
        Assert.False(RoundRule.HasFuelFor(aboard: 110, needed: 100, margin: 1.15));
        // An aircraft that burns nothing on the way always has.
        Assert.True(RoundRule.HasFuelFor(aboard: 0, needed: 0));
    }

    [Fact]
    public void ItLeavesOnlyOnceItsWaitIsOver()
    {
        Assert.False(RoundRule.WaitOver(now: 100, waitUntil: -1));
        Assert.False(RoundRule.WaitOver(100, 200));
        Assert.True(RoundRule.WaitOver(200, 200));
    }

    [Fact]
    public void ARoundHasTwoPlacesAtLeastAndNoRepeats()
    {
        Assert.True(RoundRule.MayAdd(stopsSoFar: 1, sameAsLast: false));
        Assert.False(RoundRule.MayAdd(1, sameAsLast: true));
        Assert.False(RoundRule.MayAdd(RoundRule.MostStops, false));
        Assert.False(RoundRule.Flyable(stops: 1, lastIsFirst: false));
        Assert.True(RoundRule.Flyable(2, false));
        // There and back to the same place is no round.
        Assert.False(RoundRule.Flyable(2, lastIsFirst: true));
        Assert.True(RoundRule.Flyable(3, lastIsFirst: true));
    }
}
