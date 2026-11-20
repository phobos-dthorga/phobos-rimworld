using System.Collections.Generic;
using System.Reflection;
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
    /// <summary>The least a cell takes of any seed, kilograms: a crop that yields nothing to weigh (a flower) is still sown with something
    /// (the fault-read of 21 Nov 2026, F16: one unit of bought seed had sown any number of such cells). About a rice cell's worth. Authored [A].</summary>
    public const double LeastKilogramsPerCell = 0.005;

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

    // The last "no seed this colonist can reach" answer (the fault-read of 21 Nov 2026, F9): a sowing scan asks for every cell of a zone,
    // and the answer does not depend on the cell, so a zone of a thousand cells had cost a thousand searches. Good for one tick.
    static Pawn? noSeedPawn;
    static ThingDef? noSeedDef;
    static int noSeedTick = -1;

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
        int now = Find.TickManager.TicksGame;
        if (pawn == noSeedPawn && seed == noSeedDef && now == noSeedTick)
        {
            JobFailReason.Is("PhobosFarming_HandsNoSeed".Translate(seed.label.Named("SEED"), crop.label.Named("CROP")).Resolve());
            return null;
        }
        int units = System.Math.Max(1, (int)System.Math.Ceiling(CellsPerFetch * SeedRates.OwnGrainKilogramsPerCell(crop) / System.Math.Max(0.0001, seed.BaseMass)));
        Thing? stack = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(seed), PathEndMode.ClosestTouch,
            TraverseParms.For(pawn), 9999f, t => !t.IsForbidden(pawn) && pawn.CanReserve(t, 10, System.Math.Min(units, t.stackCount)));
        if (stack == null)
        {
            noSeedPawn = pawn;
            noSeedDef = seed;
            noSeedTick = now;
            JobFailReason.Is("PhobosFarming_HandsNoSeed".Translate(seed.label.Named("SEED"), crop.label.Named("CROP")).Resolve());
            return null;
        }
        Job take = JobMaker.MakeJob(JobDefOf.TakeCountToInventory, stack);
        take.count = System.Math.Min(units, stack.stackCount);
        CombatExtendedHold.Note(pawn, stack, take.count);
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
/// Combat Extended's loadouts drop what a colonist carries that their loadout does not list, sooner than the colonist gets to sow (its
/// loadout job outranks work), so the handful of seed was fetched and dropped over and over (the fault-read of 21 Nov 2026, F8). Combat
/// Extended has its own answer for goods a job hands a colonist, the hold tracker, which it uses for animal feed the same way: this tells
/// it about the seed. By name, and only when Combat Extended is there; no reference to its assembly.
/// </summary>
static class CombatExtendedHold
{
    const string TrackerTypeName = "CombatExtended.Utility_HoldTracker";

    static bool looked;
    static MethodInfo? notify;

    public static void Note(Pawn pawn, Thing stack, int count)
    {
        if (!looked)
        {
            looked = true;
            System.Type? tracker = AccessTools.TypeByName(TrackerTypeName);
            if (tracker != null)
            {
                notify = AccessTools.Method(tracker, "Notify_HoldTrackerItem", new[] { typeof(Pawn), typeof(Thing), typeof(int) });
                if (notify == null) FarmingMod.Logger.Warn("Combat Extended's hold tracker has changed shape; a loadout may drop hand-carried seed.");
            }
        }
        if (notify == null) return;
        try
        {
            notify.Invoke(null, new object[] { pawn, stack, count });
        }
        catch (System.Exception e)
        {
            notify = null;
            FarmingMod.Logger.Warn("Combat Extended's hold tracker refused hand-carried seed, and is left alone from here: " + (e.InnerException ?? e).Message);
        }
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
