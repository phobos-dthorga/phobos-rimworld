using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class PushRuleTests
{
    const double PerPerson = 25;

    [Fact]
    public void ALightPushStillTakesOnePerson()
    {
        Assert.Equal(1, PushRule.Pushers(0, PerPerson));
        Assert.Equal(1, PushRule.Pushers(3, PerPerson));
        Assert.Equal(1, PushRule.Pushers(25, PerPerson));
    }

    [Fact]
    public void PeopleAreCountedUpToTheNextWholeOne()
    {
        Assert.Equal(2, PushRule.Pushers(25.1, PerPerson));
        Assert.Equal(2, PushRule.Pushers(50, PerPerson));
        Assert.Equal(6, PushRule.Pushers(140, PerPerson));
    }

    [Fact]
    public void ACarOnFirmGroundTakesOneOrTwoAndATankTakesFarTooMany()
    {
        // 1,500 kg rolling at 0.02 of its weight is 30 kgf; 60,000 kg at the same is 1,200 kgf.
        Assert.Equal(2, PushRule.Pushers(RecoveryRule.Required(1500, 0.02), PerPerson));
        Assert.Equal(48, PushRule.Pushers(RecoveryRule.Required(60000, 0.02), PerPerson));
    }

    [Fact]
    public void NobodyPushesWithNoPushAtAll() =>
        Assert.Equal(int.MaxValue, PushRule.Pushers(30, 0));

    [Fact]
    public void ItCanBePushedOnlyByAsManyAsCanGetTheirHandsOnIt()
    {
        Assert.True(PushRule.CanPush(1, 6));
        Assert.True(PushRule.CanPush(6, 6));
        Assert.False(PushRule.CanPush(7, 6));
        Assert.False(PushRule.CanPush(int.MaxValue, 6));
    }

    [Fact]
    public void MoreHandsThanItTakesPushFasterDownToTheBasePace()
    {
        // At their limit it is twice the base pace; with twice the hands, one and a half times.
        Assert.Equal(180, PushRule.TicksPerCell(90, 50, 2, PerPerson));
        Assert.Equal(135, PushRule.TicksPerCell(90, 50, 4, PerPerson));
    }

    [Fact]
    public void NobodyPushingNeverMovesIt() =>
        Assert.Equal(int.MaxValue, PushRule.TicksPerCell(90, 50, 0, PerPerson));
}
