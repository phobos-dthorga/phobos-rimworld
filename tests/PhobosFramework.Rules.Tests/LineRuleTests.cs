using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class LineRuleTests
{
    [Fact]
    public void ThePublishedFiguresConvert()
    {
        // The guide's 600 lb hoist: 272 kg. The Army manual's 125 ft a minute at that load, 250 ft empty: 0.64 and 1.27 m/s.
        Assert.Equal(272.2, 600 * LineRule.KilogramsPerPound, 1);
        Assert.Equal(0.635, LineRule.FeetPerMinute(125), 3);
        Assert.Equal(1.27, LineRule.FeetPerMinute(250), 2);
    }

    [Fact]
    public void ItComesDownToItsWorkHeight()
    {
        Assert.Equal(1.0 / 3, LineRule.WorkClimb(20, 60), 6);
        Assert.Equal(1, LineRule.WorkClimb(80, 60), 6);
        Assert.Equal(1, LineRule.WorkClimb(20, 0), 6);
    }

    [Fact]
    public void ALiftFromTheWorkHeightTakesAboutAMinute()
    {
        // 20 m down empty at 1.2, 10 s to hook up, 20 m up at 0.7: 16.7 + 10 + 28.6 = 55.2 s.
        Assert.Equal(55.2, LineRule.LiftSeconds(20, 1.2, 0.7, 10), 1);
        Assert.Equal(10, LineRule.LiftSeconds(0, 1.2, 0.7, 10), 6);
    }

    [Fact]
    public void TheHoistTakesUpToItsRating()
    {
        Assert.True(LineRule.HoistTakes(272, 272));
        Assert.False(LineRule.HoistTakes(272.5, 272));
    }

    [Fact]
    public void AFallHurtsByItsHeight()
    {
        Assert.Equal(0, LineRule.FallHarm(1), 6);
        Assert.Equal(37, LineRule.FallHarm(20), 6);
        Assert.Equal(12, LineRule.HeightOnCable(20, 8), 6);
        Assert.Equal(0, LineRule.HeightOnCable(20, 25), 6);
    }
}
