using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Framework.TopUp;

/// <summary>
/// The service behind topping up (Framework 0.134.0; docs/development/top-up-design.md): which Defs carry a comp a
/// <see cref="PhobosTopUpDef"/> names, what each such thing wants now, and the job that brings it. Buildings are topped up by haulers;
/// since Framework 0.135.0 mechanoids by a mechanitor, as the game's own mech repair is (owner, 18 Nov 2026: "have the mechanitor
/// approach them, sort'a like when they do to repair them"). Found once after Defs load; nothing is saved and nothing ticks. The work
/// givers and the job driver only call it.
/// </summary>
public static class TopUps
{
    /// <summary>A Def that carries a named comp, with the Def that names it.</summary>
    readonly struct Kind
    {
        public readonly ThingDef Def;
        public readonly PhobosTopUpDef TopUp;
        public readonly Type CompType;
        public readonly bool Mechanoid;

        public Kind(ThingDef def, PhobosTopUpDef topUp, Type compType, bool mechanoid)
        {
            Def = def;
            TopUp = topUp;
            CompType = compType;
            Mechanoid = mechanoid;
        }
    }

    static List<Kind>? kinds;
    static readonly HashSet<PhobosTopUpDef> reported = new();

    static List<Kind> Kinds
    {
        get
        {
            if (kinds != null) return kinds;
            kinds = new List<Kind>();
            foreach (PhobosTopUpDef topUp in DefDatabase<PhobosTopUpDef>.AllDefsListForReading)
            {
                Type? type = topUp.CompType;
                if (type == null)
                {
                    ReportProblem(topUp);
                    continue;
                }
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    if (def.comps == null) continue;
                    bool building = def.category == ThingCategory.Building && topUp.buildings;
                    bool mechanoid = def.race is { IsMechanoid: true } && topUp.mechanoids;
                    if (!building && !mechanoid) continue;
                    if (def.comps.Any(c => c.compClass != null && type.IsAssignableFrom(c.compClass))) kinds.Add(new Kind(def, topUp, type, mechanoid));
                }
            }
            return kinds;
        }
    }

    /// <summary>How many kinds of building are topped up, for the load line.</summary>
    public static int BuildingKindCount => Kinds.Count(k => !k.Mechanoid);

    /// <summary>How many kinds of mechanoid are topped up, for the load line.</summary>
    public static int MechanoidKindCount => Kinds.Count(k => k.Mechanoid);

    /// <summary>The Defs that stand down, with why, for the load line.</summary>
    public static IEnumerable<PhobosTopUpDef> Problems => DefDatabase<PhobosTopUpDef>.AllDefsListForReading.Where(d => d.problem != null);

    /// <summary>Says once, in the log, that a Def's names no longer fit the code they name.</summary>
    public static void ReportProblem(PhobosTopUpDef topUp)
    {
        if (topUp.problem == null || !reported.Add(topUp)) return;
        FrameworkMod.Logger.Warn("Topping up stands down for " + topUp.defName + ": " + topUp.problem
            + ". The mod it names may have changed; its machines must be filled by hand until the Def is put right.");
    }

    /// <summary>Whether topping up runs at all for buildings or for mechanoids: the player's setting, and at least one such kind.</summary>
    public static bool Active(bool mechanoids) => FrameworkMod.Settings.topUpMachines && Kinds.Any(k => k.Mechanoid == mechanoids);

    /// <summary>The player's buildings, or mechanoids, on a map that want more of their material now, enough to be worth a walk.</summary>
    public static IEnumerable<Thing> Wanting(Map map, Faction faction, bool mechanoids)
    {
        if (mechanoids)
        {
            // Spawned mechanoids of the faction, the same list the game's mech repair scans.
            List<Pawn> pawns = map.mapPawns.SpawnedPawnsInFaction(faction);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (KindOf(pawn) is { Mechanoid: true } kind && WantOf(pawn, kind) is { } want && TopUpRule.Worth(want.Amount, want.LeastLoad, forced: false))
                {
                    yield return pawn;
                }
            }
            yield break;
        }
        foreach (Kind kind in Kinds)
        {
            if (kind.Mechanoid) continue;
            List<Thing> things = map.listerThings.ThingsOfDef(kind.Def);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing.Faction != faction) continue;
                if (WantOf(thing, kind) is { } want && TopUpRule.Worth(want.Amount, want.LeastLoad, forced: false)) yield return thing;
            }
        }
    }

    static Kind? KindOf(Thing thing)
    {
        List<Kind> all = Kinds;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Def == thing.def) return all[i];
        }
        return null;
    }

    /// <summary>What a thing wants now, or null: it is not a kind we top up, or it wants nothing.</summary>
    public static TopUpWant? WantOf(Thing thing) => KindOf(thing) is { } kind ? WantOf(thing, kind) : null;

    static TopUpWant? WantOf(Thing thing, Kind kind) => CompOf(thing, kind) is { } comp ? kind.TopUp.Read(comp) : null;

    static ThingComp? CompOf(Thing thing, Kind kind)
    {
        if (thing is not ThingWithComps withComps) return null;
        List<ThingComp> comps = withComps.AllComps;
        for (int i = 0; i < comps.Count; i++)
        {
            if (kind.CompType.IsInstanceOfType(comps[i])) return comps[i];
        }
        return null;
    }

    /// <summary>
    /// The store the named comp keeps, where the job puts what it brings. Put there directly, never through the game's choice of a
    /// thing's holder: the game picks a thing's first comp that holds anything, and a mechanoid may carry another (Fortified's
    /// dead man's switch holds things too), so the material could land in the wrong one.
    /// </summary>
    public static ThingOwner? StoreOf(Thing thing) => KindOf(thing) is { } kind && CompOf(thing, kind) is IThingHolder holder ? holder.GetDirectlyHeldThings() : null;

    /// <summary>How much of a thing's material others are already carrying to it.</summary>
    public static int AlreadyComing(Thing target, Pawn except)
    {
        int coming = 0;
        List<Pawn> pawns = target.Map.mapPawns.SpawnedPawnsInFaction(except.Faction);
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            if (pawn == except) continue;
            Job? job = pawn.CurJob;
            if (job == null || job.def != TopUpDefOf.PhobosFramework_TopUp || job.targetB.Thing != target) continue;
            coming += Math.Max(0, job.count) + (pawn.carryTracker?.CarriedThing?.stackCount ?? 0);
        }
        return coming;
    }

    /// <summary>Whether a mechanitor must leave this mechanoid be for now, as the game's own mech repair asks (WorkGiver_RepairMech): it
    /// is hostile, berserk, burning or fighting.</summary>
    static bool MechanoidBusy(Pawn mech, Pawn mechanitor) =>
        mech.InAggroMentalState || mech.HostileTo(mechanitor) || mech.IsBurning() || mech.IsAttacking();

    /// <summary>
    /// The job that tops a thing up, or null (with the reason set for the right-click menu where there is one). One stack per trip, put
    /// straight into the comp's store (JobDriver_PhobosTopUp). A mechanoid is topped up only by a mechanitor.
    /// </summary>
    public static Job? JobFor(Pawn pawn, Thing target, bool forced)
    {
        if (KindOf(target) is not { } kind || WantOf(target, kind) is not { } want) return null;
        if (target.Faction != pawn.Faction || target.IsForbidden(pawn) || target.IsBurning()) return null;
        if (kind.Mechanoid)
        {
            if (!MechanitorUtility.IsMechanitor(pawn) || target is not Pawn mech || MechanoidBusy(mech, pawn)) return null;
        }
        if (StoreOf(target) == null) return null;
        if (!TopUpRule.Worth(want.Amount, want.LeastLoad, forced)) return null;
        if (!pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly)) return null;
        if (!pawn.CanReserve(target, 1, -1, null, forced)) return null;
        int toBring = TopUpRule.ToBring(want.Amount, AlreadyComing(target, pawn));
        if (!TopUpRule.Worth(toBring, want.LeastLoad, forced)) return null;
        List<Thing> found = HaulAIUtility.FindFixedIngredientCount(pawn, want.Material, toBring);
        if (found.NullOrEmpty())
        {
            JobFailReason.Is("PhobosFramework_TopUpNoneToFetch".Translate(want.Material.label.Named("MATERIAL")));
            return null;
        }
        Job job = JobMaker.MakeJob(TopUpDefOf.PhobosFramework_TopUp, found[0], target);
        job.count = Math.Min(toBring, found[0].stackCount);
        if (FrameworkMod.Logger.DebugEnabled)
        {
            FrameworkMod.Logger.Debug(pawn.LabelShort + " tops up " + target.LabelShort + " (" + target.ThingID + ") with " + job.count + " "
                + want.Material.defName + "; it wants " + want.Amount + ", least load " + want.LeastLoad);
        }
        return job;
    }

    /// <summary>
    /// The job's last step: puts what the pawn carries into the target's store, no more than it still wants (others may have filled it
    /// meanwhile), and drops the rest beside it. Returns how much went in.
    /// </summary>
    public static int Deliver(Pawn pawn, Thing target)
    {
        Thing? carried = pawn.carryTracker.CarriedThing;
        if (carried == null) return 0;
        int wanted = WantOf(target)?.Amount ?? 0;
        int moved = 0;
        if (StoreOf(target) is { } store && wanted > 0)
        {
            moved = pawn.carryTracker.innerContainer.TryTransferToContainer(carried, store, Math.Min(wanted, carried.stackCount));
        }
        if (pawn.carryTracker.CarriedThing != null) pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
        if (moved > 0)
        {
            if (target is Pawn) Seen.Note("framework.top-up-mechanoid", target, moved.ToString());
            else Seen.Note("framework.top-up", target, moved.ToString());
        }
        return moved;
    }
}

[DefOf]
public static class TopUpDefOf
{
    public static JobDef PhobosFramework_TopUp = null!;

    static TopUpDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(TopUpDefOf));
}

/// <summary>"Seen in play" notes for this part (Debug/SeenInPlay; AGENTS.md: a feature with a test row notes itself).</summary>
static class Seen
{
    public static void Note(string key, Thing? on = null, string? what = null)
    {
        if (!Debug.SeenInPlay.On) return;
        string? detail = on == null ? what : what == null ? on.LabelShort : on.LabelShort + ": " + what;
        Debug.SeenInPlay.Note("D'thorga Framework", FrameworkMod.ModVersion, key, detail, on);
    }
}

/// <summary>
/// Hauling work: tops up the buildings a <see cref="PhobosTopUpDef"/> names. Skipped at once on a map with nothing to top up, so it
/// costs a colony without such buildings one check per look for work.
/// </summary>
public class WorkGiver_PhobosTopUp : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Undefined);

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => !TopUps.Active(mechanoids: false) || !TopUps.Wanting(pawn.Map, pawn.Faction, mechanoids: false).Any();

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => TopUps.Wanting(pawn.Map, pawn.Faction, mechanoids: false);

    public override Job? JobOnThing(Pawn pawn, Thing thing, bool forced = false) => TopUps.Active(mechanoids: false) ? TopUps.JobFor(pawn, thing, forced) : null;
}

/// <summary>
/// A mechanitor's work (Framework 0.135.0): tops up the colony's mechanoids that carry a store a <see cref="PhobosTopUpDef"/> names,
/// walking to them as the game's own mech repair does (WorkGiver_RepairMech: mechanitors only, any danger, the same Smithing work).
/// </summary>
public class WorkGiver_PhobosTopUpMechanoid : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Undefined);

    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    public override bool ShouldSkip(Pawn pawn, bool forced = false) =>
        !MechanitorUtility.IsMechanitor(pawn) || !TopUps.Active(mechanoids: true) || !TopUps.Wanting(pawn.Map, pawn.Faction, mechanoids: true).Any();

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => TopUps.Wanting(pawn.Map, pawn.Faction, mechanoids: true);

    public override Job? JobOnThing(Pawn pawn, Thing thing, bool forced = false) => TopUps.Active(mechanoids: true) ? TopUps.JobFor(pawn, thing, forced) : null;
}

/// <summary>
/// Fetches one stack of a material and puts it into a building's or mechanoid's store (Framework 0.135.0). Its own driver, not the game's
/// haul-to-container, so the material goes into the named comp's store and nowhere else. A mechanoid that walks off is followed; one that
/// becomes hostile, berserk or is lost ends the job, and the pawn drops what it carries.
/// </summary>
public class JobDriver_PhobosTopUp : JobDriver
{
    const TargetIndex Material = TargetIndex.A;
    const TargetIndex Target = TargetIndex.B;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(job.GetTarget(Material), job, 1, -1, null, errorOnFailed) && pawn.Reserve(job.GetTarget(Target), job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(Target);
        this.FailOnDespawnedOrNull(Target);
        this.FailOnForbidden(Target);
        this.FailOn(() => job.GetTarget(Target).Thing is Pawn mech && (mech.InAggroMentalState || mech.HostileTo(pawn)));
        yield return Toils_Goto.GotoThing(Material, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(Material).FailOnSomeonePhysicallyInteracting(Material);
        yield return Toils_Haul.StartCarryThing(Material, putRemainderInQueue: false, subtractNumTakenFromJobCount: true);
        yield return Toils_Goto.GotoThing(Target, PathEndMode.Touch);
        Toil deliver = ToilMaker.MakeToil("PhobosTopUpDeliver");
        deliver.initAction = () => TopUps.Deliver(pawn, job.GetTarget(Target).Thing);
        deliver.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return deliver;
    }
}
