using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using Phobos.Framework.TopUp;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Framework.Recall;

/// <summary>
/// The service behind drones that come home in time and wrecks taken home (Framework 0.240.0; docs/development/drone-recall-design.md;
/// owner, 23 Nov 2026: "go for 1, 2 and 3"). In the owner's save every drone platform with automatic deployment on was nearly empty, and
/// 58 drone wrecks lay about the map: a drone that walks home gives its platform its cost back, one whose power cell runs flat first gives
/// nothing and leaves a wreck, and the drones were called home at a fixed two hours of power whatever the distance. So: each drone is
/// called home once its power barely covers the walk, priced by the path finder; and a hauler carries a wreck home, where the platform
/// takes back what the wreck salvages to. Which drones is said in <see cref="PhobosRecallDef"/>s. Nothing is saved; the walk prices are
/// a cache, dropped as they age.
/// </summary>
public static class Recalls
{
    /// <summary>A pawn Def carrying a named comp, with the Def that names it.</summary>
    readonly struct Kind
    {
        public readonly ThingDef Def;
        public readonly PhobosRecallDef Recall;
        public readonly Type CompType;

        public Kind(ThingDef def, PhobosRecallDef recall, Type compType)
        {
            Def = def;
            Recall = recall;
            CompType = compType;
        }
    }

    /// <summary>How often the drones are looked at, in ticks: a power cell loses no more than this between looks.</summary>
    public const int LookInterval = 250;

    /// <summary>How long a priced walk is trusted, in ticks, before the path finder is asked again.</summary>
    const int PriceKeptTicks = 1000;

    static Dictionary<ThingDef, Kind>? kinds;
    static readonly HashSet<PhobosRecallDef> reported = new();
    static readonly Dictionary<int, (int Tick, float Cost)> prices = new();

    static Dictionary<ThingDef, Kind> Kinds
    {
        get
        {
            if (kinds != null) return kinds;
            kinds = new Dictionary<ThingDef, Kind>();
            foreach (PhobosRecallDef recall in DefDatabase<PhobosRecallDef>.AllDefsListForReading)
            {
                Type? type = recall.CompType;
                if (type == null)
                {
                    ReportProblem(recall);
                    continue;
                }
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    if (def.race == null || def.comps == null || kinds.ContainsKey(def)) continue;
                    if (def.comps.Any(c => c.compClass != null && type.IsAssignableFrom(c.compClass))) kinds[def] = new Kind(def, recall, type);
                }
            }
            return kinds;
        }
    }

    /// <summary>How many kinds of drone are named, for the load line.</summary>
    public static int KindCount => Kinds.Count;

    /// <summary>The Defs that stand down, with why, for the load line.</summary>
    public static IEnumerable<PhobosRecallDef> Problems => DefDatabase<PhobosRecallDef>.AllDefsListForReading.Where(d => d.problem != null);

    /// <summary>Says once, in the log, that a Def's names no longer fit the code they name.</summary>
    public static void ReportProblem(PhobosRecallDef recall)
    {
        if (recall.problem == null || !reported.Add(recall)) return;
        FrameworkMod.Logger.Warn("Drones called home and wrecks taken home stand down for " + recall.defName + ": " + recall.problem
            + ". The mod it names may have changed; its drones work as that mod alone makes them until the Def is put right.");
    }

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

    // ---- Called home in time ----

    /// <summary>Looks at the player's drones on every map, and calls home each whose power barely covers the walk. Every
    /// <see cref="LookInterval"/> ticks, from <see cref="GameComponent_PhobosRecall"/>.</summary>
    public static void LookAtDrones()
    {
        if (!FrameworkMod.Settings.recallInTime || Kinds.Count == 0) return;
        int now = Find.TickManager.TicksGame;
        List<Map> maps = Find.Maps;
        for (int m = 0; m < maps.Count; m++)
        {
            List<Pawn> pawns = maps[m].mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn drone = pawns[i];
                if (Kinds.TryGetValue(drone.def, out Kind kind) && kind.Recall.recall) LookAt(drone, kind, now);
            }
        }
        if (now % (PriceKeptTicks * 10) < LookInterval) Prune(now);
    }

    static void LookAt(Pawn drone, Kind kind, int now)
    {
        // A drafted drone is the player's to steer; a downed or dead one cannot walk.
        if (drone.Dead || drone.Downed || drone.Drafted) return;
        if (drone.GetComp<CompMechPowerCell>() is not { } cell || cell.depleted) return;
        if (CompOf(drone, kind) is not { } comp) return;
        if (kind.Recall.ReturningJob(comp) is { } going && drone.CurJobDef == going) return;
        if (kind.Recall.HomeOf(comp) is not { Spawned: true } home || home.Map != drone.Map) return;
        int left = cell.PowerTicksLeft;
        double straight = drone.Position.DistanceTo(home.Position) * drone.TicksPerMoveCardinal;
        if (!RecallRule.WorthPricing(left, straight, kind.Recall.dockTicks)) return;
        double walk = WalkPrice(drone, home, now, straight);
        if (!RecallRule.CallHome(left, walk, kind.Recall.dockTicks)) return;
        if (!kind.Recall.CallHome(comp)) return;
        prices.Remove(drone.thingIDNumber);
        if (FrameworkMod.Logger.DebugEnabled)
        {
            FrameworkMod.Logger.Debug(drone.LabelShort + " (" + drone.ThingID + ") called home to " + home.LabelShort + ": " + left
                + " ticks of power left, walk priced at " + (int)walk + ", needs " + RecallRule.Needed(walk, kind.Recall.dockTicks));
        }
        if (Seen.Wants("framework.recall-home", drone))
        {
            Seen.Note("framework.recall-home", drone, left + " ticks of power left, " + (int)walk + " ticks' walk to " + home.LabelShort);
        }
    }

    /// <summary>The walk home in ticks: the path finder's price, kept a while; a stretched straight line where no path is found.</summary>
    static double WalkPrice(Pawn drone, Thing home, int now, double straight)
    {
        if (prices.TryGetValue(drone.thingIDNumber, out var kept) && now - kept.Tick < PriceKeptTicks) return kept.Cost;
        float cost;
        using (PawnPath path = drone.Map.pathFinder.FindPathNow(drone.Position, home, drone, null, PathEndMode.Touch))
        {
            cost = path != null && path.Found ? path.TotalCost : (float)(straight * RecallRule.RoughStretch);
        }
        prices[drone.thingIDNumber] = (now, cost);
        return cost;
    }

    static void Prune(int now)
    {
        foreach (int id in prices.Where(p => now - p.Value.Tick >= PriceKeptTicks).Select(p => p.Key).ToList()) prices.Remove(id);
    }

    // ---- Wrecks taken home ----

    /// <summary>Whether hauling wrecks home runs at all: the player's setting, and at least one kind that asks for it.</summary>
    public static bool ReclaimActive => FrameworkMod.Settings.reclaimWrecks && Kinds.Values.Any(k => k.Recall.reclaimWrecks);

    /// <summary>The wrecks of named drones lying on a map, not forbidden to the pawn.</summary>
    public static IEnumerable<Thing> Wrecks(Pawn pawn)
    {
        List<Thing> corpses = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
        for (int i = 0; i < corpses.Count; i++)
        {
            if (corpses[i] is Corpse corpse && corpse.InnerPawn is { } dead && Kinds.TryGetValue(dead.def, out Kind kind) && kind.Recall.reclaimWrecks
                && !corpse.IsForbidden(pawn))
            {
                yield return corpse;
            }
        }
    }

    /// <summary>What a wreck salvages to, in the store's material: the drone's own butchering yield of it (the drone mod's figure).</summary>
    public static int SalvageOf(Corpse corpse, ThingDef material)
    {
        List<ThingDefCountClass>? products = corpse.InnerPawn?.def.butcherProducts;
        if (products == null) return 0;
        int total = 0;
        for (int i = 0; i < products.Count; i++)
        {
            if (products[i].thingDef == material) total += products[i].count;
        }
        return total;
    }

    /// <summary>
    /// Where a wreck goes: its own home, if that is a building of the pawn's faction on the map whose store holds a material the wreck
    /// salvages to and has room; otherwise the nearest such building the pawn can reach. Null when there is none.
    /// </summary>
    public static Thing? HomeForWreck(Pawn pawn, Corpse corpse)
    {
        if (corpse.InnerPawn is not { } dead || !Kinds.TryGetValue(dead.def, out Kind kind)) return null;
        if (CompOf(dead, kind) is { } comp && kind.Recall.HomeOf(comp) is Building own && Takes(pawn, own, corpse)) return own;
        Thing? best = null;
        float bestDistance = float.MaxValue;
        foreach (Thing store in TopUps.BuildingStores(pawn.Map, pawn.Faction))
        {
            float distance = store.Position.DistanceToSquared(corpse.Position);
            if (distance >= bestDistance || !Takes(pawn, store, corpse)) continue;
            best = store;
            bestDistance = distance;
        }
        return best;
    }

    static bool Takes(Pawn pawn, Thing store, Corpse corpse) =>
        store.Spawned && store.Map == pawn.Map && store.Faction == pawn.Faction && !store.IsForbidden(pawn) && !store.IsBurning()
        && TopUps.MaterialOf(store) is { } material && SalvageOf(corpse, material) > 0 && TopUps.RoomIn(store) != 0
        && pawn.CanReach(store, PathEndMode.Touch, Danger.Deadly);

    /// <summary>The job that carries a wreck home, or null.</summary>
    public static Job? WreckJob(Pawn pawn, Thing thing, bool forced)
    {
        if (!ReclaimActive || thing is not Corpse corpse || !corpse.Spawned || corpse.IsForbidden(pawn)) return null;
        if (!pawn.CanReserveAndReach(corpse, PathEndMode.ClosestTouch, Danger.Deadly, 1, -1, null, forced)) return null;
        if (HomeForWreck(pawn, corpse) is not { } home) return null;
        Job job = JobMaker.MakeJob(RecallDefOf.PhobosFramework_ReclaimWreck, corpse, home);
        job.count = 1;
        return job;
    }

    /// <summary>The job's last step: the home takes the wreck in, its salvage into the store as far as there is room and the rest beside
    /// it, and the wreck is gone. Returns how much went into the store.</summary>
    public static int TakeWreckIn(Pawn pawn, Thing home)
    {
        if (pawn.carryTracker.CarriedThing is not Corpse corpse) return 0;
        ThingDef? material = home.Spawned ? TopUps.MaterialOf(home) : null;
        int salvage = material != null ? SalvageOf(corpse, material) : 0;
        if (material == null || salvage <= 0)
        {
            pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
            return 0;
        }
        string wreck = corpse.InnerPawn?.LabelShort ?? corpse.LabelShort;
        pawn.carryTracker.DestroyCarriedThing();
        int stored = TopUps.TakeIn(home, material, salvage);
        if (FrameworkMod.Logger.DebugEnabled)
        {
            FrameworkMod.Logger.Debug(pawn.LabelShort + " took the wreck of " + wreck + " to " + home.LabelShort + " (" + home.ThingID + "): "
                + salvage + " " + material.defName + ", " + stored + " into its store");
        }
        if (Seen.Wants("framework.reclaim-wreck", home))
        {
            Seen.Note("framework.reclaim-wreck", home, wreck + ": " + salvage + " " + material.label + ", " + stored + " into the store");
        }
        return stored;
    }
}

[DefOf]
public static class RecallDefOf
{
    public static JobDef PhobosFramework_ReclaimWreck = null!;

    static RecallDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RecallDefOf));
}

/// <summary>Looks at the player's drones every <see cref="Recalls.LookInterval"/> ticks (Framework 0.240.0). Saves nothing.</summary>
public sealed class GameComponent_PhobosRecall : GameComponent
{
    public GameComponent_PhobosRecall(Game game)
    {
    }

    public override void GameComponentTick()
    {
        if (Find.TickManager.TicksGame % Recalls.LookInterval != 0) return;
        Recalls.LookAtDrones();
    }
}

/// <summary>Hauling work (Framework 0.240.0): carries the wrecks of named drones home, where the home takes back their salvage.</summary>
public class WorkGiver_PhobosReclaimWreck : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Undefined);

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => !Recalls.ReclaimActive || !Recalls.Wrecks(pawn).Any();

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => Recalls.Wrecks(pawn);

    public override Job? JobOnThing(Pawn pawn, Thing thing, bool forced = false) => Recalls.WreckJob(pawn, thing, forced);
}

/// <summary>Carries a drone's wreck to a home that holds its material, which takes it in (Framework 0.240.0). The home is not held, as a
/// drone walking home holds it.</summary>
public class JobDriver_PhobosReclaimWreck : JobDriver
{
    const TargetIndex Wreck = TargetIndex.A;
    const TargetIndex Home = TargetIndex.B;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.GetTarget(Wreck), job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(Home);
        this.FailOnDespawnedOrNull(Home);
        this.FailOnForbidden(Home);
        this.FailOnBurningImmobile(Home);
        yield return Toils_Goto.GotoThing(Wreck, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(Wreck).FailOnSomeonePhysicallyInteracting(Wreck);
        yield return Toils_Haul.StartCarryThing(Wreck);
        yield return Toils_Goto.GotoThing(Home, PathEndMode.Touch);
        Toil takeIn = ToilMaker.MakeToil("PhobosReclaimWreckTakeIn");
        takeIn.initAction = () => Recalls.TakeWreckIn(pawn, job.GetTarget(Home).Thing);
        takeIn.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return takeIn;
    }
}
