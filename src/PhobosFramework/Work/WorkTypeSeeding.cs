using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.Framework.Work;

/// <summary>
/// Put on a WorkTypeDef (Framework 0.354.0; docs/development/rearm-and-refuel-work.md): the first time a save runs with the work type, every
/// pawn of the player's already there starts it at the priority it has for the first of these work types it is able to do
/// (<see cref="WorkTypeSeedRule"/>). New pawns after that are set up by the game as for any work type.
/// </summary>
public class PhobosWorkTypeSeedExtension : DefModExtension
{
    /// <summary>The work types to take the starting priority from, in order: the first one a pawn is able to do decides.</summary>
    public List<WorkTypeDef> copyPriorityFrom = new();
}

/// <summary>
/// Gives a work type new to a save its starting priorities, once per save and work type (<see cref="PhobosWorkTypeSeedExtension"/>). It
/// keeps only the names of the work types already done, so it is safe to add mid-game and does nothing at later loads. In the core, which
/// always loads, so a save never names a class that can vanish.
/// </summary>
public sealed class GameComponent_PhobosWorkTypeSeeding : GameComponent
{
    List<string> seeded = new();

    public GameComponent_PhobosWorkTypeSeeding(Game game) { }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        foreach (WorkTypeDef target in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            PhobosWorkTypeSeedExtension? seed = target.GetModExtension<PhobosWorkTypeSeedExtension>();
            if (seed == null || seeded.Contains(target.defName)) continue;
            int started = 0;
            foreach (Pawn pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction)
            {
                if (Seed(pawn, target, seed)) started++;
            }
            seeded.Add(target.defName);
            FrameworkMod.Logger.Info($"Work type {target.defName} started for {started} of the colony's pawns.");
            if (started > 0 && Seen.Wants("framework.work-type-seeded", null, target.defName))
                Seen.Note("framework.work-type-seeded", null, $"{target.label}: {started} pawns", target.defName);
        }
    }

    /// <summary>Starts the work type for one pawn; false when it already had a priority, cannot do it, or has nothing to copy.</summary>
    static bool Seed(Pawn pawn, WorkTypeDef target, PhobosWorkTypeSeedExtension seed)
    {
        if (pawn.workSettings == null || !pawn.workSettings.EverWork || pawn.WorkTypeIsDisabled(target)) return false;
        if (pawn.workSettings.GetPriority(target) > 0) return false;
        int priority = WorkTypeSeedRule.StartingPriority(
            seed.copyPriorityFrom.Where(source => source != null)
                .Select(source => (!pawn.WorkTypeIsDisabled(source), pawn.workSettings.GetPriority(source))));
        if (priority <= 0) return false;
        pawn.workSettings.SetPriority(target, priority);
        return true;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref seeded, "PhobosFramework_SeededWorkTypes", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) seeded ??= new List<string>();
    }
}
