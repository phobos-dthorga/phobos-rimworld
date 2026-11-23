using Phobos.Radio.Physics;
using RimWorld;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>What one line between two antennas crossed: built things and natural rock, in dB before the band's factor.</summary>
public readonly record struct Crossings(float BuiltDb, float NaturalDb)
{
    /// <summary>What it costs on a band: walls and built roofs by the band's factor, rock and natural roofs in full.</summary>
    public double LossDb(BandDef band) => Propagation.ObstructionDb(BuiltDb, NaturalDb, band.obstructionFactor);
}

/// <summary>
/// What the map puts between two antennas: walls and doors on the straight line (counted only where
/// the line is below wall height), natural rock (always), and the roof over each antenna.
/// </summary>
public static class ObstructionService
{
    public static float LossDb(Map map, RadioEndpoint a, RadioEndpoint b, BandDef band, RadioRulesDef rules) =>
        (float)Walk(map, a.Cell, a.AntennaHeightMetres, b.Cell, b.AntennaHeightMetres, rules).LossDb(band);

    /// <summary>
    /// The one walk every contact and every coverage cell uses (Radio 0.2.0): the roofs over both ends, then each cell of the game's
    /// straight line from <paramref name="a"/> to <paramref name="b"/>. Walls are counted where the line is below wall height; rock
    /// always. A door counts while closed, or always with <paramref name="doorsClosed"/> (the overlay's choice, so it does not
    /// redraw each time a door swings). With <paramref name="stopAboveDb"/>, the walk stops once the loss on its most forgiving band
    /// (<paramref name="leastBandFactor"/>) passes it: the caller only needs to know the link is lost.
    /// </summary>
    public static Crossings Walk(Map map, IntVec3 a, float heightA, IntVec3 b, float heightB, RadioRulesDef rules,
        bool doorsClosed = false, float stopAboveDb = float.PositiveInfinity, float leastBandFactor = 1f)
    {
        float built = 0f, natural = 0f;
        AddRoof(map, a, rules, ref built, ref natural);
        AddRoof(map, b, rules, ref built, ref natural);
        // The game's own line, kept in one shared list it reuses: no allocation.
        var cells = GenSight.BresenhamCellsBetween(a, b);
        var last = cells.Count - 1;
        for (var i = 1; i < last; i++)
        {
            var cell = cells[i];
            if (!cell.InBounds(map)) continue;
            var edifice = cell.GetEdifice(map);
            if (edifice == null) continue;
            if (!doorsClosed && edifice is Building_Door door && door.Open) continue;
            if (IsRock(edifice))
            {
                natural += edifice.def.GetModExtension<RadioObstructionExtension>()?.lossDb ?? rules.rockLossDb;
            }
            else
            {
                var fraction = (float)i / last;
                if (!Propagation.RayBlockedByWall(fraction, heightA, heightB, rules.wallHeightMetres)) continue;
                built += CrossingLossDb(edifice, rules);
            }
            if (built * leastBandFactor + natural > stopAboveDb) break;
        }
        return new Crossings(built, natural);
    }

    private static void AddRoof(Map map, IntVec3 cell, RadioRulesDef rules, ref float built, ref float natural)
    {
        var roof = cell.InBounds(map) ? cell.GetRoof(map) : null;
        if (roof == null) return;
        if (roof.isNatural || roof.isThickRoof) natural += RoofLossDb(map, cell, rules);
        else built += RoofLossDb(map, cell, rules);
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
