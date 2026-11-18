using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class GatherRuleTests
{
    [Fact]
    public void TheTalusStandsFiveCellsAway() => Assert.Equal(5, GatherRule.StandOffRing(3, 8));

    [Fact]
    public void TheRingIsTheSameWhicheverSideIsLong() => Assert.Equal(GatherRule.StandOffRing(8, 3), GatherRule.StandOffRing(3, 8));

    [Fact]
    public void AnOddLengthRunsHalfOfOneLessEachWay() => Assert.Equal(5, GatherRule.StandOffRing(3, 9));

    [Fact]
    public void AOneCellVehicleStandsBeside() => Assert.Equal(1, GatherRule.StandOffRing(1, 1));

    [Fact]
    public void ANothingSizedVehicleStillStandsBeside() => Assert.Equal(1, GatherRule.StandOffRing(0, 0));

    [Fact]
    public void TheTalusGrabReachesFromTheRingInAnyFacing() => Assert.True(GatherRule.RingAlwaysInReach(3, 8, 4));

    [Fact]
    public void ALongerBodyWithTheSameReachDoesNot() => Assert.False(GatherRule.RingAlwaysInReach(3, 10, 4));

    [Fact]
    public void AWiderBodyReachesFurther() => Assert.True(GatherRule.RingAlwaysInReach(4, 10, 5));

    [Fact]
    public void NothingInReachMeansLookAgain() => Assert.Null(GatherRule.AfterWork(GatherWorkEnd.NothingInReach));

    [Fact]
    public void AFullCargoEndsTheRun() => Assert.Equal(GatherRunEnd.Full, GatherRule.AfterWork(GatherWorkEnd.Full));

    [Fact]
    public void AFaultEndsTheRun() => Assert.Equal(GatherRunEnd.Fault, GatherRule.AfterWork(GatherWorkEnd.Fault));

    [Fact]
    public void StowingTheGrabByHandEndsTheRun() => Assert.Equal(GatherRunEnd.Stopped, GatherRule.AfterWork(GatherWorkEnd.Stopped));

    [Fact]
    public void StandingStillPastItsPatienceIsStuck() => Assert.True(GatherRule.Stuck(2001, 2000));

    [Fact]
    public void StandingStillUpToItsPatienceIsNot() => Assert.False(GatherRule.Stuck(2000, 2000));

    [Fact]
    public void NoPatienceIsNeverStuck() => Assert.False(GatherRule.Stuck(1_000_000, 0));

    [Fact]
    public void FiveFailedDrivesGiveUp() => Assert.True(GatherRule.GiveUp(5, 5));

    [Fact]
    public void FourDoNot() => Assert.False(GatherRule.GiveUp(4, 5));

    [Fact]
    public void FullAndNothingLeftEndWell()
    {
        Assert.True(GatherRule.EndedWell(GatherRunEnd.Full));
        Assert.True(GatherRule.EndedWell(GatherRunEnd.NothingLeft));
        Assert.False(GatherRule.EndedWell(GatherRunEnd.Threat));
        Assert.False(GatherRule.EndedWell(GatherRunEnd.Orders));
    }

    [Fact]
    public void ADoneRunSwitchesOffTheEngineItStarted()
    {
        Assert.True(GatherRule.SwitchesOffAtEnd(GatherRunEnd.Full));
        Assert.True(GatherRule.SwitchesOffAtEnd(GatherRunEnd.NothingLeft));
        Assert.True(GatherRule.SwitchesOffAtEnd(GatherRunEnd.Stopped));
    }

    [Fact]
    public void OtherOrdersAndEnemiesLeaveTheEngineRunning()
    {
        Assert.False(GatherRule.SwitchesOffAtEnd(GatherRunEnd.Orders));
        Assert.False(GatherRule.SwitchesOffAtEnd(GatherRunEnd.Threat));
        Assert.False(GatherRule.SwitchesOffAtEnd(GatherRunEnd.EngineOff));
    }
}
