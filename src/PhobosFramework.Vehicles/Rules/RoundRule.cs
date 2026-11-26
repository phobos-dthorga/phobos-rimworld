using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// An aircraft's round between places (Framework 0.307.0; docs/development/autonomous-vehicles.md, step 9; the owner's cards of 26 Nov
/// 2026): stops picked on the world map, a wait set per stop, a leg flown only with the fuel for it and some to spare. Game-free and tested.
/// </summary>
public static class RoundRule
{
    /// <summary>One game hour, in ticks.</summary>
    public const int TicksPerHour = 2500;

    /// <summary>The wait at a new stop, hours: an hour, as a loop's (authored).</summary>
    public const float DefaultWaitHours = 1f;

    /// <summary>The waits offered for a stop, hours, as preset buttons (the owner's card: set per stop; the choices authored).</summary>
    public static readonly IReadOnlyList<float> WaitChoices = new[] { 0f, 0.5f, 1f, 2f, 4f, 8f, 12f, 24f };

    /// <summary>The fuel a leg must have aboard against what Vehicle Framework says it burns: exactly that (Framework 0.310.0, the owner's
    /// answer on the Decisions page, 26 Nov 2026, over 0.307.0's sixth more).</summary>
    public const double FuelMargin = 1.0;

    /// <summary>The fewest stops a round has: there and back.</summary>
    public const int FewestStops = 2;

    /// <summary>The most stops a round has (authored: enough for any supply run, few enough to read in a menu).</summary>
    public const int MostStops = 8;

    public static int WaitTicks(float hours) => (int)Math.Round(Math.Max(0f, hours) * TicksPerHour);

    /// <summary>Whether there is fuel aboard for the next leg with its margin; an aircraft that takes no fuel always has.</summary>
    public static bool HasFuelFor(double aboard, double needed, double margin = FuelMargin) => needed <= 0 || aboard >= needed * margin;

    /// <summary>Whether its wait at a stop is over.</summary>
    public static bool WaitOver(int now, int waitUntil) => waitUntil >= 0 && now >= waitUntil;

    /// <summary>Whether another stop may be added: under the most, and not the same place as the stop before it.</summary>
    public static bool MayAdd(int stopsSoFar, bool sameAsLast) => stopsSoFar < MostStops && !sameAsLast;

    /// <summary>Whether a round may be flown: at least two stops, and its last not the same place as its first (that would be one stop).</summary>
    public static bool Flyable(int stops, bool lastIsFirst) => stops >= FewestStops && !(lastIsFirst && stops == FewestStops);
}
