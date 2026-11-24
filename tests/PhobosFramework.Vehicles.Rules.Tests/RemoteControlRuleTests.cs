using Phobos.Framework.Vehicles.Rules;
using Xunit;
using S = Phobos.Framework.Vehicles.Rules.RemoteState;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class RemoteControlRuleTests
{
    const int Delay = 600;

    static S Next(S state, bool linked, int lostFor = 0, LostLinkChoice choice = LostLinkChoice.Hold, bool moving = true, bool wanted = true) =>
        RemoteControlRule.Next(state, wanted, linked, lostFor, Delay, choice, moving);

    [Fact]
    public void ALinkTakesItAndTurningItOffLetsGo()
    {
        Assert.Equal(S.Linked, Next(S.Off, linked: true));
        Assert.Equal(S.Linked, Next(S.Holding, linked: true));
        Assert.Equal(S.Linked, Next(S.ComingBack, linked: true));
        Assert.Equal(S.Off, Next(S.Linked, linked: true, wanted: false));
        Assert.True(RemoteControlRule.Drives(S.Linked));
        Assert.False(RemoteControlRule.Drives(S.Off));
    }

    [Fact]
    public void ALinkNeverHadIsWaitedForNotActedOn()
    {
        Assert.Equal(S.Unlinked, Next(S.Off, linked: false));
        Assert.Equal(S.Unlinked, Next(S.Unlinked, linked: false, lostFor: 5000));
        Assert.False(RemoteControlRule.Drives(S.Unlinked));
        Assert.False(RemoteControlRule.TellsLost(S.Off, S.Unlinked));
    }

    [Fact]
    public void ALostLinkWaitsOutTheDelayAndCarriesOnMeanwhile()
    {
        Assert.Equal(S.Waiting, Next(S.Linked, linked: false));
        Assert.Equal(S.Waiting, Next(S.Waiting, linked: false, lostFor: Delay - 1));
        Assert.True(RemoteControlRule.Drives(S.Waiting));
        // Back within the delay: nothing happens, and nobody is told.
        Assert.Equal(S.Linked, Next(S.Waiting, linked: true));
        Assert.False(RemoteControlRule.TellsFound(S.Waiting, S.Linked));
    }

    [Fact]
    public void AfterTheDelayEachChoiceActs()
    {
        Assert.Equal(S.Holding, Next(S.Waiting, linked: false, lostFor: Delay, choice: LostLinkChoice.Hold));
        Assert.Equal(S.ComingBack, Next(S.Waiting, linked: false, lostFor: Delay, choice: LostLinkChoice.ComeBack));
        Assert.Equal(S.CarryingOn, Next(S.Waiting, linked: false, lostFor: Delay, choice: LostLinkChoice.CarryOn));
        // Carrying on with no drive left is holding.
        Assert.Equal(S.Holding, Next(S.Waiting, linked: false, lostFor: Delay, choice: LostLinkChoice.CarryOn, moving: false));
        Assert.False(RemoteControlRule.Drives(S.Holding));
        Assert.True(RemoteControlRule.Drives(S.ComingBack));
        Assert.True(RemoteControlRule.TellsLost(S.Waiting, S.Holding));
    }

    [Fact]
    public void ComingBackOrCarryingOnHoldsOnceItStops()
    {
        Assert.Equal(S.ComingBack, Next(S.ComingBack, linked: false, lostFor: 2000, moving: true));
        Assert.Equal(S.Holding, Next(S.ComingBack, linked: false, lostFor: 2000, moving: false));
        Assert.Equal(S.Holding, Next(S.CarryingOn, linked: false, lostFor: 2000, moving: false));
        // Stopping is no new loss: the player was told when it started acting.
        Assert.False(RemoteControlRule.TellsLost(S.ComingBack, S.Holding));
        Assert.True(RemoteControlRule.TellsFound(S.Holding, S.Linked));
    }

    [Fact]
    public void NoDelayActsAtOnce() =>
        Assert.Equal(S.Holding, RemoteControlRule.Next(S.Linked, true, false, 0, 0, LostLinkChoice.Hold, true));

    [Fact]
    public void AFailedKitHoldsAtOnceAndIsTold()
    {
        // Framework 0.284.0: whatever was chosen, no wait and no drive back; the player is told once.
        var after = RemoteControlRule.Next(S.Linked, true, false, 0, Delay, LostLinkChoice.ComeBack, true, kitDown: true);
        Assert.Equal(S.Holding, after);
        Assert.True(RemoteControlRule.TellsLost(S.Linked, after));
        Assert.Equal(S.Holding, RemoteControlRule.Next(S.Waiting, true, false, 10, Delay, LostLinkChoice.CarryOn, true, kitDown: true));
        Assert.Equal(S.Holding, RemoteControlRule.Next(S.ComingBack, true, false, 2000, Delay, LostLinkChoice.ComeBack, true, kitDown: true));
        // Never linked: it keeps waiting. Not asked: off. Back to work: linked again, and told.
        Assert.Equal(S.Unlinked, RemoteControlRule.Next(S.Off, true, false, 0, Delay, LostLinkChoice.Hold, false, kitDown: true));
        Assert.Equal(S.Off, RemoteControlRule.Next(S.Holding, false, false, 0, Delay, LostLinkChoice.Hold, false, kitDown: true));
        Assert.True(RemoteControlRule.TellsFound(S.Holding, RemoteControlRule.Next(S.Holding, true, true, 0, Delay, LostLinkChoice.Hold, false, kitDown: false)));
    }
}
