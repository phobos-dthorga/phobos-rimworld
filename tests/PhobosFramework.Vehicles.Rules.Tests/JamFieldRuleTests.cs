using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class JamFieldRuleTests
{
    [Theory]
    [InlineData(30, 30, 44, true)]
    [InlineData(44, 0, 44, true)]
    [InlineData(32, 32, 44, false)]
    [InlineData(0, 0, 0, false)]
    public void APointIsInsideWithinTheRadius(double dx, double dz, double radius, bool inside) => Assert.Equal(inside, JamFieldRule.Inside(dx, dz, radius));

    [Fact]
    public void OnlyTheListedCapabilitiesAreBlinded()
    {
        var jammed = new[] { "radar", "warningReceiver" };
        Assert.True(JamFieldRule.Blinds(jammed, "radar"));
        Assert.True(JamFieldRule.Blinds(jammed, "warningReceiver"));
        Assert.False(JamFieldRule.Blinds(jammed, "nightSight"));
        Assert.False(JamFieldRule.Blinds(jammed, "autopilot"));
    }

    [Fact]
    public void RadarAloneKeepsThePodsOnRadarAndTheRadioJammerSilent()
    {
        Assert.Equal(new AircraftJamShare(1f, 0f, false), JamFieldRule.Share(radar: true, radios: false, commsJammer: true, 0.5f));
        Assert.Equal(new AircraftJamShare(1f, 0f, false), JamFieldRule.Share(radar: true, radios: false, commsJammer: false, 0.5f));
    }

    [Fact]
    public void RadiosAloneTurnThePodsToRadiosWithTheRadioJammerBeside()
    {
        Assert.Equal(new AircraftJamShare(0f, 1f, true), JamFieldRule.Share(radar: false, radios: true, commsJammer: true, 0.5f));
        Assert.Equal(new AircraftJamShare(0f, 1f, false), JamFieldRule.Share(radar: false, radios: true, commsJammer: false, 0.5f));
    }

    [Fact]
    public void BothShareThePodsUnlessARadioJammerTakesTheRadios()
    {
        Assert.Equal(new AircraftJamShare(0.5f, 0.5f, false), JamFieldRule.Share(radar: true, radios: true, commsJammer: false, 0.5f));
        Assert.Equal(new AircraftJamShare(1f, 0f, true), JamFieldRule.Share(radar: true, radios: true, commsJammer: true, 0.5f));
        Assert.Equal(new AircraftJamShare(0f, 0f, false), JamFieldRule.Share(radar: false, radios: false, commsJammer: true, 0.5f));
    }

    [Theory]
    [InlineData(44, 1, 44)]
    [InlineData(44, 0, 0)]
    [InlineData(18, 0.5, 12.728)]
    public void RadarReachFallsAsTheRootOfItsShare(double full, double share, double expected) =>
        Assert.Equal(expected, JamFieldRule.RadarReach(full, share), 3);
}
