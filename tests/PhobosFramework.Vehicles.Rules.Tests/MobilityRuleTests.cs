using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class MobilityRuleTests
{
    static readonly string[] Suffixes = { "Track" };

    [Fact]
    public void ATrackFeedingSpeedIsATrack() => Assert.True(MobilityRule.IsTrack("LeftTrack", feedsSpeed: true, Suffixes));

    [Fact]
    public void ACaseDifferenceDoesNotMatter() => Assert.True(MobilityRule.IsTrack("right_track", feedsSpeed: true, new[] { "TRACK" }));

    [Fact]
    public void TrackArmourThatFeedsNoSpeedIsNotATrack() => Assert.False(MobilityRule.IsTrack("SkirtOverTrack", feedsSpeed: false, Suffixes));

    [Fact]
    public void RoadWheelsAreNotTracks() => Assert.False(MobilityRule.IsTrack("LeftRoadWheels", feedsSpeed: true, Suffixes));

    [Fact]
    public void ADestroyedTrackIsThrown() => Assert.True(MobilityRule.Thrown(0, 0));

    [Fact]
    public void ADamagedTrackStillRuns() => Assert.False(MobilityRule.Thrown(0.05, 0));

    [Fact]
    public void AThrownTrackIsNamedFirst()
    {
        var parts = new[] { new SpeedPart(0, false, true), new SpeedPart(1, true, false), new SpeedPart(0, true, false) };
        Assert.Equal(2, MobilityRule.Cause(parts, 0));
    }

    [Fact]
    public void ADeadEngineIsNamedBeforeAWornWheel()
    {
        var parts = new[] { new SpeedPart(0.3, false, false), new SpeedPart(0, false, true) };
        Assert.Equal(1, MobilityRule.Cause(parts, 0));
    }

    [Fact]
    public void OtherwiseTheWeakestPartIsNamed()
    {
        var parts = new[] { new SpeedPart(0.6, false, false), new SpeedPart(0.1, false, false), new SpeedPart(1, false, true) };
        Assert.Equal(1, MobilityRule.Cause(parts, 0));
    }

    [Fact]
    public void NothingDamagedNamesNothing() => Assert.Equal(-1, MobilityRule.Cause(new[] { new SpeedPart(1, false, true) }, 0));
}
