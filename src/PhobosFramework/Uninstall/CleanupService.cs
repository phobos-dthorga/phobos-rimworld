using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Framework.Uninstall;

/// <summary>
/// Lets a player remove a Phobos mod cleanly: strip every condition the mod's Defs put on pawns, then save, then switch the mod
/// off. Without this the game logs one line per missing Def on the next load and carries on; with it, nothing is left to miss.
/// Every mod offers it as a settings button and a dev action that name the mod.
/// </summary>
public static class CleanupService
{
    /// <summary>True while a sweep runs, so conditions that leave another behind when removed (HediffComp_PhobosLeavesLasting) do not.</summary>
    public static bool Sweeping { get; private set; }

    /// <summary>Removes every hediff whose Def belongs to <paramref name="pack"/> from every pawn, alive or dead, anywhere.</summary>
    public static int RemoveHediffsOf(ModContentPack pack)
    {
        Sweeping = true;
        try
        {
            return RemoveAll(pack);
        }
        finally
        {
            Sweeping = false;
        }
    }

    static int RemoveAll(ModContentPack pack)
    {
        int removed = 0;
        var seen = new HashSet<Pawn>();
        foreach (Pawn pawn in PawnsFinder.All_AliveOrDead)
        {
            if (seen.Add(pawn)) removed += RemoveFrom(pawn, pack);
        }
        if (Find.WorldPawns != null)
        {
            foreach (Pawn pawn in Find.WorldPawns.AllPawnsAliveOrDead)
            {
                if (seen.Add(pawn)) removed += RemoveFrom(pawn, pack);
            }
        }
        return removed;
    }

    static int RemoveFrom(Pawn pawn, ModContentPack pack)
    {
        Pawn_HealthTracker? health = pawn.health;
        List<Hediff>? hediffs = health?.hediffSet?.hediffs;
        if (health == null || hediffs == null) return 0;
        var ours = new List<Hediff>();
        foreach (Hediff hediff in hediffs)
        {
            if (hediff.def.modContentPack == pack) ours.Add(hediff);
        }
        foreach (Hediff hediff in ours)
        {
            health.RemoveHediff(hediff);
        }
        return ours.Count;
    }
}
