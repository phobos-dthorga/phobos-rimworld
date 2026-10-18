using Phobos.Radio.Physics;
using RimWorld;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// What the map puts between two antennas: walls and doors on the straight line (counted only where
/// the line is below wall height), natural rock (always), and the roof over each antenna.
/// </summary>
public static class ObstructionService
{
    public static float LossDb(Map map, RadioEndpoint a, RadioEndpoint b, BandDef band, RadioRulesDef rules)
    {
        var total = RoofLossDb(map, a.Cell, rules) + RoofLossDb(map, b.Cell, rules);
        var cells = GenSight.BresenhamCellsBetween(a.Cell, b.Cell);
        var last = cells.Count - 1;
        for (var i = 1; i < last; i++)
        {
            var cell = cells[i];
            if (!cell.InBounds(map)) continue;
            var edifice = cell.GetEdifice(map);
            if (edifice == null) continue;
            if (edifice is Building_Door door && door.Open) continue;
            if (IsRock(edifice))
            {
                total += rules.rockLossDb;
                continue;
            }
            var fraction = (float)i / last;
            if (!Propagation.RayBlockedByWall(fraction, a.AntennaHeightMetres, b.AntennaHeightMetres, rules.wallHeightMetres)) continue;
            total += CrossingLossDb(edifice, rules);
        }
        return total * band.obstructionFactor;
    }

    public static float RoofLossDb(Map map, IntVec3 cell, RadioRulesDef rules)
    {
        var roof = cell.InBounds(map) ? cell.GetRoof(map) : null;
        if (roof == null) return 0f;
        if (roof.isThickRoof) return rules.roofThickLossDb;
        return roof.isNatural ? rules.roofThinNaturalLossDb : rules.roofConstructedLossDb;
    }

    public static bool IsRock(Building edifice)
    {
        var building = edifice.def.building;
        return building != null && (building.isNaturalRock || building.isResourceRock);
    }

    /// <summary>What one crossing of a built thing costs: its own extension, its stuff's, or a fill-scaled default.</summary>
    public static float CrossingLossDb(Building edifice, RadioRulesDef rules)
    {
        var own = edifice.def.GetModExtension<RadioObstructionExtension>();
        if (own != null) return own.lossDb;
        var stuff = edifice.Stuff?.GetModExtension<RadioObstructionExtension>();
        if (stuff != null) return stuff.lossDb;
        var fill = edifice.def.fillPercent;
        var isWall = edifice.def.building?.isWall == true;
        return isWall || fill >= 1f ? rules.defaultStuffLossDb : rules.defaultStuffLossDb * fill;
    }
}
