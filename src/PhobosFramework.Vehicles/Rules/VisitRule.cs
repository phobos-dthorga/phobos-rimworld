using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// People use parked vehicles (Framework 0.289.0; docs/development/vehicle-parked-use.md): the arithmetic of a visit, game-free and tested.
/// Every figure passed in is the rules Def's, authored unless the record names a source.
/// </summary>
public static class VisitRule
{
    /// <summary>Whether a visit has run its longest: never when <paramref name="maxHours"/> is 0 or less (a warm-up lasts while it helps).</summary>
    public static bool OverTime(int sinceTick, int nowTick, double maxHours, int ticksPerHour) =>
        maxHours > 0 && nowTick - sinceTick >= maxHours * ticksPerHour;

    /// <summary>
    /// The recreation an hour the stereo gives: a visitor who came to sit and listen gets the game's own rate for a recreation job (0.36 an
    /// hour, read from the game's code); a rider passing the time on a drive gets the life-aboard figure.
    /// </summary>
    public static double StereoJoyPerHour(bool cameToListen, double visitorRate, double riderRate) => cameToListen ? visitorRate : riderRate;

    /// <summary>
    /// The visitor's radio goes off (and they climb out) once the starting battery is at or below the line a start needs, unless the player's
    /// setting lets visitors flatten it. A line that is not known (NaN) never stops it.
    /// </summary>
    public static bool RadioOffForTheStart(double reading, double line, bool mayFlatten) => !mayFlatten && !double.IsNaN(line) && reading <= line;

    /// <summary>The weight of a hotbox session for someone whose friend already sits in one: the base chance times the friends' factor.</summary>
    public static double HotboxChance(double baseChance, bool friendAboard, double friendFactor) =>
        friendAboard ? baseChance * Math.Max(1, friendFactor) : baseChance;

    /// <summary>Whether a hotbox visitor has stayed long enough after their smoke to share it.</summary>
    public static bool SmokedAndShared(int smokedAt, int nowTick, double stayHours, int ticksPerHour) =>
        smokedAt >= 0 && nowTick - smokedAt >= stayHours * ticksPerHour;

    /// <summary>Whether a nap was long enough to remember.</summary>
    public static bool NapRemembered(int sinceTick, int nowTick, double hours, int ticksPerHour) => nowTick - sinceTick >= hours * ticksPerHour;

    /// <summary>Whether a temperature lies in a comfortable range, its ends included.</summary>
    public static bool Comfortable(double temperature, double min, double max) => temperature >= min && temperature <= max;
}
