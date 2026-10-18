using System.Collections.Generic;
using Phobos.Radio.Physics;
using RimWorld;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// The colony's own electrical noise. A snapshot is taken once per recompute by scanning the colonist
/// buildings whose Defs carry <see cref="RadioNoiseExtension"/>; endpoints then sum the few sources in
/// range. Nothing here walks cells.
/// </summary>
public sealed class NoiseService
{
    private readonly struct Source
    {
        public readonly IntVec3 Cell;
        public readonly float LevelDbmAt1m;
        public readonly int RadiusCells;

        public Source(IntVec3 cell, float levelDbmAt1m, int radiusCells)
        {
            Cell = cell;
            LevelDbmAt1m = levelDbmAt1m;
            RadiusCells = radiusCells;
        }
    }

    private static Dictionary<ThingDef, RadioNoiseExtension>? noisyDefs;
    private readonly List<Source> sources = new();
    private readonly List<double> levels = new();

    private static Dictionary<ThingDef, RadioNoiseExtension> NoisyDefs
    {
        get
        {
            if (noisyDefs == null)
            {
                noisyDefs = new Dictionary<ThingDef, RadioNoiseExtension>();
                foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    var extension = def.GetModExtension<RadioNoiseExtension>();
                    if (extension != null) noisyDefs[def] = extension;
                }
            }
            return noisyDefs;
        }
    }

    public int SourceCount => sources.Count;

    public void Snapshot(Map map, RadioRulesDef rules)
    {
        sources.Clear();
        if (NoisyDefs.Count == 0) return;
        foreach (var building in map.listerBuildings.allBuildingsColonist)
        {
            if (!NoisyDefs.TryGetValue(building.def, out var extension) || !IsActive(building, extension)) continue;
            var radius = extension.radiusCells > 0 ? extension.radiusCells : rules.noiseRadiusDefaultCells;
            sources.Add(new Source(building.Position, extension.noiseDbmAt1m, radius));
        }
    }

    private static bool IsActive(Building building, RadioNoiseExtension extension)
    {
        switch (extension.noiseWhen)
        {
            case NoiseWhen.Always:
                return true;
            case NoiseWhen.Connected:
                return building.TryGetComp<CompPower>()?.PowerNet != null;
            default:
                return building.TryGetComp<CompPowerTrader>()?.PowerOn == true;
        }
    }

    /// <summary>Local noise at a cell in dBm, or −∞ when no source reaches it.</summary>
    public double LocalNoiseDbm(IntVec3 cell, float metresPerCell)
    {
        levels.Clear();
        foreach (var source in sources)
        {
            var distanceCells = source.Cell.DistanceTo(cell);
            if (distanceCells > source.RadiusCells) continue;
            levels.Add(NoiseFloor.AtDistanceDbm(source.LevelDbmAt1m, distanceCells * metresPerCell));
        }
        return levels.Count == 0 ? double.NegativeInfinity : Decibels.PowerSumDbm(levels);
    }
}
