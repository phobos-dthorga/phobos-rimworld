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
