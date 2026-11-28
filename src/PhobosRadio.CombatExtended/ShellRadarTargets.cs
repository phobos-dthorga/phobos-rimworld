using System.Collections.Generic;
using CombatExtended;
using Phobos.Radio.Services;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.Radio.CombatExtended;

/// <summary>
/// Combat Extended's shells in flight between world tiles as radar targets (Radio 0.74.0; docs/development/radar-and-air-defence.md, row RD8;
/// the owner's card of 28 Nov 2026, "CE shells in flight"): each <c>TravelingShell</c> (read 28 Nov 2026), whoever fired it, at an authored
/// height and size; one aimed at a player's map is inbound, and the first of our radars to see it warns by letter.
/// <para>Cost: one pass over the world's objects at most once a tick, only while a scope or the warnings' background look asks.</para>
/// </summary>
[StaticConstructorOnStartup]
public static class ShellRadarTargets
{
    /// <summary>A shell's radar size, in m² [authored]: bigger than a mortar bomb's 0.01 m² (the record's cited figure), smaller than a missile.</summary>
    public const double ShellSquareMetres = 0.05;
    /// <summary>The height a shell is taken to arc at, in metres [authored]: a long-range shell's arc tops out kilometres up.</summary>
    public const double ShellMetres = 10000.0;

    static ShellRadarTargets() => RadarTargets.Register(new Source());

    private sealed class Source : IRadarTargetSource
    {
        public void Collect(List<RadarTarget> into)
        {
            foreach (WorldObject place in Find.WorldObjects.AllWorldObjects)
            {
                if (place is not TravelingShell { Destroyed: false } shell) continue;
                PlanetTile tile = RadarTargets.TileUnder(shell, ((WorldObject)shell).DrawPos, shell.Tile);
                if (!tile.Valid) continue;
                Faction? faction = shell.launcher?.Faction;
                bool inbound = faction != Faction.OfPlayer && shell.globalTarget.Tile.Valid
                    && Find.WorldObjects.MapParentAt(shell.globalTarget.Tile) is { HasMap: true } target && target.Map.IsPlayerHome;
                into.Add(new RadarTarget(shell, tile, ShellMetres, ShellSquareMetres, faction, shell.Label.CapitalizeFirst(), RadarContactKind.Shell,
                    false, inbound));
            }
        }
    }
}
