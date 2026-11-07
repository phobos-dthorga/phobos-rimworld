using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Heat;
using Phobos.BurnsAndSmoke.Rules;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.BurnsAndSmoke.Services;

/// <summary>
/// Whether a pawn should get out of a fire's hot zone, and where to. Only our zones start it (a room the game has heated through is
/// the game's business), but a cooler cell must be cooler by the full temperature, the game's and ours. The threshold is the game's
/// own: air 150 degrees over the top of the pawn's comfortable range burns them, so clothing that keeps them cool counts too.
/// </summary>
public static class HotCellService
{
    /// <summary>Cells found cool but not reachable before the search gives up, so a walled-in pawn costs little (agent choice).</summary>
    const int MaxReachChecks = 10;

    public static bool TooHotFor(Pawn pawn, IntVec3 cell, Map map) =>
        FireHeatRule.BurnsAt(cell.GetTemperature(map), pawn.ComfortableTemperatureRange().max);

    /// <summary>
    /// True when the pawn stands in a hot zone hot enough to burn them and is not choosing to be there: firefighters beating a fire
    /// out stay at it, as does anyone the player ordered there (agent choice). The empty case is one dictionary count.
    /// </summary>
    public static bool ShouldMoveAway(Pawn pawn)
    {
        if (!BurnsAndSmokeMod.Settings.fleeHotCells || !pawn.Spawned || !pawn.RaceProps.IsFlesh) return false;
        Map map = pawn.Map;
        if (!MapComponent_PhobosFireHeat.TryGetRise(map, pawn.Position, out _)) return false;
        JobDef? current = pawn.CurJobDef;
        if (current == JobDefOf.BeatFire || current == JobDefOf.ExtinguishSelf || current == BurnsDefOf.PhobosBurns_MoveAwayFromHeat) return false;
        if (PawnUtility.PlayerForcedJobNowOrSoon(pawn)) return false;
        return TooHotFor(pawn, pawn.Position, map);
    }

    /// <summary>The nearest cell the pawn can stand in and reach whose air would not burn them, within the rules Def's radius.</summary>
    public static bool TryFindCoolCell(Pawn pawn, out IntVec3 found)
    {
        found = IntVec3.Invalid;
        Map map = pawn.Map;
        int cells = GenRadial.NumCellsInRadius(PhobosBurnRulesDef.Current.fireHeatFleeSearchRadiusCells);
        int unreachable = 0;
        for (int i = 1; i < cells; i++)
        {
            IntVec3 cell = pawn.Position + GenRadial.RadialPattern[i];
            if (!cell.InBounds(map) || !cell.Standable(map) || cell.ContainsStaticFire(map) || TooHotFor(pawn, cell, map)) continue;
            if (!pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
            {
                if (++unreachable >= MaxReachChecks) return false;
                continue;
            }
            found = cell;
            return true;
        }
        return false;
    }
}
