using System.Collections.Generic;
using Phobos.Radio.Physics;
using RimWorld;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// The colony's own electrical noise. A snapshot is taken once per recompute by scanning the colonist
/// buildings whose Defs carry <see cref="RadioNoiseExtension"/>; endpoints then sum the few sources in
/// range. Nothing here walks cells, and asking about a cell allocates nothing (the coverage overlay asks for every cell it paints).
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
    private float fadeStartFraction = 1f;

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

    /// <summary>
    /// Changes whenever the snapshot holds different sources (one switched on or off, built, moved or gone), so a picture drawn from
    /// an earlier snapshot knows to redraw. Equal snapshots give equal signatures.
    /// </summary>
    public int Signature { get; private set; }

    public void Snapshot(Map map, RadioRulesDef rules)
    {
        sources.Clear();
        fadeStartFraction = rules.noiseFadeStartFraction;
        var signature = 17;
        if (NoisyDefs.Count > 0)
        {
            foreach (var building in map.listerBuildings.allBuildingsColonist)
            {
                if (!NoisyDefs.TryGetValue(building.def, out var extension) || !IsActive(building, extension)) continue;
                var radius = extension.radiusCells > 0 ? extension.radiusCells : rules.noiseRadiusDefaultCells;
                sources.Add(new Source(building.Position, extension.noiseDbmAt1m, radius));
                signature = unchecked(signature * 31 + Gen.HashCombineInt(building.Position.GetHashCode(), radius)
                                      + extension.noiseDbmAt1m.GetHashCode());
            }
        }
        Signature = unchecked(signature * 31 + sources.Count);
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
                var trader = building.TryGetComp<CompPowerTrader>();
                if (trader == null || !trader.PowerOn) return false;
                // A generator out of fuel stays "on" and simply produces nothing.
                return trader is not CompPowerPlant plant || plant.PowerOutput > 0f;
        }
    }

    /// <summary>
    /// Local noise at a cell in dBm, or −∞ when no source reaches it: each source by the inverse-square law, faded to nothing towards
    /// its radius (<see cref="NoiseFloor.FadeDb"/>), power-summed.
    /// </summary>
    public double LocalNoiseDbm(IntVec3 cell, float metresPerCell)
    {
        double milliwatts = 0;
        foreach (var source in sources)
        {
            var distanceCells = source.Cell.DistanceTo(cell);
            var fade = NoiseFloor.FadeDb(distanceCells, source.RadiusCells, fadeStartFraction);
            if (double.IsNegativeInfinity(fade)) continue;
            milliwatts += Decibels.ToMilliwatts(NoiseFloor.AtDistanceDbm(source.LevelDbmAt1m, distanceCells * metresPerCell) + fade);
        }
        return Decibels.MilliwattsToDbm(milliwatts);
    }
}
