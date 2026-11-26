using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class EngineSoundRuleTests
{
    [Theory]
    [InlineData(700, 0.9)]    // Idle.
    [InlineData(2200, 1.35)]  // Rated.
    [InlineData(1450, 1.125)] // Half way.
    [InlineData(300, 0.9)]    // Below idle holds at idle's pitch.
    [InlineData(3000, 1.35)]  // Over-revving holds at rated's.
    public void PitchFollowsEngineSpeed(double rpm, double expected) =>
        Assert.Equal(expected, EngineSoundRule.Pitch(rpm, 700, 2200, 0.9, 1.35), 6);

    [Fact]
    public void ABrokenRangeKeepsIdlePitch()
    {
        Assert.Equal(0.9, EngineSoundRule.Pitch(1000, 2000, 2000, 0.9, 1.35), 6);
        Assert.Equal(0.9, EngineSoundRule.Pitch(double.NaN, 700, 2200, 0.9, 1.35), 6);
    }

    [Fact]
    public void ReadingsGlideInsteadOfJumping()
    {
        Assert.Equal(1.1, EngineSoundRule.Approach(1.0, 2.0, 0.1), 6);
        Assert.Equal(2.0, EngineSoundRule.Approach(1.0, 2.0, 5), 6);
        Assert.Equal(1.4, EngineSoundRule.Approach(double.NaN, 1.4, 0.1), 6); // Nothing yet: start at the target.
    }

    [Theory]
    [InlineData(false, 0.5, true, EngineVoice.Silent)]   // Engines off: nothing, whatever else holds.
    [InlineData(true, 0.0, false, EngineVoice.Engine)]   // Standing: the idling and labouring loops.
    [InlineData(true, 0.0, true, EngineVoice.Taxi)]      // Moving on the ground under its own power.
    [InlineData(true, 0.03, true, EngineVoice.Engine)]   // Lifting off, under 5%: the loops at take-off power, neither taxiing nor aloft.
    [InlineData(true, 0.05, false, EngineVoice.Aloft)]   // Off the ground: its sound in the air.
    [InlineData(true, 1.0, true, EngineVoice.Aloft)]     // In the air, even if the pather still says it moves.
    public void TheVoiceFollowsWhereItIs(bool running, double climb, bool taxiing, EngineVoice expected) =>
        Assert.Equal(expected, EngineSoundRule.Voice(running, climb, taxiing, hasAloft: true, hasTaxi: true));

    [Fact]
    public void AVoiceWithNoSoundFallsBackToTheEngine()
    {
        Assert.Equal(EngineVoice.Engine, EngineSoundRule.Voice(true, 1.0, false, hasAloft: false, hasTaxi: true));
        Assert.Equal(EngineVoice.Engine, EngineSoundRule.Voice(true, 0.0, true, hasAloft: true, hasTaxi: false));
    }
}
