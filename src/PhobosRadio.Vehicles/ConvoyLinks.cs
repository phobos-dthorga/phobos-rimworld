using System.Collections.Generic;
using Phobos.Radio.Services;
using Phobos.VehicleSystems;
using RimWorld.Planet;
using Verse;

namespace Phobos.Radio.Vehicles;

/// <summary>
/// A caravan's way home as its reach check last found it, hop by hop, for the link sheet between tiles (Radio 0.48.0): the nodes of each
/// hop, and the mode it is worked in where that is not the caller's own (Morse). In reach it is the chain; out of reach, the one hop
/// straight to the nearest colony, so the sheet can say why. The sheet's paths are built from it only when it changes.
/// <para>Cost: nothing unless a link sheet is open; then one look a frame at a kept list, and a rebuild (a link and two walks a hop, three
/// hops at most) each time the reach check has run again, at most once a tenth of a game hour.</para>
/// </summary>
public static class ConvoyLinks
{
    static readonly Dictionary<Caravan, List<(RelayNode A, RelayNode B, ModeDef? Mode)>> kept = new();
    static readonly List<WorldLinkPath> built = new();
    static object? builtFrom;

    /// <summary>The hops a caravan's reach check found. Called by that check only.</summary>
    internal static void Keep(Caravan convoy, List<(RelayNode A, RelayNode B, ModeDef? Mode)> hops)
    {
        if (kept.Count > 64) kept.Clear();
        kept[convoy] = hops;
    }

    /// <summary>The caravan's hops home laid out for the sheet, as fresh as its reach check keeps them.</summary>
    public static IReadOnlyList<WorldLinkPath> Paths(Caravan convoy)
    {
        // Asks the caravan's reach as its line does (kept, so no new search most frames).
        _ = ConvoyRadio.Of(convoy);
        if (!kept.TryGetValue(convoy, out var hops))
        {
            built.Clear();
            builtFrom = null;
            return built;
        }
        if (ReferenceEquals(hops, builtFrom)) return built;
        built.Clear();
        builtFrom = hops;
        RadioRulesDef rules = RadioRulesDef.Current;
        foreach (var (a, b, mode) in hops) built.Add(WorldLinkPath.For(a, b, rules, mode));
        return built;
    }
}
