using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class SmokeRuleTests
{
    [Fact]
    public void ThePlayersTruckPopsWhenHitUnderThreatWithItsSwitchOn()
    {
        Assert.True(SmokeRule.PopsWhenHit(players: true, switchOn: true, threat: true, ticksSinceLastPop: 100000, otherPoppedThisVisit: false));
        Assert.False(SmokeRule.PopsWhenHit(players: true, switchOn: false, threat: true, ticksSinceLastPop: 100000, otherPoppedThisVisit: false));
    }

    [Fact]
    public void NoThreatNoSmoke()
    {
        // A fire or a friend's stray shot is not a reason to screen.
        Assert.False(SmokeRule.PopsWhenHit(players: true, switchOn: true, threat: false, ticksSinceLastPop: 100000, otherPoppedThisVisit: false));
        Assert.False(SmokeRule.PopsWhenHit(players: false, switchOn: false, threat: false, ticksSinceLastPop: 100000, otherPoppedThisVisit: false));
    }

    [Fact]
    public void ABurstOfHitsMakesOneScreen()
    {
        Assert.False(SmokeRule.PopsWhenHit(true, true, true, SmokeRule.PopGapTicks - 1, false));
        Assert.True(SmokeRule.PopsWhenHit(true, true, true, SmokeRule.PopGapTicks, false));
    }

    [Fact]
    public void AnotherFactionsTruckPopsOnceAVisitWhateverItsSwitch()
    {
        Assert.True(SmokeRule.PopsWhenHit(players: false, switchOn: false, threat: true, ticksSinceLastPop: 100000, otherPoppedThisVisit: false));
        Assert.False(SmokeRule.PopsWhenHit(players: false, switchOn: true, threat: true, ticksSinceLastPop: 100000, otherPoppedThisVisit: true));
    }
}

public class AmmoLockerRuleTests
{
    [Fact]
    public void TheLockerAddsItsRoomWhileOtherCargoFitsTheBed()
    {
        Assert.Equal(150, AmmoLockerRule.Room(lockerKg: 150, roundsKg: 0, otherKg: 40, bedKg: 50));
        Assert.Equal(150, AmmoLockerRule.Room(lockerKg: 150, roundsKg: 120, otherKg: 50, bedKg: 50));
        Assert.False(AmmoLockerRule.Spilled(150, otherKg: 50, bedKg: 50));
    }

    [Fact]
    public void OtherCargoPastTheBedLeavesTheLockerOnlyItsRounds()
    {
        // 60 kg of goods in a 50 kg bed, 30 kg of rounds: room is 50 + 30 = 80 for 90 aboard, so it reads 10 kg over, what spilled.
        double room = AmmoLockerRule.Room(lockerKg: 150, roundsKg: 30, otherKg: 60, bedKg: 50);
        Assert.Equal(30, room);
        Assert.Equal(10, 60 + 30 - (50 + room));
        Assert.True(AmmoLockerRule.Spilled(150, otherKg: 60, bedKg: 50));
        // Never more than the locker holds.
        Assert.Equal(150, AmmoLockerRule.Room(lockerKg: 150, roundsKg: 400, otherKg: 60, bedKg: 50));
    }

    [Fact]
    public void NoLockerNoRoom()
    {
        Assert.Equal(0, AmmoLockerRule.Room(lockerKg: 0, roundsKg: 30, otherKg: 0, bedKg: 50));
        Assert.False(AmmoLockerRule.Spilled(0, otherKg: 100, bedKg: 50));
    }
}
