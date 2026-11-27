using Phobos.Radio.Physics;
using Xunit;

namespace Phobos.Radio.Physics.Tests;

public class TalkRuleTests
{
    const int Hour = 2500;

    [Fact]
    public void ALoneWorkerEveryFewHoursTalksMoreOftenThanOneInCompany()
    {
        double lone = TalkRule.ChancePerLook(250, 3, Hour);
        double company = TalkRule.ChancePerLook(250, 12, Hour);
        Assert.True(lone > company);
        // A gap of three hours, looked at every tenth of an hour: about one look in thirty starts a chat.
        Assert.InRange(lone, 0.03, 0.035);
    }

    [Fact]
    public void LooksAddUpToTheGap()
    {
        // Over the gap's length, the chance of at least one chat is 1 - 1/e, whatever the look.
        double perLook = TalkRule.ChancePerLook(250, 3, Hour);
        double none = System.Math.Pow(1 - perLook, 3 * Hour / 250);
        Assert.Equal(1 - 1 / System.Math.E, 1 - none, 6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NoGapNoChat(double gap) => Assert.Equal(0, TalkRule.ChancePerLook(250, gap, Hour));

    [Theory]
    [InlineData(19, true)]
    [InlineData(20, true)]
    [InlineData(21, false)]
    [InlineData(18, false)]
    public void TheEvening(int hour, bool inside) => Assert.Equal(inside, TalkRule.InHours(hour, 19, 21));

    [Theory]
    [InlineData(22, true)]
    [InlineData(0, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [InlineData(12, false)]
    public void TheNightCrossesMidnight(int hour, bool inside) => Assert.Equal(inside, TalkRule.InHours(hour, 22, 6));

    [Fact]
    public void AnEmptySpanHoldsNoHour() => Assert.False(TalkRule.InHours(5, 5, 5));

    [Fact]
    public void APartnerComesBeforeFamilyAndFamilyBeforeAFriend()
    {
        Assert.Equal(TalkRule.Tie.Partner, TalkRule.TieOf(true, true, 100, 20));
        Assert.Equal(TalkRule.Tie.Family, TalkRule.TieOf(false, true, -50, 20));
        Assert.Equal(TalkRule.Tie.Friend, TalkRule.TieOf(false, false, 20, 20));
        Assert.Equal(TalkRule.Tie.None, TalkRule.TieOf(false, false, 19, 20));
    }

    [Fact]
    public void NobodyCallsSomeoneEitherCannotStand()
    {
        Assert.Equal(0, TalkRule.ChatWeight(-30, 50, -20));
        Assert.Equal(0, TalkRule.ChatWeight(50, -30, -20));
    }

    [Fact]
    public void ALikedOneIsCalledMoreOften()
    {
        Assert.True(TalkRule.ChatWeight(80, 80, -20) > TalkRule.ChatWeight(0, 0, -20));
        Assert.Equal(3, TalkRule.ChatWeight(100, 100, -20), 9);
        Assert.Equal(1, TalkRule.ChatWeight(-10, -10, -20), 9);
    }

    [Fact]
    public void TheyTakeTurnsToSpeak()
    {
        Assert.True(TalkRule.StarterSpeaks(0, 60));
        Assert.True(TalkRule.StarterSpeaks(59, 60));
        Assert.False(TalkRule.StarterSpeaks(60, 60));
        Assert.True(TalkRule.StarterSpeaks(120, 60));
    }

    [Fact]
    public void SinceAThingLastHappened()
    {
        Assert.True(TalkRule.Since(-1, 100, 30000));
        Assert.False(TalkRule.Since(1000, 20000, 30000));
        Assert.True(TalkRule.Since(1000, 31000, 30000));
    }
}
