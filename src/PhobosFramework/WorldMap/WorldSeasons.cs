using System.Collections.Generic;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.Framework.WorldMap;

/// <summary>
/// The season ahead at a place on the world map, in the player's words (Framework 0.188.0; docs/development/vehicle-emergent-play.md, EM16).
/// Reads the game's own winter figure for a tile on a future date, so it agrees with what the game will do to movement there. Each answer
/// is kept for the rest of the game day, since inspect panes ask every frame.
/// </summary>
public static class WorldSeasons
{
    static readonly Dictionary<int, (int day, string? line)> cache = new();

    /// <summary>The game's winter figure at a tile, so many days from now: 0 in the warm months to 2 in deep winter.</summary>
    public static float WinterOnDay(PlanetTile tile, int daysAhead) =>
        WorldPathGrid.GetCurrentWinterMovementDifficultyOffset(tile, GenTicks.TicksAbs + daysAhead * GenDate.TicksPerDay);

    /// <summary>"Snow reaches the ground near Toxo in about 12 days." or "The snow near Toxo lifts in about 5 days."; null when nothing
    /// changes within the forecast's horizon, or the tile is not on a planet's surface.</summary>
    public static string? ForecastLine(PlanetTile tile)
    {
        if (!tile.Valid || Find.WorldGrid == null || tile.LayerDef == null || tile.LayerDef.isSpace) return null;
        int today = GenDate.DaysPassed;
        if (cache.TryGetValue(tile.tileId, out (int day, string? line) known) && known.day == today) return known.line;
        if (cache.Count > 64) cache.Clear();
        int? days = WinterForecastRule.DaysUntilChange(day => WinterOnDay(tile, day), out bool snowNow);
        string? line = days is int d
            ? (snowNow ? "PhobosSeason_SnowLifts" : "PhobosSeason_SnowComes").Translate(WorldPlaces.Describe(tile).Named("PLACE"), d.Named("DAYS")).Resolve()
            : null;
        cache[tile.tileId] = (today, line);
        return line;
    }
}
