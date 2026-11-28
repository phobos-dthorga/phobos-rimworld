using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// One jammer on a map, as the radio hears it (Radio 0.45.0; docs/development/electronic-warfare.md, EW1): where it is, the power it
/// sends out, and the bands it spreads that power over. Any faction's, ours included: noise is noise (the owner's card of 27 Nov 2026).
/// </summary>
public readonly struct RadioJammer
{
    public readonly IntVec3 Cell;
    /// <summary>The power it sends out, in watts, from an omnidirectional antenna.</summary>
    public readonly float RadiatedWatts;
    /// <summary>The bands it covers; null for every band the radio knows.</summary>
    public readonly IReadOnlyList<BandDef>? Bands;
    /// <summary>What it is (a vehicle, a building), for notes and the link sheet; may be null.</summary>
    public readonly Thing? Source;

    public RadioJammer(IntVec3 cell, float radiatedWatts, IReadOnlyList<BandDef>? bands = null, Thing? source = null)
    {
        Cell = cell;
        RadiatedWatts = radiatedWatts;
        Bands = bands;
        Source = source;
    }
}

/// <summary>
/// A jammer in flight over a world tile (Radio 0.75.0; docs/development/electronic-warfare.md, EW3): an aircraft with a radio jamming pod
/// switched on, holding station or passing. Every end of a link between tiles on that tile hears it, at the aircraft's height above it.
/// </summary>
public readonly struct WorldRadioJammer
{
    public readonly RimWorld.Planet.PlanetTile Tile;
    public readonly float RadiatedWatts;
    /// <summary>The bands it covers; null for every band.</summary>
    public readonly IReadOnlyList<BandDef>? Bands;
    /// <summary>How high it flies above the tile's plain, in metres: the distance to an end beneath it.</summary>
    public readonly double HeightMetres;
    public readonly Thing? Source;

    public WorldRadioJammer(RimWorld.Planet.PlanetTile tile, float radiatedWatts, IReadOnlyList<BandDef>? bands, double heightMetres, Thing? source)
    {
        Tile = tile;
        RadiatedWatts = radiatedWatts;
        Bands = bands;
        HeightMetres = heightMetres;
        Source = source;
    }
}

/// <summary>
/// Where jammers come from: the seam through which anything that jams radios joins the radio's noise (Radio 0.45.0; EW1, "the join").
/// The radio's core names no vehicle and no other mod; a tie-in registers a source once at start-up, which lists the jammers on a map when
/// the net takes its noise snapshot (about once a second). The radio's Vehicle Framework tie-in feeds D'thorga Framework's jam fields
/// through it. A source that throws is left out until the next start, as D'thorga Framework's jam fields do.
/// </summary>
public static class RadioJamming
{
    private static readonly List<Func<Map, IEnumerable<RadioJammer>>> sources = new();

    /// <summary>Registers a source of jammers: called with a map, it lists the jammers there now. Call once, at start-up.</summary>
    public static void Register(Func<Map, IEnumerable<RadioJammer>> source)
    {
        if (source != null && !sources.Contains(source)) sources.Add(source);
    }

    /// <summary>Whether anything could jam: no source registered costs the snapshot nothing.</summary>
    public static bool Any => sources.Count > 0;

    /// <summary>
    /// The radio's bands a jammer covering these frequencies jams (Radio 0.75.0; docs/development/electronic-warfare.md, EW4 and EW10): each band
    /// whose centre lies in one of the ranges, in MHz (a simplification, labelled: a range that clips a band's edge jams all of it or none).
    /// Null ranges, null: every band. Kept per list of ranges, as a jammer's ranges come from a rules Def and do not change while the game runs.
    /// </summary>
    public static IReadOnlyList<BandDef>? BandsIn(IReadOnlyList<FloatRange>? rangesMHz)
    {
        if (rangesMHz == null) return null;
        if (bandsByRanges.TryGetValue(rangesMHz, out var known)) return known;
        var bands = new List<BandDef>();
        foreach (var band in DefDatabase<BandDef>.AllDefsListForReading)
        {
            foreach (var range in rangesMHz)
            {
                if (band.centreMHz < range.min || band.centreMHz > range.max) continue;
                bands.Add(band);
                break;
            }
        }
        bandsByRanges[rangesMHz] = bands;
        return bands;
    }

    private static readonly Dictionary<IReadOnlyList<FloatRange>, IReadOnlyList<BandDef>> bandsByRanges = new();

    // ---- Jammers over a world tile (Radio 0.75.0; EW3) ----

    private static readonly List<Func<IEnumerable<WorldRadioJammer>>> worldSources = new();
    private static readonly List<WorldRadioJammer> overhead = new();
    private static int overheadTick = int.MinValue;
    private static Game? overheadGame;

    /// <summary>Registers a source of jammers in flight on the world map (an aircraft with a pod switched on). Call once, at start-up.</summary>
    public static void RegisterWorld(Func<IEnumerable<WorldRadioJammer>> source)
    {
        if (source != null && !worldSources.Contains(source)) worldSources.Add(source);
    }

    /// <summary>
    /// The jammers over this tile now (EW3), read again from every source at most every <c>worldJammerRefreshTicks</c> of the rules: one pass
    /// over the world's objects, and an empty list in almost every game. Into <paramref name="into"/>; nothing while the setting is off.
    /// </summary>
    internal static void Overhead(RimWorld.Planet.PlanetTile tile, List<WorldRadioJammer> into)
    {
        if (worldSources.Count == 0 || !tile.Valid || !RadioMod.Settings.jammingReachesRadios || Current.Game == null) return;
        var now = Find.TickManager.TicksGame;
        if (overheadGame != Current.Game || now - overheadTick >= RadioRulesDef.Current.worldJammerRefreshTicks || now < overheadTick)
        {
            overheadGame = Current.Game;
            overheadTick = now;
            overhead.Clear();
            for (var i = 0; i < worldSources.Count; i++)
            {
                try
                {
                    foreach (var jammer in worldSources[i]())
                    {
                        if (jammer.RadiatedWatts > 0f && jammer.Tile.Valid) overhead.Add(jammer);
                    }
                }
                catch (Exception e)
                {
                    RadioMod.Logger.Warn("A jammer over the world map could not be read and is left out until the next start: " + e.Message);
                    worldSources.RemoveAt(i);
                    i--;
                }
            }
        }
        foreach (var jammer in overhead)
        {
            if (jammer.Tile == tile) into.Add(jammer);
        }
    }

    /// <summary>Every jammer on the map now, from every source, into <paramref name="into"/>.</summary>
    internal static void Collect(Map map, List<RadioJammer> into)
    {
        for (var i = 0; i < sources.Count; i++)
        {
            try
            {
                foreach (var jammer in sources[i](map))
                {
                    if (jammer.RadiatedWatts > 0f) into.Add(jammer);
                }
            }
            catch (Exception e)
            {
                RadioMod.Logger.Warn("A jammer's source could not be read and is left out until the next start: " + e.Message);
                sources.RemoveAt(i);
                i--;
            }
        }
    }
}

/// <summary>
/// Calls sent off the map by another mod's building (Radio 0.46.0; docs/development/electronic-warfare.md, EW18): whether jamming at the
/// building drowns the far end's reply, so the call cannot go out. Reads the net's last noise snapshot: one lookup, no walk. Any faction's
/// building, ours included (noise is noise). With the setting "Jamming reaches radios" off, nothing is jammed.
/// </summary>
public static class JammedCalls
{
    /// <summary>Whether a call from this cell is jammed now, and the jamming at its receiver, in dBm (−∞ when none).</summary>
    public static bool Jammed(Map map, IntVec3 cell, out double jammingDbm)
    {
        jammingDbm = double.NegativeInfinity;
        if (!RadioMod.Settings.jammingReachesRadios || !RadioJamming.Any) return false;
        var rules = RadioRulesDef.Current;
        var band = rules.callOutBand ?? DefDatabase<BandDef>.AllDefsListForReading.FirstOrDefault();
        var mode = rules.callOutMode ?? DefDatabase<ModeDef>.AllDefsListForReading.FirstOrDefault();
        if (band == null || mode == null) return false;
        var noise = RadioNet.For(map).Noise;
        if (noise.JammerCount == 0) return false;
        var metresPerCell = RadioMod.Settings.metresPerCell;
        jammingDbm = noise.JammingDbm(cell, metresPerCell, mode.bandwidthHz, band);
        var floor = Physics.NoiseFloor.TotalDbm(mode.bandwidthHz, rules.callOutNoiseFigureDb, noise.LocalNoiseDbm(cell, metresPerCell));
        return Physics.Jamming.Drowns(rules.callOutReplyDbm, floor, jammingDbm, mode.requiredSnrDb);
    }
}
