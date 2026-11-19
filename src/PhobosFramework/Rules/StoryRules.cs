using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// How far apart places on the world map are in real terms, game-free (Framework 0.181.0). The owner decided on 18 Nov 2026 that a world
/// tile is about 30 km across (docs/development/world-elevation-and-descents.md, WE2): where our vehicles' own speed (29.4 km a tile) meets
/// an Earth-sized planet (31.5 km). Nothing read it in code until the logbook's trips and the stranded convoy's way home.
/// </summary>
public static class WorldScaleRule
{
    /// <summary>The owner's figure for a world tile (18 Nov 2026).</summary>
    public const double KilometresPerTile = 30;

    /// <summary>Tiles as kilometres; never negative.</summary>
    public static double Kilometres(double tiles) => Math.Max(0, tiles) * KilometresPerTile;
}

/// <summary>
/// What a lasting logbook keeps (Framework 0.181.0; docs/development/vehicle-emergent-play.md, EM23): every entry until it is full, then
/// the oldest go first. Unlike the graphs' marks (EventMarkRule) nothing ages out: a vehicle's life is the point.
/// </summary>
public static class LogbookRule
{
    /// <summary>The agent's figure (20 Nov 2026), for the owner to revise: a busy year of trips, repairs and faults, small in a save.</summary>
    public const int DefaultCap = 200;

    /// <summary>How many of the oldest entries to drop so no more than the cap remain.</summary>
    public static int DropCount(int count, int cap) => cap < 0 ? 0 : Math.Max(0, count - cap);
}

/// <summary>
/// Whether a journey is worth a letter (Framework 0.181.0, EM25): the trip report always goes in the logbook, and becomes a letter only
/// when something went wrong on the way, so a quiet run says nothing (the agent's default, agreed by the owner on 20 Nov 2026).
/// </summary>
public static class TripRule
{
    public static bool WentWrong(int newFaults, bool halted, bool someoneHurt) => newFaults > 0 || halted || someoneHurt;
}
