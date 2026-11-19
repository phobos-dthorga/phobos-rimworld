using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class UnloadBayRuleTests
{
    static BayCandidate Bay(int distance, bool takes = true, bool room = true, bool passed = false) => new(distance, takes, room, passed);

    [Fact]
    public void TheNearestBayWithRoomIsChosen() => Assert.Equal(1, UnloadBayRule.Nearest(new[] { Bay(50), Bay(10), Bay(30) }));

    [Fact]
    public void AFullBayIsPassedForTheNextNearest() => Assert.Equal(2, UnloadBayRule.Nearest(new[] { Bay(50), Bay(10, room: false), Bay(30) }));

    [Fact]
    public void ABayThatTakesNothingAboardIsNeverChosen() => Assert.Equal(-1, UnloadBayRule.Nearest(new[] { Bay(10, takes: false) }));

    [Fact]
    public void ABayPassedOverOnThisTripIsNotChosenAgain() => Assert.Equal(0, UnloadBayRule.Nearest(new[] { Bay(40), Bay(10, passed: true) }));

    [Fact]
    public void NoBaysMeansNoneMarked() => Assert.Equal(BayShortfall.NoneMarked, UnloadBayRule.WhyNone(new BayCandidate[0]));

    [Fact]
    public void BaysThatTakeNothingSaySo() => Assert.Equal(BayShortfall.NoneTakes, UnloadBayRule.WhyNone(new[] { Bay(10, takes: false, room: false) }));

    [Fact]
    public void BaysThatTakeItButAreFullSaySo() =>
        Assert.Equal(BayShortfall.NoneWithRoom, UnloadBayRule.WhyNone(new[] { Bay(10, takes: false), Bay(20, room: false) }));

    [Fact]
    public void AnEmptiedOrFullBayLooksForAnother()
    {
        Assert.True(UnloadBayRule.LooksForAnother(UnloadEnd.Emptied));
        Assert.True(UnloadBayRule.LooksForAnother(UnloadEnd.PlaceFull));
    }

    [Fact]
    public void AFaultOrAStopEndsTheTrip()
    {
        Assert.False(UnloadBayRule.LooksForAnother(UnloadEnd.Fault));
        Assert.False(UnloadBayRule.LooksForAnother(UnloadEnd.Stopped));
    }

    [Fact]
    public void ADeliveryWithNothingLeftIsDelivered() => Assert.Equal(GatherRunEnd.Delivered, UnloadBayRule.DeliveryEnd(false));

    [Fact]
    public void ADeliveryWithSomethingLeftHadNoBay() => Assert.Equal(GatherRunEnd.NoBay, UnloadBayRule.DeliveryEnd(true));

    [Fact]
    public void ItDrivesBackOnlyWhenChosenAndAway()
    {
        Assert.True(UnloadBayRule.DrivesBack(true, true, GatherRunEnd.Delivered));
        Assert.True(UnloadBayRule.DrivesBack(true, true, GatherRunEnd.NoBay));
        Assert.False(UnloadBayRule.DrivesBack(false, true, GatherRunEnd.Delivered));
        Assert.False(UnloadBayRule.DrivesBack(true, false, GatherRunEnd.Delivered));
    }

    [Fact]
    public void AFaultOrAnOrderLeavesItWhereItIs()
    {
        Assert.False(UnloadBayRule.DrivesBack(true, true, GatherRunEnd.Fault));
        Assert.False(UnloadBayRule.DrivesBack(true, true, GatherRunEnd.Orders));
        Assert.False(UnloadBayRule.DrivesBack(true, true, GatherRunEnd.Threat));
        Assert.False(UnloadBayRule.DrivesBack(true, true, GatherRunEnd.Stopped));
    }

    [Fact]
    public void ADeliveryDoneCountsAsEndingWell() => Assert.True(GatherRule.EndedWell(GatherRunEnd.Delivered));
}
