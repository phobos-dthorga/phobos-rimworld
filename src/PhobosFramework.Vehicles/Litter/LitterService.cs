using System.Collections.Generic;
using Phobos.Framework;
using Phobos.Framework.Health;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// "Take casualties aboard" (Framework 0.126.0; the owner's idea of 17 Nov 2026): a vehicle with the litter kit, switched on by the player,
/// becomes where carriers bring the downed. Every decision is here; the toggle and the work giver only call it.
/// <list type="bullet">
/// <item>Who: one of the colony's people (prisoners and guests included) lying on the ground, whose care setting is not "no care".</item>
/// <item>Where: the nearest switched-on vehicle on the map that can take them, when it is nearer than their bed or they have no bed
/// (<see cref="LitterRule"/>). Litters already promised to a carrier on the way count as taken.</item>
/// <item>When: with field medics about (Search and Rescue), a patient bleeding towards death waits for them to be treated first; without,
/// they are carried at once, as the game's own rescue does, and a doctor in the crew tends them aboard.</item>
/// </list>
/// Agent defaults, for the owner to revise. With every toggle off the work giver is skipped, so this costs nothing.
/// </summary>
public static class LitterService
{
    const int CacheIntervalTicks = 250;

    /// <summary>Litter vehicles switched on and standing on a map. Derived: rebuilt from each vehicle's saved flag as it spawns.</summary>
    static readonly HashSet<CompPhobosLitter> collecting = new();
    static readonly List<CompPhobosLitter> stale = new();
    static readonly MapPatientCache<Pawn> casualties = new(RebuildCasualties);
    /// <summary>Squared distance from a patient to their bed, by load ID, and when it was found: a bed search is dear, and a doctor
    /// looking for work asks about every casualty.</summary>
    static readonly Dictionary<int, (int tick, int distanceSquared)> bedDistances = new();

    // ---- The toggle ----

    /// <summary>The player's toggle. Says what it did.</summary>
    public static void SetCollecting(CompPhobosLitter litter, bool on)
    {
        litter.collecting = on;
        if (on && litter.Vehicle.Spawned) collecting.Add(litter);
        else collecting.Remove(litter);
        NamedArgument vehicle = litter.Vehicle.LabelShort.Named("VEHICLE");
        Messages.Message((on ? "PhobosLitter_CollectOn" : "PhobosLitter_CollectOff").Translate(vehicle).Resolve(), litter.Vehicle,
            MessageTypeDefOf.NeutralEvent, historical: false);
    }

    /// <summary>Why the toggle cannot be switched on now, or null. Switching it off is always allowed.</summary>
    public static string? SwitchOnRefusal(CompPhobosLitter litter)
    {
        NamedArgument vehicle = litter.Vehicle.LabelShort.Named("VEHICLE");
        if (litter.Vehicle.vehiclePather is { Moving: true }) return "PhobosLitter_Moving".Translate(vehicle).Resolve();
        if (OnMapFlight.Airborne(litter.Vehicle)) return "PhobosLitter_Airborne".Translate(vehicle).Resolve();
        return null;
    }

    internal static void Spawned(CompPhobosLitter litter)
    {
        if (litter.collecting) collecting.Add(litter);
    }

    internal static void Despawned(CompPhobosLitter litter) => collecting.Remove(litter);

    // ---- The work ----

    /// <summary>Whether any vehicle on the map is taking casualties aboard: the work giver's skip.</summary>
    public static bool AnyCollecting(Map map)
    {
        stale.Clear();
        bool any = false;
        foreach (CompPhobosLitter litter in collecting)
        {
            if (litter.Vehicle.Destroyed || !litter.Vehicle.Spawned) stale.Add(litter);
            else if (litter.Vehicle.Map == map && litter.KitFitted) any = true;
        }
        foreach (CompPhobosLitter gone in stale) collecting.Remove(gone);
        return any;
    }

    /// <summary>Downed people lying on the ground whom a carrier may take aboard; rebuilt every few seconds.</summary>
    public static IReadOnlyList<Pawn> Casualties(Map map) => casualties.Get(map, CacheIntervalTicks);

    static void RebuildCasualties(Map map, List<Pawn> list)
    {
        foreach (Pawn pawn in map.mapPawns.AllHumanlikeSpawned)
        {
            if (!IsCasualty(pawn)) continue;
            list.Add(pawn);
            if (IsStrangerInNeed(pawn)) Seen.Note("vehicles.litter-stranger", pawn);
        }
    }

    static bool IsCasualty(Pawn pawn) =>
        Patients.OnTheGround(pawn) && ((Patients.IsPlayersPerson(pawn) && Patients.CareLevel(pawn) != MedicalCareCategory.NoCare) || IsStrangerInNeed(pawn)
            || IsEnemyToCapture(pawn));

    /// <summary>
    /// A downed enemy the game would let the player capture, while the setting allows (Framework 0.335.0, EM21(d); the owner's card of 27 Nov
    /// 2026, "Order it, and collect them too"): put aboard, they become prisoners (<see cref="CompPhobosLitter.Capturable"/>).
    /// </summary>
    static bool IsEnemyToCapture(Pawn pawn) => VehicleSystemsMod.Settings.litterCollectsEnemies && pawn.Spawned && !pawn.Dead && CompPhobosLitter.Capturable(pawn);

    /// <summary>
    /// A downed stranger who is no enemy: a refugee from a crashed pod, a traveller hurt on the road (Framework 0.187.0;
    /// docs/development/vehicle-emergent-play.md, EM27). The manual order already took them; collecting now does too. Set down with the
    /// others, a colonist still has to be told to rescue them, as the game asks for any stranger.
    /// </summary>
    static bool IsStrangerInNeed(Pawn pawn) =>
        pawn.Spawned && !pawn.Dead && pawn.RaceProps.Humanlike && !DevelopmentalStageExtensions.Baby(pawn.DevelopmentalStage)
        && pawn.Faction != Faction.OfPlayer && pawn.HostFaction == null && !pawn.IsPrisoner && !pawn.HostileTo(Faction.OfPlayer);

    /// <summary>
    /// With field medics about, a casualty bleeding towards death (the game's own "tend now, urgently") is treated where they lie
    /// before anyone lifts them; whoever is treating holds them on the game's layer meanwhile, so the carry waits for that too.
    /// </summary>
    static bool WaitsForFieldCare(Pawn patient) =>
        OptionalMods.IsActive(OptionalMods.SearchAndRescue) && HealthAIUtility.ShouldBeTendedNowByPlayerUrgent(patient);

    /// <summary>The carry to the best litter for this casualty, or null.</summary>
    public static Job? CollectionJob(Pawn carrier, Pawn patient)
    {
        if (carrier == patient || !IsCasualty(patient) || WaitsForFieldCare(patient)) return null;
        // Framework 0.328.1: should our job ever be missing from Search and Rescue's list, it would drop the patient as soon as they were
        // lifted, and another carrier would be sent, round and round. Leave its marked patients to it instead.
        if (SearchAndRescueRoles.WouldDropCarry(patient)) return null;
        Danger danger = carrier.NormalMaxDanger();
        if (!carrier.CanReserveAndReach(patient, PathEndMode.OnCell, danger)) return null;
        CompPhobosLitter? best = null;
        int bestDistance = int.MaxValue;
        foreach (CompPhobosLitter litter in collecting)
        {
            if (litter.Vehicle.Destroyed || litter.Vehicle.Map != patient.Map || litter.RefusalFor(patient) != null) continue;
            if (LitterRule.FreeForNewPatients(litter.Free, CarriersOnTheWay(litter)) <= 0) continue;
            int distance = litter.Vehicle.Position.DistanceToSquared(patient.Position);
            if (distance >= bestDistance || !carrier.CanReach(litter.Vehicle, PathEndMode.Touch, danger)) continue;
            best = litter;
            bestDistance = distance;
        }
        if (best == null || !LitterRule.PrefersLitter(bestDistance, BedDistanceSquared(patient, carrier))) return null;
        Job job = JobMaker.MakeJob(LitterJobDefOf.PhobosVehicleSystems_CarryToLitter, patient, best.Vehicle);
        job.count = 1;
        return job;
    }

    /// <summary>Carriers already bringing someone to this vehicle's litters, ordered or not.</summary>
    static int CarriersOnTheWay(CompPhobosLitter litter)
    {
        int count = 0;
        foreach (Pawn pawn in litter.Vehicle.Map.mapPawns.FreeColonistsSpawned)
        {
            Job? job = pawn.CurJob;
            if (job?.def == LitterJobDefOf.PhobosVehicleSystems_CarryToLitter && job.targetB.Thing == litter.Vehicle) count++;
        }
        return count;
    }

    static int BedDistanceSquared(Pawn patient, Pawn carrier)
    {
        int now = Find.TickManager.TicksGame;
        if (bedDistances.TryGetValue(patient.thingIDNumber, out (int tick, int distanceSquared) known) && now - known.tick < CacheIntervalTicks)
        {
            return known.distanceSquared;
        }
        if (bedDistances.Count > 256) bedDistances.Clear();
        Building_Bed? bed = RestUtility.FindBedFor(patient, carrier, checkSocialProperness: false, ignoreOtherReservations: false, patient.GuestStatus);
        int distance = bed == null ? LitterRule.NoBed : bed.Position.DistanceToSquared(patient.Position);
        bedDistances[patient.thingIDNumber] = (now, distance);
        return distance;
    }

    /// <summary>A litter vehicle has just filled its last litter while switched on: says so once.</summary>
    internal static void NotifyLoaded(CompPhobosLitter litter)
    {
        if (!litter.collecting || litter.Free > 0) return;
        Messages.Message("PhobosLitter_CollectFull".Translate(litter.Vehicle.LabelShort.Named("VEHICLE")).Resolve(), litter.Vehicle,
            MessageTypeDefOf.NeutralEvent, historical: false);
    }
}

/// <summary>
/// Doctor work (and, with Search and Rescue, field rescue work): carry a downed person to a litter aboard a vehicle that is taking
/// casualties aboard. Every decision is <see cref="LitterService"/>'s; skipped outright while no vehicle on the map is switched on.
/// </summary>
public class WorkGiver_PhobosCarryToLitter : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

    public override PathEndMode PathEndMode => PathEndMode.OnCell;

    public override Danger MaxPathDanger(Pawn pawn) => pawn.NormalMaxDanger();

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => pawn.Map == null || !LitterService.AnyCollecting(pawn.Map);

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => LitterService.Casualties(pawn.Map);

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) => t is Pawn patient && LitterService.CollectionJob(pawn, patient) != null;

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Pawn patient) return null;
        Job? job = LitterService.CollectionJob(pawn, patient);
        if (job != null) Seen.Note("vehicles.litter-collection-carry", patient, job.targetB.Thing.LabelShort);
        return job;
    }
}
