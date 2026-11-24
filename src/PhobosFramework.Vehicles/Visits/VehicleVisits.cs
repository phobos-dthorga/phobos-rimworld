using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// A reason to visit a parked vehicle (Framework 0.289.0; docs/development/vehicle-parked-use.md, PV0): the walk-and-climb job that brings
/// someone, how long they may stay, and the worker that says what the vehicle offers them and when they are done. Another mod adds a reason
/// with a Def and a worker of its own.
/// </summary>
public class PhobosVehicleVisitDef : Def
{
    /// <summary>The job that walks the visitor over and climbs them in (its report string is what the player reads).</summary>
    public JobDef? jobDef;
    /// <summary>The longest a visit lasts, in hours; 0 for as long as its reason holds (a warm-up).</summary>
    public float maxHours;
    /// <summary>The worker: what the vehicle offers, and when the visitor is done.</summary>
    public Type workerClass = typeof(VisitWorker);
    /// <summary>The vehicle's logbook line for the first such visit of the day (Framework 0.292.0); none when left out.</summary>
    public Phobos.Framework.Persistence.PhobosLogEntryDef? logEntry;

    [Unsaved] VisitWorker? worker;

    public VisitWorker Worker
    {
        get
        {
            if (worker != null) return worker;
            worker = (VisitWorker)Activator.CreateInstance(workerClass)!;
            worker.def = this;
            return worker;
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (jobDef == null) yield return "has no jobDef";
        if (workerClass == null || !typeof(VisitWorker).IsAssignableFrom(workerClass)) yield return "workerClass is not a VisitWorker";
    }
}

/// <summary>What a visit's reason asks of a vehicle and of the visitor. The base offers nothing; each reason overrides it.</summary>
public class VisitWorker
{
    public PhobosVehicleVisitDef def = null!;

    /// <summary>Whether this vehicle offers this person what they come for now (cheap: read on every recreation choice).</summary>
    public virtual bool Offers(CompPhobosVehicleSystems comp, Pawn pawn) => false;

    /// <summary>Null to stay; "" to climb out quietly, their reason spent; or a Keyed message key ({PAWN}, {VEHICLE}) to say why.</summary>
    public virtual string? Done(CompPhobosVehicleSystems comp, VehicleVisit visit) => null;

    /// <summary>Whether the visitor is asleep in the seat: they rest each tick and take no part in life aboard.</summary>
    public virtual bool Asleep => false;
}

/// <summary>One person visiting one vehicle. Saved, so a visit loaded from a save still ends.</summary>
public sealed class VehicleVisit : IExposable
{
    public Pawn pawn = null!;
    public VehiclePawn vehicle = null!;
    public PhobosVehicleVisitDef def = null!;
    public int since;
    /// <summary>A hotbox visitor's smoke, the tick they lit up; -1 until they do.</summary>
    public int smokedAt = -1;

    public void ExposeData()
    {
        Scribe_References.Look(ref pawn, "PhobosVisit_Pawn");
        Scribe_References.Look(ref vehicle, "PhobosVisit_Vehicle");
        Scribe_Defs.Look(ref def, "PhobosVisit_Def");
        Scribe_Values.Look(ref since, "PhobosVisit_Since");
        Scribe_Values.Look(ref smokedAt, "PhobosVisit_SmokedAt", -1);
    }
}

/// <summary>
/// Everyone visiting a vehicle now (Framework 0.289.0). Each tick it rests the nappers, as the game rests a sleeper only while it is told so
/// every tick; every 250 ticks it asks each visit whether to end. Empty in a colony nobody visits: one count a tick.
/// </summary>
public class GameComponent_PhobosVehicleVisits : GameComponent
{
    internal List<VehicleVisit> visits = new();

    public GameComponent_PhobosVehicleVisits(Game game)
    {
    }

    /// <summary>Set by a load: the visits are checked against who is aboard at the first tick.</summary>
    bool checkAfterLoad;

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref visits, "PhobosVisits_List", LookMode.Deep);
        if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
        visits ??= new List<VehicleVisit>();
        visits.RemoveAll(v => v.pawn == null || v.vehicle == null || v.def == null);
        // Who is aboard is known only once Vehicle Framework's own load step has run, after this one (fault-read F1): before it, every
        // vehicle reads as empty and every visit was dropped at each load. So the check waits for the first tick.
        checkAfterLoad = visits.Count > 0;
    }

    public override void GameComponentTick()
    {
        if (visits.Count == 0) return;
        if (checkAfterLoad)
        {
            checkAfterLoad = false;
            // A visit whose person is no longer in a passenger seat of its vehicle is dropped: they ride on, if at all, as any rider would.
            visits.RemoveAll(v => !VehicleVisits.StillVisiting(v));
        }
        VehicleVisits.Rest(visits);
        if (Find.TickManager.TicksGame % VehicleVisits.LookTicks == 0) VehicleVisits.Look(this);
    }
}

/// <summary>
/// People use parked vehicles (Framework 0.289.0; docs/development/vehicle-parked-use.md): the service every visit, fallback and recreation
/// choice goes through. A colonist walks to one of the colony's parked vehicles, climbs into a free passenger seat (as the cab bunk's sleeper
/// does), stays while their reason holds, and climbs out by themselves; they climb out for danger, fire, illness, the vehicle setting off,
/// or the setting being turned off. People the player seated are never visitors and never put out.
/// </summary>
public static class VehicleVisits
{
    public const int LookTicks = 250;
    /// <summary>The electrical system's starting power, whose line a visitor's radio keeps clear of.</summary>
    public const string StartingPower = "startingPower";

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    public static bool Enabled => VehicleSystemsMod.Settings.parkedVehicleUse;

    static GameComponent_PhobosVehicleVisits? Component => Current.Game?.GetComponent<GameComponent_PhobosVehicleVisits>();

    // ---- Who is visiting ----

    /// <summary>This person's visit, or null.</summary>
    public static VehicleVisit? VisitOf(Pawn pawn)
    {
        if (Component is not { } component) return null;
        List<VehicleVisit> visits = component.visits;
        for (int i = 0; i < visits.Count; i++)
        {
            if (visits[i].pawn == pawn) return visits[i];
        }
        return null;
    }

    /// <summary>Whether this vehicle has a visitor here for this reason now.</summary>
    public static bool Hosts(VehiclePawn vehicle, PhobosVehicleVisitDef def)
    {
        if (Component is not { } component) return false;
        foreach (VehicleVisit visit in component.visits)
        {
            if (visit.vehicle == vehicle && visit.def == def) return true;
        }
        return false;
    }

    /// <summary>
    /// Whether someone aboard is asleep: napping in a seat, or in the cab bunk. The game counts a rider with no job as awake, so life aboard
    /// asks here before giving a sleeper a book, a treat or a smoke.
    /// </summary>
    public static bool AsleepAboard(Pawn pawn) =>
        VisitOf(pawn) is { def.Worker.Asleep: true } || BunkSleepers.BunkOf(pawn) is { SleeperAsleep: true };

    // ---- Which vehicles ----

    static readonly Dictionary<Map, (int tick, List<CompPhobosVehicleSystems> comps)> candidates = new();
    static Game? candidatesGame;

    /// <summary>The colony's parked vehicles on this map with our systems: a short list, read again every 250 ticks.</summary>
    public static List<CompPhobosVehicleSystems> Candidates(Map map)
    {
        if (candidatesGame != Current.Game)
        {
            candidates.Clear();
            candidatesGame = Current.Game;
        }
        int now = Find.TickManager.TicksGame;
        if (candidates.TryGetValue(map, out var known) && now - known.tick < LookTicks) return known.comps;
        var comps = new List<CompPhobosVehicleSystems>();
        foreach (Pawn pawn in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
        {
            if (pawn is VehiclePawn vehicle && vehicle.GetComp<CompPhobosVehicleSystems>() is { Usable: true } comp) comps.Add(comp);
        }
        candidates[map] = (now, comps);
        return comps;
    }

    /// <summary>
    /// A passenger seat with room that this person may take: never a driver's or a gunner's (read after upgrades: the Osprey's crew chief seat
    /// becomes a gun seat), never one another has reserved, and never a role they cannot fill (fault-read F6).
    /// </summary>
    static VehicleRoleHandler? FreeSeat(VehiclePawn vehicle, Pawn pawn) =>
        vehicle.handlers?.FirstOrDefault(h => h.role != null && h.role.HandlingTypes == HandlingType.None && h.AreSlotsAvailableAndReservable && h.CanOperateRole(pawn));

    /// <summary>Whether this visitor still sits in a passenger seat of their vehicle: the player has not moved them, nor taken them out.</summary>
    internal static bool StillVisiting(VehicleVisit visit)
    {
        if (visit.pawn == null || visit.vehicle?.handlers == null || visit.pawn.Dead) return false;
        foreach (VehicleRoleHandler handler in visit.vehicle.handlers)
        {
            if (handler.role != null && handler.role.HandlingTypes == HandlingType.None && handler.thingOwner != null && handler.thingOwner.Contains(visit.pawn)) return true;
        }
        return false;
    }

    /// <summary>
    /// Whether this person may use this vehicle now: the setting on, the vehicle open to the colony, parked here, not setting off, flying or
    /// burning, and within reach; climbing in also needs a free passenger seat. Nobody is told: no order is refused, the person simply
    /// chooses something else. Cheap checks first; the path last.
    /// </summary>
    public static bool MayUse(CompPhobosVehicleSystems comp, Pawn pawn, bool climbIn)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (!Enabled || !comp.OpenToColony) return false;
        if (!vehicle.Spawned || vehicle.Map != pawn.Map || vehicle.Faction != Faction.OfPlayer) return false;
        if (vehicle.Drafted || vehicle.vehiclePather is { Moving: true } || OnMapFlight.Airborne(vehicle)) return false;
        if (Phobos.Framework.Environment.AttachedFires.OnFire(vehicle)) return false;
        if (climbIn && FreeSeat(vehicle, pawn) == null) return false;
        return pawn.CanReach(vehicle, PathEndMode.Touch, Danger.Some);
    }

    /// <summary>Whether this person is one who may visit by themselves: a colonist (slaves included), awake and in their right mind.</summary>
    public static bool MayVisit(Pawn pawn) =>
        Enabled && pawn.Spawned && pawn.IsColonist && !pawn.Downed && !pawn.InMentalState && !pawn.Drafted && pawn.Map != null;

    /// <summary>
    /// The nearest of the colony's parked vehicles that offers this person what <paramref name="offers"/> asks and that they may use now,
    /// within the rules' reach. Null when there is none, or danger is on the map.
    /// </summary>
    public static CompPhobosVehicleSystems? Nearest(Pawn pawn, Func<CompPhobosVehicleSystems, bool> offers, bool climbIn)
    {
        if (!MayVisit(pawn)) return null;
        List<CompPhobosVehicleSystems> comps = Candidates(pawn.Map);
        if (comps.Count == 0) return null;
        if (GenHostility.AnyHostileActiveThreatToPlayer(pawn.Map, countDormantPawnsAsHostile: false)) return null;
        float reach = Rules.visitReachCells;
        CompPhobosVehicleSystems? best = null;
        float bestDistance = float.MaxValue;
        foreach (CompPhobosVehicleSystems comp in comps)
        {
            float distance = comp.Vehicle.Position.DistanceTo(pawn.Position);
            if (distance > reach || distance >= bestDistance || !offers(comp)) continue;
            if (!MayUse(comp, pawn, climbIn)) continue;
            best = comp;
            bestDistance = distance;
        }
        return best;
    }

    /// <summary>Whether any parked vehicle on this person's map offers them this reason now, without the path check (a recreation chance).</summary>
    public static bool AnyOffers(Pawn pawn, PhobosVehicleVisitDef def)
    {
        if (!MayVisit(pawn) || UnwellFor(pawn, def)) return false;
        float reach = Rules.visitReachCells;
        foreach (CompPhobosVehicleSystems comp in Candidates(pawn.Map))
        {
            if (!comp.OpenToColony || comp.Vehicle.Drafted || FreeSeat(comp.Vehicle, pawn) == null) continue;
            if (comp.Vehicle.Position.DistanceTo(pawn.Position) > reach) continue;
            if (def.Worker.Offers(comp, pawn)) return true;
        }
        return false;
    }

    /// <summary>The job that takes this person to the nearest vehicle offering this reason, or null.</summary>
    public static Job? JobFor(Pawn pawn, PhobosVehicleVisitDef def)
    {
        if (def.jobDef == null || UnwellFor(pawn, def)) return null;
        CompPhobosVehicleSystems? comp = Nearest(pawn, c => def.Worker.Offers(c, pawn), climbIn: true);
        return comp == null ? null : JobMaker.MakeJob(def.jobDef, comp.Vehicle);
    }

    /// <summary>
    /// Whether this person should rest in a bed for their health, so a visit would only put them straight out again (fault-read F2). Never
    /// for a warm-up: the cold is what hurts them, and a cab beats the weather.
    /// </summary>
    static bool UnwellFor(Pawn pawn, PhobosVehicleVisitDef def) =>
        def != VehicleVisitDefOf.PhobosVisit_Warm && HealthAIUtility.ShouldSeekMedicalRest(pawn);

    static Dictionary<JobDef, PhobosVehicleVisitDef>? byJob;

    /// <summary>The visit a walk-and-climb job is for.</summary>
    public static PhobosVehicleVisitDef? DefForJob(JobDef job)
    {
        byJob ??= DefDatabase<PhobosVehicleVisitDef>.AllDefsListForReading.Where(d => d.jobDef != null).ToDictionary(d => d.jobDef!, d => d);
        return byJob.TryGetValue(job, out PhobosVehicleVisitDef? def) ? def : null;
    }

    /// <summary>
    /// Opens a vehicle to the colony, or keeps its stores for the road (the vehicle's switch). Closing it puts its visitors out at the next
    /// look. The player is told what changed.
    /// </summary>
    public static void SetOpen(CompPhobosVehicleSystems comp, bool open)
    {
        comp.OpenToColony = open;
        NamedArgument name = comp.Vehicle.LabelShort.Named("VEHICLE");
        string said = open ? "PhobosVisit_Opened".Translate(name).Resolve() : "PhobosVisit_Closed".Translate(name).Resolve();
        Messages.Message(said, comp.Vehicle, MessageTypeDefOf.SilentInput, historical: false);
    }

    // ---- In and out ----

    /// <summary>The visitor has reached the vehicle: aboard in a free passenger seat, their visit begun. False, quietly, when it can no longer take them.</summary>
    public static bool Begin(Pawn pawn, VehiclePawn vehicle, PhobosVehicleVisitDef def)
    {
        if (Component is not { } component || vehicle.GetComp<CompPhobosVehicleSystems>() is not { } comp) return false;
        if (!MayUse(comp, pawn, climbIn: true) || !def.Worker.Offers(comp, pawn)) return false;
        bool friendAboard = def == VehicleVisitDefOf.PhobosVisit_Hotbox && FriendAboard(pawn, vehicle);
        if (FreeSeat(vehicle, pawn) is not { } seat) return false;
        VisitSeats.seatingVisitor = true;
        try
        {
            if (!vehicle.TryAddPawn(pawn, seat)) return false;
        }
        finally
        {
            VisitSeats.seatingVisitor = false;
        }
        component.visits.Add(new VehicleVisit { pawn = pawn, vehicle = vehicle, def = def, since = Find.TickManager.TicksGame });
        if (def.logEntry != null) LogFirstToday(vehicle, def.logEntry, pawn);
        if (Seen.Wants("vehicles.visit", vehicle, def.defName)) Seen.Note("vehicles.visit", vehicle, pawn.LabelShort, def.defName);
        if (friendAboard && Seen.Wants("vehicles.visit-join", vehicle)) Seen.Note("vehicles.visit-join", vehicle, pawn.LabelShort);
        return true;
    }

    /// <summary>Ends a visit: the visitor climbs out beside the vehicle; with a message key, the player is told why.</summary>
    static void End(GameComponent_PhobosVehicleVisits component, VehicleVisit visit, string reasonKey)
    {
        component.visits.Remove(visit);
        Pawn pawn = visit.pawn;
        VehiclePawn vehicle = visit.vehicle;
        if (pawn == null || vehicle == null || !vehicle.AllPawnsAboard.Contains(pawn)) return;
        if (vehicle.Spawned) vehicle.DisembarkPawn(pawn);
        int now = Find.TickManager.TicksGame;
        if (visit.def.Worker.Asleep && VisitRule.NapRemembered(visit.since, now, Rules.napMemoryHours, GenDate.TicksPerHour)
            && pawn.needs?.mood?.thoughts?.memories is { } memories)
        {
            memories.TryGainMemory(VehicleVisitDefOf.PhobosSleptSittingUp);
        }
        string variant = reasonKey.Length == 0 ? "done" : reasonKey;
        if (Seen.Wants("vehicles.visit-end", vehicle, variant)) Seen.Note("vehicles.visit-end", vehicle, pawn.LabelShort + " (" + visit.def.defName + ")", variant);
        if (reasonKey.Length == 0 || pawn.Faction != Faction.OfPlayer) return;
        Messages.Message(reasonKey.Translate(pawn.LabelShort.Named("PAWN"), vehicle.LabelShort.Named("VEHICLE")).Resolve(), pawn,
            reasonKey == "PhobosVisit_OutDanger" || reasonKey == "PhobosVisit_OutFire" ? MessageTypeDefOf.NegativeEvent : MessageTypeDefOf.SilentInput,
            historical: false);
    }

    /// <summary>Whether anyone visits this vehicle now (one count when nobody visits anything).</summary>
    public static bool AnyVisitorIn(VehiclePawn vehicle)
    {
        if (Component is not { } component || component.visits.Count == 0) return false;
        foreach (VehicleVisit visit in component.visits)
        {
            if (visit.vehicle == vehicle) return true;
        }
        return false;
    }

    /// <summary>Someone else is being seated in this full passenger seat: a visitor in it climbs out first, and the player is told.</summary>
    public static void MakeRoom(VehiclePawn vehicle, VehicleRoleHandler handler)
    {
        if (Component is not { } component) return;
        foreach (VehicleVisit visit in component.visits.ToList())
        {
            if (visit.vehicle != vehicle || handler.thingOwner == null || !handler.thingOwner.Contains(visit.pawn)) continue;
            End(component, visit, "PhobosVisit_OutMakeRoom");
            return;
        }
    }

    /// <summary>The vehicle is setting off (drafted or moving, Vehicle Framework's events): every visitor climbs out first, and the player is told.</summary>
    public static void SettingOff(VehiclePawn vehicle)
    {
        if (Component is not { } component || component.visits.Count == 0) return;
        foreach (VehicleVisit visit in component.visits.Where(v => v.vehicle == vehicle).ToList())
        {
            // Someone the player has moved into a driving or gun seat is theirs now, never put out (fault-read F8).
            if (StillVisiting(visit)) End(component, visit, "PhobosVisit_OutMoving");
            else component.visits.Remove(visit);
        }
    }

    /// <summary>Someone left the vehicle (Vehicle Framework's event): a visit whose person is gone ends there, so a quick return is no visit.</summary>
    public static void Exited(VehiclePawn vehicle)
    {
        if (Component is not { } component || component.visits.Count == 0) return;
        component.visits.RemoveAll(v => v.vehicle == vehicle && !StillVisiting(v));
    }

    /// <summary>The vehicle has left the map with visitors still aboard (a caravan formed at once): they ride on as riders, their visits over.</summary>
    public static void Left(VehiclePawn vehicle)
    {
        if (Component is not { } component || component.visits.Count == 0) return;
        component.visits.RemoveAll(v => v.vehicle == vehicle);
    }

    /// <summary>Every tick: nappers rest at the rules' seat figure while their vehicle stands still.</summary>
    internal static void Rest(List<VehicleVisit> visits)
    {
        for (int i = 0; i < visits.Count; i++)
        {
            VehicleVisit visit = visits[i];
            if (!visit.def.Worker.Asleep || visit.vehicle.vehiclePather is { Moving: true }) continue;
            visit.pawn.needs?.rest?.TickResting(Rules.napRestEffectiveness);
        }
    }

    /// <summary>Every 250 ticks: whether each visit ends, for the common reasons first and then its own.</summary>
    internal static void Look(GameComponent_PhobosVehicleVisits component)
    {
        foreach (VehicleVisit visit in component.visits.ToList())
        {
            Pawn pawn = visit.pawn;
            VehiclePawn vehicle = visit.vehicle;
            // Moved into another seat or taken out by the player: theirs now, never a visitor's (fault-read F8).
            if (pawn == null || vehicle == null || pawn.Destroyed || !StillVisiting(visit))
            {
                component.visits.Remove(visit);
                continue;
            }
            if (!vehicle.Spawned) continue;
            // Vehicle Framework puts everyone out of a vehicle idle for five hours (fault-read F3): a visit is its own reason to stay.
            CrewOnStation.Engaged(vehicle);
            if (vehicle.GetComp<CompPhobosVehicleSystems>() is not { } comp)
            {
                End(component, visit, "");
                continue;
            }
            string? why = CommonEnd(comp, visit) ?? visit.def.Worker.Done(comp, visit);
            if (why != null) End(component, visit, why);
        }
    }

    static string? CommonEnd(CompPhobosVehicleSystems comp, VehicleVisit visit)
    {
        Pawn pawn = visit.pawn;
        VehiclePawn vehicle = comp.Vehicle;
        if (!Enabled || !comp.OpenToColony) return "";
        if (GenHostility.AnyHostileActiveThreatToPlayer(vehicle.Map, countDormantPawnsAsHostile: false)) return "PhobosVisit_OutDanger";
        if (Phobos.Framework.Environment.AttachedFires.OnFire(vehicle)) return "PhobosVisit_OutFire";
        if (vehicle.Drafted || vehicle.vehiclePather is { Moving: true }) return "PhobosVisit_OutMoving";
        if (visit.def != VehicleVisitDefOf.PhobosVisit_Warm && HealthAIUtility.ShouldSeekMedicalRest(pawn)) return "PhobosVisit_OutUnwell";
        if (pawn.InMentalState) return "";
        if (VisitRule.OverTime(visit.since, Find.TickManager.TicksGame, visit.def.maxHours, GenDate.TicksPerHour)) return "";
        return null;
    }

    // ---- The logbook ----

    static readonly Dictionary<(VehiclePawn, Def), int> loggedAt = new();
    static Game? loggedGame;

    /// <summary>
    /// A line in the vehicle's logbook for the first visit of its kind there in a day (Framework 0.292.0), so the book tells the story without
    /// filling with every cuppa. Not saved: after a load, the next visit may write its line again.
    /// </summary>
    public static void LogFirstToday(VehiclePawn vehicle, Phobos.Framework.Persistence.PhobosLogEntryDef entry, Pawn pawn)
    {
        if (loggedGame != Current.Game)
        {
            loggedAt.Clear();
            loggedGame = Current.Game;
        }
        int now = Find.TickManager.TicksGame;
        if (loggedAt.TryGetValue((vehicle, entry), out int at) && now - at < GenDate.TicksPerDay) return;
        loggedAt[(vehicle, entry)] = now;
        VehicleStory.Of(vehicle, entry, pawn.LabelShort);
    }

    // ---- Shared by the reasons ----

    /// <summary>Whether a friend of this person (opinion at or above the rules' figure) sits in this vehicle on a hotbox visit.</summary>
    public static bool FriendAboard(Pawn pawn, VehiclePawn vehicle)
    {
        if (Component is not { } component || pawn.relations == null) return false;
        foreach (VehicleVisit visit in component.visits)
        {
            if (visit.vehicle != vehicle || visit.def != VehicleVisitDefOf.PhobosVisit_Hotbox || visit.pawn == pawn) continue;
            if (pawn.relations.OpinionOf(visit.pawn) >= Rules.hotboxFriendOpinion) return true;
        }
        return false;
    }

    /// <summary>Whether a friend sits in any vehicle on this person's map on a hotbox visit (the session's pull).</summary>
    public static bool FriendHotboxing(Pawn pawn)
    {
        if (Component is not { } component || component.visits.Count == 0 || pawn.relations == null) return false;
        foreach (VehicleVisit visit in component.visits)
        {
            if (visit.def != VehicleVisitDefOf.PhobosVisit_Hotbox || visit.pawn == pawn || visit.vehicle.MapHeld != pawn.Map) continue;
            if (pawn.relations.OpinionOf(visit.pawn) >= Rules.hotboxFriendOpinion) return true;
        }
        return false;
    }

    /// <summary>A hotbox visitor lit up (life aboard's smoke): their hour of sharing it starts now.</summary>
    public static void Smoked(Pawn pawn)
    {
        if (VisitOf(pawn) is { } visit && visit.def == VehicleVisitDefOf.PhobosVisit_Hotbox && visit.smokedAt < 0)
        {
            visit.smokedAt = Find.TickManager.TicksGame;
        }
    }

    /// <summary>Whether this vehicle has a visitor warming up in it (the night heater runs for them).</summary>
    public static bool WarmingUp(VehiclePawn vehicle) => Hosts(vehicle, VehicleVisitDefOf.PhobosVisit_Warm);

    /// <summary>Whether this comfortable range holds the outside air where this person stands.</summary>
    public static bool ComfortableOutside(Pawn pawn, IntVec3 at, Map map)
    {
        FloatRange range = pawn.ComfortableTemperatureRange();
        return VisitRule.Comfortable(GenTemperature.GetTemperatureForCell(at, map), range.min, range.max);
    }
}

[DefOf]
public static class VehicleVisitDefOf
{
    public static PhobosVehicleVisitDef PhobosVisit_Radio = null!;
    public static PhobosVehicleVisitDef PhobosVisit_Hotbox = null!;
    public static PhobosVehicleVisitDef PhobosVisit_Warm = null!;
    public static PhobosVehicleVisitDef PhobosVisit_Nap = null!;
    public static JobDef PhobosVehicleSystems_TakeFoodFromVehicle = null!;
    public static JobDef PhobosVehicleSystems_DrinkFromVehicle = null!;
    public static ThoughtDef PhobosSleptSittingUp = null!;
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_VisitFood = null!;

    static VehicleVisitDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VehicleVisitDefOf));
}
