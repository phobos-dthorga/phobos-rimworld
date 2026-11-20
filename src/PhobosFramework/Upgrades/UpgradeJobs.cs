using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Framework.Upgrades;

[DefOf]
public static class UpgradeJobDefOf
{
    public static JobDef PhobosFramework_DeliverUpgradeMaterials = null!;
    public static JobDef PhobosFramework_FitBuildingUpgrade = null!;

    static UpgradeJobDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(UpgradeJobDefOf));
}

/// <summary>
/// Construction work: brings the materials an ordered upgrade still lacks to its building, one stack at a time, as the game's own builders
/// bring materials to a frame. Skipped at once on a map with no upgrade ordered.
/// </summary>
public class WorkGiver_PhobosDeliverUpgradeMaterials : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Undefined);

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => !Wanting(pawn).Any();

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => Wanting(pawn).Select(comp => (Thing)comp.parent);

    static IEnumerable<CompPhobosBuildingUpgrades> Wanting(Pawn pawn) =>
        BuildingUpgrades.WithOrders(pawn.Map, pawn.Faction).Where(comp => !comp.Removing && comp.Missing().Count > 0);

    public override Job? JobOnThing(Pawn pawn, Thing thing, bool forced = false)
    {
        if (thing.TryGetComp<CompPhobosBuildingUpgrades>() is not { } comp || thing.IsForbidden(pawn) || !pawn.CanReach(thing, PathEndMode.Touch, Danger.Some)) return null;
        foreach ((ThingDef what, int count) in comp.Missing())
        {
            Thing? found = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(what), PathEndMode.ClosestTouch,
                TraverseParms.For(pawn), 9999f, x => !x.IsForbidden(pawn) && pawn.CanReserve(x));
            if (found == null) continue;
            Job job = JobMaker.MakeJob(UpgradeJobDefOf.PhobosFramework_DeliverUpgradeMaterials, found, thing);
            job.count = System.Math.Min(count, found.stackCount);
            return job;
        }
        JobFailReason.Is("PhobosUpgrade_NoMaterial".Translate(comp.Missing()[0].what.label.Named("MATERIAL")));
        return null;
    }
}

/// <summary>Fetches one stack and puts it into the upgrade's order at the building.</summary>
public class JobDriver_PhobosDeliverUpgradeMaterials : JobDriver
{
    const TargetIndex Material = TargetIndex.A;
    const TargetIndex Building = TargetIndex.B;

    CompPhobosBuildingUpgrades? Comp => job.GetTarget(Building).Thing?.TryGetComp<CompPhobosBuildingUpgrades>();

    public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.GetTarget(Material), job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(Building);
        this.FailOn(() => Comp is not { Ordered: not null, Removing: false });
        yield return Toils_Goto.GotoThing(Material, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(Material).FailOnSomeonePhysicallyInteracting(Material);
        yield return Toils_Haul.StartCarryThing(Material, putRemainderInQueue: false, subtractNumTakenFromJobCount: true);
        yield return Toils_Goto.GotoThing(Building, PathEndMode.Touch);
        Toil deliver = ToilMaker.MakeToil("PhobosUpgradeDeliver");
        deliver.initAction = () =>
        {
            if (Comp is { } comp) BuildingUpgrades.Deliver(pawn, comp);
        };
        deliver.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return deliver;
    }
}

/// <summary>Construction work: fits an upgrade whose materials are all brought, or takes one off, at the building.</summary>
public class WorkGiver_PhobosFitBuildingUpgrade : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Undefined);

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => !Ready(pawn).Any();

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => Ready(pawn).Select(comp => (Thing)comp.parent);

    static IEnumerable<CompPhobosBuildingUpgrades> Ready(Pawn pawn) => BuildingUpgrades.WithOrders(pawn.Map, pawn.Faction).Where(comp => comp.ReadyForWork);

    public override Job? JobOnThing(Pawn pawn, Thing thing, bool forced = false)
    {
        if (thing.TryGetComp<CompPhobosBuildingUpgrades>() is not { ReadyForWork: true } comp || thing.IsForbidden(pawn)) return null;
        int needed = comp.Ordered!.constructionSkill;
        if (needed > 0 && (pawn.skills?.GetSkill(SkillDefOf.Construction).Level ?? 0) < needed)
        {
            JobFailReason.Is("PhobosUpgrade_SkillTooLow".Translate(needed.Named("LEVEL")));
            return null;
        }
        if (!pawn.CanReserve(thing, 1, -1, null, forced) || !pawn.CanReach(thing, PathEndMode.Touch, Danger.Some)) return null;
        return JobMaker.MakeJob(UpgradeJobDefOf.PhobosFramework_FitBuildingUpgrade, thing);
    }
}

/// <summary>Works the order at the building, at the colonist's construction speed, as the game's builders finish a frame.</summary>
public class JobDriver_PhobosFitBuildingUpgrade : JobDriver
{
    /// <summary>The game's own multiplier from construction speed to frame work each tick (JobDriver_ConstructFinishFrame).</summary>
    const float WorkPerSpeed = 1.7f;
    /// <summary>Construction experience a tick of this work gives, as the game's frame work does.</summary>
    const float LearnPerTick = 0.25f;

    CompPhobosBuildingUpgrades? Comp => TargetA.Thing?.TryGetComp<CompPhobosBuildingUpgrades>();

    /// <summary>The work finished this job: the order is gone then, and that is no failure.</summary>
    bool finished;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOn(() => !finished && Comp is not { ReadyForWork: true });
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        Toil work = ToilMaker.MakeToil("PhobosUpgradeWork");
        work.tickIntervalAction = delta =>
        {
            if (Comp is not { } comp) return;
            float amount = work.actor.GetStatValue(StatDefOf.ConstructionSpeed) * WorkPerSpeed * delta;
            work.actor.skills?.Learn(SkillDefOf.Construction, LearnPerTick * delta);
            if (!BuildingUpgrades.Work(work.actor, comp, amount)) return;
            finished = true;
            ReadyForNextToil();
        };
        work.defaultCompleteMode = ToilCompleteMode.Never;
        work.WithEffect(EffecterDefOf.ConstructMetal, TargetIndex.A);
        work.WithProgressBar(TargetIndex.A, () => Comp?.Progress ?? 1f);
        work.activeSkill = () => SkillDefOf.Construction;
        yield return work;
    }
}
