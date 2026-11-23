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
/// approach them, sort'a like when they do to repair them"). Since Framework 0.180.0 Vehicle Framework's vehicles by haulers, as buildings
/// are (an aircraft's chaff and flare magazine; owner, 20 Nov 2026: countermeasures "handled as ammunition"). Found once after Defs load;
/// nothing is saved and nothing ticks. The work givers and the job driver only call it.
/// </summary>
public static class TopUps
{
    /// <summary>What holds the store: who tops it up follows from it.</summary>
    enum Holder
    {
        Building,
        Mechanoid,
        Vehicle,
    }

    /// <summary>A Def that carries a named comp, with the Def that names it.</summary>
    readonly struct Kind
    {
        public readonly ThingDef Def;
        public readonly PhobosTopUpDef TopUp;
        public readonly Type CompType;
        public readonly Holder Holder;

        public Kind(ThingDef def, PhobosTopUpDef topUp, Type compType, Holder holder)
        {
            Def = def;
            TopUp = topUp;
            CompType = compType;
            Holder = holder;
        }

        public bool Mechanoid => Holder == Holder.Mechanoid;
    }

    /// <summary>Vehicle Framework's vehicle class, by name: the core references none of its assemblies.</summary>
    const string VehicleClassName = "Vehicles.VehiclePawn";

    static List<Kind>? kinds;
    static readonly HashSet<PhobosTopUpDef> reported = new();

    static List<Kind> Kinds
    {
        get
        {
            if (kinds != null) return kinds;
            kinds = new List<Kind>();
            Type? vehicleClass = OptionalMods.TypeNamed(VehicleClassName);
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
                    // A vehicle first: it is a pawn with a race of its own, never a building, and is read by its class.
                    Holder? holder = vehicleClass != null && def.thingClass != null && vehicleClass.IsAssignableFrom(def.thingClass)
                        ? (topUp.vehicles ? Holder.Vehicle : (Holder?)null)
                        : def.category == ThingCategory.Building && topUp.buildings ? Holder.Building
                        : def.race is { IsMechanoid: true } && topUp.mechanoids ? Holder.Mechanoid
                        : (Holder?)null;
                    if (holder == null) continue;
                    if (def.comps.Any(c => c.compClass != null && type.IsAssignableFrom(c.compClass))) kinds.Add(new Kind(def, topUp, type, holder.Value));
                }
            }
            return kinds;
        }
    }

    /// <summary>How many kinds of building are topped up, for the load line.</summary>
    public static int BuildingKindCount => Kinds.Count(k => k.Holder == Holder.Building);

    /// <summary>How many kinds of mechanoid are topped up, for the load line.</summary>
    public static int MechanoidKindCount => Kinds.Count(k => k.Holder == Holder.Mechanoid);

    /// <summary>How many kinds of vehicle are topped up, for the load line (Framework 0.180.0).</summary>
    public static int VehicleKindCount => Kinds.Count(k => k.Holder == Holder.Vehicle);

    /// <summary>Whether a thing's store is a vehicle's (Framework 0.180.0).</summary>
    public static bool IsVehicle(Thing? thing) => thing != null && KindOf(thing) is { Holder: Holder.Vehicle };

    /// <summary>
    /// Whether topping a thing up holds it for the one doing it. Only a mechanoid is held, as the game's mech repair holds it. A building is
    /// not (Framework 0.240.0; the owner's report of 23 Nov 2026, drone bays "very rarely topped-up"): a drone walking home holds its
    /// platform for the whole walk, so a hauler that asked to hold it was turned away all that while. A vehicle is not (Framework 0.180.0),
    /// as Vehicle Framework's own loading job does not, so a hauler never keeps a pilot from boarding. Two haulers still never bring the
    /// same load: each counts what the others are carrying to it (<see cref="AlreadyComing"/>).
    /// </summary>
    public static bool Holds(Thing? thing) => thing != null && KindOf(thing) is { Holder: Holder.Mechanoid };

    /// <summary>The player's buildings on a map whose store a <see cref="PhobosTopUpDef"/> names, wanting or not (Framework 0.240.0: where
    /// a wreck may be taken).</summary>
    public static IEnumerable<Thing> BuildingStores(Map map, Faction faction)
    {
        foreach (Kind kind in Kinds)
        {
            if (kind.Holder != Holder.Building) continue;
            List<Thing> things = map.listerThings.ThingsOfDef(kind.Def);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].Faction == faction) yield return things[i];
            }
        }
    }

    /// <summary>The material a thing's store holds, or null: not a kind we top up, or unread (Framework 0.240.0).</summary>
    public static ThingDef? MaterialOf(Thing thing) => KindOf(thing) is { } kind && CompOf(thing, kind) is { } comp ? kind.TopUp.MaterialIn(comp) : null;

    /// <summary>How much more a thing's store can hold: 0 for a thing we do not top up, -1 where no capacity is known (Framework 0.240.0).</summary>
    public static int RoomIn(Thing thing) => KindOf(thing) is { } kind && CompOf(thing, kind) is { } comp ? kind.TopUp.RoomIn(comp) : 0;

    /// <summary>
    /// Puts <paramref name="count"/> of a material, made new, into a thing's store as far as it has room, and sets the rest down beside it
    /// (Framework 0.240.0: a wreck's salvage). Returns how much went into the store.
    /// </summary>
    public static int TakeIn(Thing target, ThingDef material, int count)
    {
        if (count <= 0 || target.MapHeld is not { } map) return 0;
        (int into, int beside) = RecallRule.TakeBack(count, RoomIn(target));
        int stored = 0;
        if (StoreOf(target) is { } store)
        {
            int left = into;
            while (left > 0)
            {
                Thing stack = ThingMaker.MakeThing(material);
                stack.stackCount = Math.Min(left, Math.Max(1, material.stackLimit));
                left -= stack.stackCount;
                int size = stack.stackCount;
                if (store.TryAdd(stack)) stored += size;
                else
                {
                    beside += stack.stackCount;
                    if (!stack.Destroyed) stack.Destroy();
                }
            }
        }
        else beside += into;
        while (beside > 0)
        {
            Thing stack = ThingMaker.MakeThing(material);
            stack.stackCount = Math.Min(beside, Math.Max(1, material.stackLimit));
            beside -= stack.stackCount;
            GenPlace.TryPlaceThing(stack, target.PositionHeld, map, ThingPlaceMode.Near);
        }
        return stored;
    }

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

    /// <summary>The player's buildings and vehicles (for haulers), or mechanoids (for a mechanitor), on a map that want more of their
    /// material now, enough to be worth a walk.</summary>
    public static IEnumerable<Thing> Wanting(Map map, Faction faction, bool mechanoids)
    {
        if (mechanoids)
        {
            foreach (Pawn pawn in PawnsWanting(map, faction, Holder.Mechanoid)) yield return pawn;
            yield break;
        }
        bool anyVehicles = false;
        foreach (Kind kind in Kinds)
        {
            if (kind.Holder == Holder.Vehicle) anyVehicles = true;
            if (kind.Holder != Holder.Building) continue;
            List<Thing> things = map.listerThings.ThingsOfDef(kind.Def);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing.Faction != faction) continue;
                if (WantOf(thing, kind) is { } want && TopUpRule.Worth(want.Amount, want.LeastLoad, forced: false)) yield return thing;
            }
        }
        if (!anyVehicles) yield break;
        foreach (Pawn vehicle in PawnsWanting(map, faction, Holder.Vehicle)) yield return vehicle;
    }

    /// <summary>The faction's spawned pawns of one holder kind that want enough to be worth a walk: the list the game's mech repair scans.</summary>
    static IEnumerable<Pawn> PawnsWanting(Map map, Faction faction, Holder holder)
    {
        List<Pawn> pawns = map.mapPawns.SpawnedPawnsInFaction(faction);
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            if (KindOf(pawn) is { } kind && kind.Holder == holder && WantOf(pawn, kind) is { } want && TopUpRule.Worth(want.Amount, want.LeastLoad, forced: false))
            {
                yield return pawn;
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
    /// The job that tops a thing up, or null (with the reason set for the right-click menu where there is one). A full load per trip,
    /// gathered from as many stacks as it takes (Framework 0.240.0), put straight into the comp's store (JobDriver_PhobosTopUp). A
    /// mechanoid is topped up only by a mechanitor.
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
        // Only a mechanoid is held (Holds): a drone walking home holds its platform, and a building or vehicle need not be.
        if (Holds(target) && !pawn.CanReserve(target, 1, -1, null, forced)) return null;
        int toBring = TopUpRule.ToBring(want.Amount, AlreadyComing(target, pawn));
        if (!TopUpRule.Worth(toBring, want.LeastLoad, forced)) return null;
        // As much as the pawn can carry in one trip, gathered from as many stacks as it takes (Framework 0.240.0: one small stack a trip,
        // 17 or 20 steel at a time, could not keep up with a platform sending drones out), as the game's own hauling to a container does.
        int load = Math.Min(toBring, pawn.carryTracker.MaxStackSpaceEver(want.Material));
        if (load <= 0) return null;
        List<Thing> found = HaulAIUtility.FindFixedIngredientCount(pawn, want.Material, load);
        if (found.NullOrEmpty())
        {
            JobFailReason.Is("PhobosFramework_TopUpNoneToFetch".Translate(want.Material.label.Named("MATERIAL")));
            return null;
        }
        Job job = JobMaker.MakeJob(TopUpDefOf.PhobosFramework_TopUp, found[0], target);
        job.count = load;
        if (found.Count > 1) job.targetQueueA = found.Skip(1).Select(t => new LocalTargetInfo(t)).ToList();
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
    public static int Deliver(Pawn pawn, Thing target, int stacks = 1)
    {
        Thing? carried = pawn.carryTracker.CarriedThing;
        if (carried == null) return 0;
        if (!IsVehicle(target) && target is not Pawn)
        {
            // Evidence for row 383 (Framework 0.240.0): a load gathered from several stacks, and one brought while another (a drone walking
            // home) holds the building.
            if (stacks > 1 && Seen.Wants("framework.top-up-gathered", target))
            {
                Seen.Note("framework.top-up-gathered", target, carried.stackCount + " " + carried.def.label + " from " + stacks + " stacks");
            }
            if (Seen.Wants("framework.top-up-while-held", target) && target.Map?.reservationManager.FirstRespectedReserver(target, pawn) is { } holder)
            {
                Seen.Note("framework.top-up-while-held", target, "held by " + holder.LabelShort);
            }
        }
        int wanted = WantOf(target)?.Amount ?? 0;
        int moved = 0;
        if (StoreOf(target) is { } store && wanted > 0)
        {
            moved = pawn.carryTracker.innerContainer.TryTransferToContainer(carried, store, Math.Min(wanted, carried.stackCount));
        }
        if (pawn.carryTracker.CarriedThing != null) pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
        if (moved > 0)
        {
            if (IsVehicle(target)) { if (Seen.Wants("framework.top-up-vehicle", target)) Seen.Note("framework.top-up-vehicle", target, moved.ToString()); }
            else if (target is Pawn) { if (Seen.Wants("framework.top-up-mechanoid", target)) Seen.Note("framework.top-up-mechanoid", target, moved.ToString()); }
            else if (Seen.Wants("framework.top-up", target)) Seen.Note("framework.top-up", target, moved.ToString());
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

    /// <summary>How many stacks this load was gathered from, for the seen-in-play note only.</summary>
    int stacks;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref stacks, "PhobosStacks", 0);
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (!pawn.Reserve(job.GetTarget(Material), job, 1, -1, null, errorOnFailed)) return false;
        // The further stacks of one load (Framework 0.240.0), as the game's own hauling to a container holds them.
        pawn.ReserveAsManyAsPossible(job.GetTargetQueue(Material), job);
        return !TopUps.Holds(job.GetTarget(Target).Thing) || pawn.Reserve(job.GetTarget(Target), job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(Target);
        this.FailOnDespawnedOrNull(Target);
        this.FailOnForbidden(Target);
        // A vehicle has no mind to turn hostile (Framework 0.180.0): only its faction can, and leaving the map ends the job above.
        this.FailOn(() => job.GetTarget(Target).Thing is Pawn holder && (TopUps.IsVehicle(holder) ? holder.HostileTo(pawn) : holder.InAggroMentalState || holder.HostileTo(pawn)));
        Toil fetch = Toils_Goto.GotoThing(Material, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(Material).FailOnSomeonePhysicallyInteracting(Material);
        yield return fetch;
        yield return Toils_Haul.StartCarryThing(Material, putRemainderInQueue: false, subtractNumTakenFromJobCount: true);
        Toil counted = ToilMaker.MakeToil("PhobosTopUpCountStack");
        counted.initAction = () => stacks++;
        counted.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return counted;
        // Then the next stack of the load, while there is room in the pawn's hands (Framework 0.240.0).
        yield return Toils_Haul.JumpIfAlsoCollectingNextTargetInQueue(fetch, Material);
        yield return Toils_Goto.GotoThing(Target, PathEndMode.Touch);
        Toil deliver = ToilMaker.MakeToil("PhobosTopUpDeliver");
        deliver.initAction = () => TopUps.Deliver(pawn, job.GetTarget(Target).Thing, stacks);
        deliver.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return deliver;
    }
}
