using System.Collections.Generic;
using HarmonyLib;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Farming;

/// <summary>The seed rates machines and hands share: one place, so a drill and a colonist sow a cell with the same seed.</summary>
public static class SeedRates
{
    /// <summary>The share of a cell's harvest, by weight, a cell takes as seed: about 3 % (the Birchip Cropping Group and Teagasc figures
    /// in docs/development/farming-machines.md). Authored [A].</summary>
    public const float SeedShare = 0.03f;
    /// <summary>What cleaning takes off a crop's own grain used as seed. Authored [A].</summary>
    public const float CleaningLoss = 0.1f;

    /// <summary>The kilograms of a crop's own grain a cell takes, cleaning loss included.</summary>
    public static double OwnGrainKilogramsPerCell(ThingDef crop, float seedShare = SeedShare, float cleaningLoss = CleaningLoss)
    {
        double perCell = FieldWorkRule.SeedKilogramsPerCell(crop.plant.harvestYield > 0 ? (int)System.Math.Round(crop.plant.harvestYield) : 0,
            crop.plant.harvestedThingDef?.BaseMass ?? 0, seedShare);
        return perCell / System.Math.Max(0.05, 1 - cleaningLoss);
    }
}

/// <summary>
/// "Hands use seed too" (the owner's switch, off by default: docs/development/farming-batch-two.md, "Seed"). With it on, a colonist
/// sowing a crop whose harvest is its own seed (its crop class says so) carries a handful of that grain, fetched from a stockpile into
/// their inventory before the first cell, and uses it up cell by cell at the drill's rate. Without seed anywhere they can reach, the sowing
/// job is refused and the reason shown. Crops whose harvest is not seed (cotton, hay, crops in no class) are sown free, as the game does.
/// What a colonist owes below one unit is kept per colonist and not saved: at most a unit's worth is forgotten at a load.
/// </summary>
public static class HandSeed
{
    /// <summary>How many cells' seed a colonist fetches at once. Authored [A].</summary>
    const int CellsPerFetch = 20;

    static readonly Dictionary<Pawn, double> owed = new();
    static Game? game;

    static void Check()
    {
        if (game == Current.Game) return;
        owed.Clear();
        game = Current.Game;
    }

    /// <summary>The job a colonist does instead of this sowing job: the job itself when they carry seed or the crop needs none, a fetch
    /// of seed into their inventory first, or null (with the reason given to the game) when there is none they can reach.</summary>
    public static Job? Instead(Pawn pawn, Job job)
    {
        ThingDef? crop = job.plantDefToSow;
        if (crop == null || Crops.OwnSeed(crop) is not { } seed || pawn.inventory == null || pawn.Map == null) return job;
        if (pawn.inventory.Count(seed) > 0) return job;
        int units = System.Math.Max(1, (int)System.Math.Ceiling(CellsPerFetch * SeedRates.OwnGrainKilogramsPerCell(crop) / System.Math.Max(0.0001, seed.BaseMass)));
        Thing? stack = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(seed), PathEndMode.ClosestTouch,
            TraverseParms.For(pawn), 9999f, t => !t.IsForbidden(pawn) && pawn.CanReserve(t, 10, System.Math.Min(units, t.stackCount)));
        if (stack == null)
        {
            JobFailReason.Is("PhobosFarming_HandsNoSeed".Translate(seed.label.Named("SEED"), crop.label.Named("CROP")).Resolve());
            return null;
        }
        Job take = JobMaker.MakeJob(JobDefOf.TakeCountToInventory, stack);
        take.count = System.Math.Min(units, stack.stackCount);
        if (Seen.Wants("farming.hand-seed.fetch", pawn)) Seen.Note("farming.hand-seed.fetch", pawn, seed.label);
        return take;
    }

    /// <summary>A colonist has sown a cell of this crop by hand: its seed comes out of their inventory, a unit each time a unit is owed.</summary>
    public static void Sowed(Pawn pawn, ThingDef crop)
    {
        if (Crops.OwnSeed(crop) is not { } seed || pawn.inventory == null) return;
        Check();
        owed.TryGetValue(pawn, out double kilograms);
        kilograms += SeedRates.OwnGrainKilogramsPerCell(crop);
        int used = 0;
        while (kilograms >= seed.BaseMass && seed.BaseMass > 0 && Take(pawn, seed))
        {
            kilograms -= seed.BaseMass;
            used++;
        }
        // Out of seed partway: what is still owed is forgiven rather than carried, so the next handful starts clean.
        owed[pawn] = pawn.inventory.Count(seed) > 0 ? kilograms : 0;
        if (used > 0 && Seen.Wants("farming.hand-seed.used", pawn)) Seen.Note("farming.hand-seed.used", pawn, seed.label);
    }

    static bool Take(Pawn pawn, ThingDef seed)
    {
        ThingOwner<Thing> held = pawn.inventory.innerContainer;
        for (int i = 0; i < held.Count; i++)
        {
            if (held[i].def != seed) continue;
            held[i].SplitOff(1).Destroy();
            return true;
        }
        return false;
    }
}

/// <summary>
/// "Hands use seed too", patch one: the sowing work giver's job for a cell. Reason for the patch: the game's sowing needs no seed and has
/// no extension point for a cost. A postfix, run last so it sees the job other mods' postfixes (Smart Farming, Vanilla Plants Expanded
/// More Plants) settled on; the game's method always runs, and nothing changes with the switch off.
/// </summary>
[HarmonyPatch(typeof(WorkGiver_GrowerSow), nameof(WorkGiver_GrowerSow.JobOnCell))]
public static class Patch_WorkGiverGrowerSow_JobOnCell
{
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(Pawn pawn, ref Job __result)
    {
        if (__result == null || !FarmingMod.Settings.handsUseSeed || __result.def != JobDefOf.Sow) return;
        __result = HandSeed.Instead(pawn, __result)!;
    }
}

/// <summary>
/// "Hands use seed too", patch two: the colonist's "plants sown" record, which the game's sowing job counts up exactly once for each cell it
/// finishes sowing (JobDriver_PlantSow, read 20 Nov 2026). Reason for the patch: the finished cell is inside the job's own toil, with no
/// event of its own. A postfix; nothing changes with the switch off or for any other record.
/// </summary>
[HarmonyPatch(typeof(Pawn_RecordsTracker), nameof(Pawn_RecordsTracker.Increment))]
public static class Patch_PawnRecordsTracker_Increment
{
    public static void Postfix(Pawn_RecordsTracker __instance, RecordDef def)
    {
        if (def != RecordDefOf.PlantsSown || !FarmingMod.Settings.handsUseSeed) return;
        Pawn? pawn = __instance.pawn;
        if (pawn?.CurJob is { plantDefToSow: { } crop } job && job.def == JobDefOf.Sow) HandSeed.Sowed(pawn, crop);
    }
}
