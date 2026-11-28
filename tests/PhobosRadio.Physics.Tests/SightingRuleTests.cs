using Phobos.Radio.Physics;
using Xunit;

namespace Phobos.Radio.Physics.Tests;

/// <summary>Slice 3 of life on the air (Radio 0.76.0): practice and traits in a chat (RN10), and a sighting's side of the map (RN17).</summary>
public class SightingRuleTests
{
    [Fact]
    public void AnUnpractisedVoiceIsMisheardAtTheRulesChance() => Assert.Equal(0.25, TalkRule.MishearChance(0.25, 0, 0.4), 9);

    [Fact]
    public void HalfThePracticeTakesAwayHalfTheDrop()
    {
        // At 40 hours the share is a half: 0.25 x (1 - 0.6 x 0.5) = 0.175, seven tenths of the rules' figure.
        Assert.Equal(0.175, TalkRule.MishearChance(0.25, 0.5, 0.4), 9);
    }

    [Fact]
    public void PracticeNeverGoesBelowTheFloorNorAboveTheChance()
    {
        Assert.Equal(0.1, TalkRule.MishearChance(0.25, 5, 0.4), 9);
        Assert.Equal(0.25, TalkRule.MishearChance(0.25, -1, 0.4), 9);
        Assert.Equal(0, TalkRule.MishearChance(-0.1, 0.5, 0.4), 9);
    }

    [Theory]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, false, true)]
    [InlineData(false, false, false, false)]
    [InlineData(true, true, true, false)]
    public void AKindVoiceSmoothsAChatOver(bool misheard, bool abrasive, bool kind, bool slight) =>
        Assert.Equal(slight, TalkRule.Slight(misheard, abrasive, kind));

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(10, 10, 45)]
    [InlineData(10, 0, 90)]
    [InlineData(0, -10, 180)]
    [InlineData(-10, 0, 270)]
    [InlineData(-10, 10, 315)]
    public void TheCompassReadsClockwiseFromNorth(double dx, double dz, double degrees) =>
        Assert.Equal(degrees, SightingRule.Bearing(dx, dz), 6);

    [Fact]
    public void TheMiddleItselfReadsNorth() => Assert.Equal(0, SightingRule.Bearing(0, 0));
}
