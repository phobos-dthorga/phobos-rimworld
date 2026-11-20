using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Avionics A3, A4, A10 and A11 (aircraft service-life upgrades, 21 Nov 2026).</summary>
public class AvionicsEffectsTests
{
    const double Basic = 0.8, None = 0.667, Blind = 0.7, RadarBack = 0.5;

    static double Aim(SightState sight, bool dark = false, bool weather = false, bool night = false, bool radar = false) =>
        AvionicsAimRule.Factor(sight, dark, weather, night, radar, Basic, None, Blind, RadarBack);

    [Fact]
    public void AFullSightByDayAimsAsTheWeaponWasMade() => Assert.Equal(1, Aim(SightState.Full), 9);

    [Fact]
    public void ABasicSightAimsWorseAndNoSightWorseStill()
    {
        Assert.Equal(Basic, Aim(SightState.Basic), 9);
        Assert.Equal(None, Aim(SightState.Out), 9);
    }

    [Fact]
    public void ANightSightGivesTheNightBack()
    {
        Assert.Equal(Blind, Aim(SightState.Full, dark: true), 9);
        Assert.Equal(1, Aim(SightState.Full, dark: true, night: true), 9);
    }

    [Fact]
    public void ARadarGivesHalfTheWeatherBack()
    {
        Assert.Equal(Blind, Aim(SightState.Full, weather: true), 9);
        Assert.Equal(0.85, Aim(SightState.Full, weather: true, radar: true), 9);
        // A night sight does nothing for the weather.
        Assert.Equal(Blind, Aim(SightState.Full, weather: true, night: true), 9);
    }

    [Fact]
    public void DarkAndWeatherMultiply() => Assert.Equal(Blind * Blind, Aim(SightState.Full, dark: true, weather: true), 9);

    [Fact]
    public void ScatterWidensAsAimFalls()
    {
        Assert.Equal(3, AvionicsAimRule.MissRadius(3, 1), 9);
        Assert.Equal(4.5, AvionicsAimRule.MissRadius(3, 2.0 / 3), 9);
    }

    [Fact]
    public void ALandingSeenAndControlledIsNeverRough()
    {
        Assert.Equal(0, RoughLandingRule.Chance(false, true, 1, false, 0.4, 0.5, 0.5));
        // Poor visibility with the instruments working is fine.
        Assert.Equal(0, RoughLandingRule.Chance(true, true, 1, false, 0.4, 0.5, 0.5));
    }

    [Fact]
    public void BlindOrLooseControlsRiskARoughLandingWhichAnAutopilotHalves()
    {
        Assert.Equal(0.4, RoughLandingRule.Chance(true, false, 1, false, 0.4, 0.5, 0.5), 9);
        Assert.Equal(0.4, RoughLandingRule.Chance(false, true, 0.3, false, 0.4, 0.5, 0.5), 9);
        Assert.Equal(0.2, RoughLandingRule.Chance(false, true, 0.3, true, 0.4, 0.5, 0.5), 9);
    }
}
