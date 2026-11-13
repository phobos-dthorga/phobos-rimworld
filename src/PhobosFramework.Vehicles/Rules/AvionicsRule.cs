namespace Phobos.Framework.Vehicles.Rules;

/// <summary>How much of a capability an aircraft carries: none, a basic fit, or a full one (docs/development/vehicle-avionics.md, the fit).</summary>
public enum AvionicsLevel
{
    None,
    Basic,
    Full,
}

/// <summary>
/// What an aircraft's avionics carry, game-free (Framework 0.110.0; avionics, the fit). Nine capabilities, as in the record; this batch gives
/// attitude and navigation their effects (A1, A2), and every capability its draw. The amps are authored, at 24 V; their direction is real (a
/// radar is the heavy load). Essential capabilities stay on the battery when the generator is lost (A6); the rest are shed with the other
/// loads. A full fit of navigation or a sight is a basic one plus more, so its basic part stays essential.
/// </summary>
public sealed class AvionicsFitValues
{
    public bool Attitude = true;
    public AvionicsLevel Navigation = AvionicsLevel.Basic;
    public bool Autopilot;
    public bool Radar;
    public AvionicsLevel Sight;
    public bool NightSight;
    public bool Warning;
    public bool Countermeasures;
    public bool Oxygen;
}

public static class AvionicsRule
{
    public const double AttitudeAmps = 2;
    public const double NavigationBasicAmps = 2;
    public const double NavigationFullAmps = 6;
    public const double AutopilotAmps = 4;
    public const double RadarAmps = 20;
    public const double SightBasicAmps = 1;
    public const double SightFullAmps = 5;
    public const double NightSightAmps = 4;
    public const double WarningAmps = 2;
    public const double CountermeasuresAmps = 1;
    public const double OxygenAmps = 2;

    /// <summary>What the capabilities kept on the battery draw.</summary>
    public static double EssentialAmps(AvionicsFitValues fit) =>
        (fit.Attitude ? AttitudeAmps : 0)
        + (fit.Navigation != AvionicsLevel.None ? NavigationBasicAmps : 0)
        + (fit.Sight != AvionicsLevel.None ? SightBasicAmps : 0)
        + (fit.Warning ? WarningAmps : 0)
        + (fit.Countermeasures ? CountermeasuresAmps : 0)
        + (fit.Oxygen ? OxygenAmps : 0);

    /// <summary>What the capabilities shed with the other loads draw: the full fits' extra, the autopilot, the radar and the night sight.</summary>
    public static double OtherAmps(AvionicsFitValues fit) =>
        (fit.Navigation == AvionicsLevel.Full ? NavigationFullAmps - NavigationBasicAmps : 0)
        + (fit.Sight == AvionicsLevel.Full ? SightFullAmps - SightBasicAmps : 0)
        + (fit.Autopilot ? AutopilotAmps : 0)
        + (fit.Radar ? RadarAmps : 0)
        + (fit.NightSight ? NightSightAmps : 0);

    /// <summary>The avionics system's parameters for this fit: 1 for carried, 0 for not.</summary>
    public static (string name, double value)[] Parameters(AvionicsFitValues fit) => new[]
    {
        ("attitude", fit.Attitude ? 1.0 : 0.0),
        ("navigation", fit.Navigation != AvionicsLevel.None ? 1.0 : 0.0),
        ("navigationFull", fit.Navigation == AvionicsLevel.Full ? 1.0 : 0.0),
    };
}

/// <summary>
/// A turbine rating's time limit (Framework 0.110.0; avionics L1): a rating above maximum continuous may be held only so long ("take-off
/// power for five minutes"). Past it the hot section's life is used faster. Game-free; the factor is the rules Def's.
/// </summary>
public static class RatingLimitRule
{
    /// <summary>Whether a rating held this many ticks is past its limit in game minutes; a rating with no limit never is.</summary>
    public static bool Over(double ticksHeld, double limitMinutes, double ticksPerMinute) =>
        limitMinutes > 0 && ticksPerMinute > 0 && ticksHeld > limitMinutes * ticksPerMinute;

    /// <summary>The factor on the hot section's wear: the rules' factor past the limit, 1 within it.</summary>
    public static double LifeFactor(bool over, double factorPastLimit) => over ? (factorPastLimit < 1 ? 1 : factorPastLimit) : 1;
}
