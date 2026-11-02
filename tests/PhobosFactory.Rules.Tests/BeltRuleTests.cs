using Phobos.Factory.Rules;
using Xunit;

namespace Phobos.Factory.Rules.Tests;

public class BeltRuleTests
{
    [Fact]
    public void AnEmptyWaitingBeltIsStuck() =>
        // The owner's PG6 save, 30 Oct 2026: both belts of the human-meat line were like this.
        Assert.True(BeltRule.IsStuck(waiting: true, heldStacks: 0));

    [Fact]
    public void ABeltWaitingWithAnItemIsQueueing() =>
        // A full belt waits for room ahead; VFE Factory moves it on by itself.
        Assert.False(BeltRule.IsStuck(waiting: true, heldStacks: 1));

    [Fact]
    public void AnEmptyBeltThatIsNotWaitingStillPulls() =>
        Assert.False(BeltRule.IsStuck(waiting: false, heldStacks: 0));

    [Fact]
    public void AHopperSwitchedOffRefusesBelts()
    {
        Assert.True(BeltRule.RefusesBelts(mayTake: false, startsOn: true));
        Assert.False(BeltRule.IsSwitchedOn(mayTake: false, startsOn: true));
    }

    [Fact]
    public void AHopperLeftOnIsVfeFactorysToFeedFrom()
    {
        // Neither of this mod's rules touches it, so stock hoppers behave as VFE Factory wrote them.
        Assert.False(BeltRule.RefusesBelts(mayTake: true, startsOn: true));
        Assert.False(BeltRule.IsSwitchedOn(mayTake: true, startsOn: true));
    }

    [Fact]
    public void OrdinaryStorageSwitchedOnFeedsBelts()
    {
        Assert.True(BeltRule.IsSwitchedOn(mayTake: true, startsOn: false));
        Assert.False(BeltRule.RefusesBelts(mayTake: true, startsOn: false));
    }

    [Fact]
    public void OrdinaryStorageLeftOffIsNotOurConcern()
    {
        // VFE Factory never takes from it anyway, so refusing would only cost a lookup.
        Assert.False(BeltRule.IsSwitchedOn(mayTake: false, startsOn: false));
        Assert.False(BeltRule.RefusesBelts(mayTake: false, startsOn: false));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, true, true)]
    public void InspectTextMentionsOnlyAChangedSwitch(bool mayTake, bool startsOn, bool shows) =>
        Assert.Equal(shows, BeltRule.ShowsInInspect(mayTake, startsOn));
}
